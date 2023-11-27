﻿using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using Antelcat.Parameterization.SourceGenerators.Extensions;
using Antelcat.Parameterization.SourceGenerators.Models;
using Antelcat.Parameterization.SourceGenerators.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Antelcat.Parameterization.SourceGenerators.Generators;

[Generator(LanguageNames.CSharp)]
public class ParameterizationGenerator : ClassAttributeBaseGenerator
{
	protected override string AttributeName => $"{Global.Namespace}.{nameof(ParameterizationAttribute)}";

	protected override void GenerateCode(SourceProductionContext context, ImmutableArray<(GeneratorAttributeSyntaxContext, AttributeSyntax)> targets)
	{
		var convertersMap = new Dictionary<ITypeSymbol, string>(SymbolEqualityComparer.Default);
		var converterBuilder = new SourceStringBuilder()
			.Append(
				"""
				// <auto-generated/>
				#nullable enable
				using System;

				namespace Antelcat.Parameterization
				{
					public static class Converters
					{
				""")
			.Indent(2);

		string GetConverterName(ITypeSymbol typeSymbol)
		{
			var converterName = typeSymbol.Name;
			if (convertersMap.ContainsValue(converterName))
			{
				var i = 1;
				while (convertersMap.ContainsValue(converterName + i))
				{
					i++;
				}
				converterName += i;
			}

			convertersMap.Add(typeSymbol, converterName);
			return converterName;
		}

		foreach (var (syntaxContext, attributeSyntax) in targets)
		{
			var attributeProxy = new AttributeProxy(attributeSyntax);
			var caseSensitive = attributeProxy[nameof(ParameterizationAttribute.CaseSensitive)] as bool? ?? false;

			var classDeclarationSyntax = (ClassDeclarationSyntax)syntaxContext.TargetNode;

			var classAccessModifier =
				classDeclarationSyntax.Modifiers
					.FirstOrDefault(modifier =>
						modifier.IsKind(SyntaxKind.PublicKeyword) || modifier.IsKind(SyntaxKind.InternalKeyword))
					.Text ??
				"internal"; // 默认为 internal
			var isClassStatic = classDeclarationSyntax.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.StaticKeyword));

			IEnumerable<(MethodDeclarationSyntax, AttributeProxy)> GetCandidateMethods()
			{
				foreach (var method in classDeclarationSyntax.Members.OfType<MethodDeclarationSyntax>())
				{
					// 如果类是static，那么方法也必须是static
					if (method.Modifiers.All(modifier => !modifier.IsKind(SyntaxKind.StaticKeyword)) && isClassStatic) continue;
					if (method.GetSpecifiedAttributes<CommandAttribute>(syntaxContext.SemanticModel, context.CancellationToken)
						    .FirstOrDefault() is not { } commandAttribute) continue;
					yield return (method, commandAttribute);
				}
			}

			var parameterTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
			var converterTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
			foreach (var (method, _) in GetCandidateMethods())
			{
				foreach (var parameter in method.ParameterList.Parameters)
				{
					if (parameter.GetSpecifiedAttributes<ArgumentAttribute>(
							    syntaxContext.SemanticModel,
							    context.CancellationToken)
						    .FirstOrDefault()?[nameof(ArgumentAttribute.Converter)] is TypeSyntax converterType &&
					    syntaxContext.SemanticModel.GetSymbolInfo(converterType).Symbol is ITypeSymbol typeSymbol)
					{
						if (!typeSymbol.IsDerivedFrom<StringConverter>())
						{
							var diagnostic = Diagnostic.Create(
								new DiagnosticDescriptor(
									"PC0001",
									"Error",
									$"Converter \"{converterType}\" must be derived from \"{nameof(StringConverter)}\".",
									nameof(ParameterizationAttribute),
									DiagnosticSeverity.Error,
									isEnabledByDefault: true
								),
								parameter.GetLocation()
							);

							context.ReportDiagnostic(diagnostic);
							return;
						}

						if (typeSymbol.GetMembers().All(symbol =>
							    symbol is not IMethodSymbol
							    {
								    MethodKind: MethodKind.Constructor,
								    Parameters.IsEmpty: true,
								    DeclaredAccessibility: Accessibility.Public
							    }))
						{
							var diagnostic = Diagnostic.Create(
								new DiagnosticDescriptor(
									"PC0002",
									"Error",
									$"Converter \"{converterType}\" must have a public parameterless constructor.",
									nameof(ParameterizationAttribute),
									DiagnosticSeverity.Error,
									isEnabledByDefault: true
								),
								parameter.GetLocation()
							);

							context.ReportDiagnostic(diagnostic);
							return;
						}

						converterTypes.Add(syntaxContext.SemanticModel.GetSymbolInfo(converterType).Symbol.NotNull<ITypeSymbol>());
					}
					else
					{
						parameterTypes.Add(syntaxContext.SemanticModel.GetSymbolInfo(parameter.Type.NotNull()).Symbol.NotNull<ITypeSymbol>());
					}
				}
			}

			foreach (var parameterType in parameterTypes)
			{
				converterBuilder.AppendLine(
					$"public static {Global.TypeConverter} {GetConverterName(parameterType)}Converter {{ get; }} = {Global.TypeDescriptor}.GetConverter(typeof({parameterType}));");
			}
			foreach (var converterType in converterTypes)
			{
				converterBuilder.AppendLine(
					$"public static {Global.TypeConverter} {GetConverterName(converterType)}Converter {{ get; }} = new {converterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}();");
			}

			var caseBuilder = new SourceStringBuilder(initialIndentCount: 3);
			foreach (var (method, commandAttribute) in GetCandidateMethods())
			{
				var fullName = commandAttribute[nameof(CommandAttribute.FullName)] as string ?? method.Identifier.ValueText;
				caseBuilder.AppendLine($"case \"{(caseSensitive ? fullName : fullName.ToLower())}\":");
				if (commandAttribute[nameof(CommandAttribute.ShortName)] is string shortName)
				{
					caseBuilder.AppendLine($"case \"{(caseSensitive ? shortName : shortName.ToLower())}\":");
				}

				caseBuilder.AppendLine('{').Indent();

				if (method.ParameterList.Parameters.Count > 0)
				{
					var methodParameters = method.ParameterList.Parameters.ToImmutableList();
					var parameterAttributes = methodParameters
						.Select(parameter => parameter.GetSpecifiedAttributes<ArgumentAttribute>(
							syntaxContext.SemanticModel,
							context.CancellationToken).FirstOrDefault())
						.ToImmutableList();

					caseBuilder
						.AppendLine($"var names = new {Global.ValueTuple}<{Global.String}, {Global.String}?>[] {{ {string.Join(", ",
							parameterAttributes.Zip(methodParameters)
								.Select(x =>
								(
									x.Item1?[nameof(ArgumentAttribute.FullName)] as string ?? x.Item2.Identifier.ValueText,
									x.Item1?[nameof(ArgumentAttribute.ShortName)] as string
								))
								.Select(x => $"new {Global.ValueTuple}<{Global.String}, {Global.String}?>({x.Item1.Escape()}, {x.Item2.Escape()})"))} }};")
						.AppendLine($"var isBoolean = new {Global.Boolean}[] {{ {string.Join(", ",
							methodParameters
								.Select(parameter => parameter.Type is PredefinedTypeSyntax { Keyword.Text: "bool" })
								.Select(b => b ? "true" : "false"))} }};") // TODO: Default Converter而不是bool硬编码
						.AppendLine($"var argumentConverters = new {Global.TypeConverter}[] {{ {string.Join(", ",
							parameterAttributes.Zip(methodParameters)
								.Select(x => x.Item1?[nameof(ArgumentAttribute.Converter)] is TypeSyntax converterType
									? convertersMap[syntaxContext.SemanticModel.GetSymbolInfo(converterType).Symbol.NotNull<ITypeSymbol>()]
									: convertersMap[syntaxContext.SemanticModel.GetSymbolInfo(x.Item2.Type.NotNull()).Symbol.NotNull<ITypeSymbol>()])
								.Select(name => $"{Global.Namespace}.Converters.{name}Converter"))} }};")
						.AppendLine($"var args = global::{Global.Namespace}.Utils.ParseArguments(commandAndArguments, names, isBoolean, argumentConverters);")
						.AppendLine($"{method.Identifier.ValueText}({string.Join(", ",
							methodParameters
								.Select(p => p.Type.ToDisplayName(syntaxContext.SemanticModel).NotNull())
								.WithIndex()
								.Select(x => $"({x.Item2})args[{x.Item1}]"))});");
				}
				else
				{
					caseBuilder.AppendLine($"{method.Identifier.ValueText}();");
				}

				caseBuilder.AppendLine("break;").OutDent().AppendLine('}');
			}

			var sourceBuilder = new SourceStringBuilder().AppendLine("// <auto-generated/>").AppendLine("#nullable enable");

			var namespaceSyntax = classDeclarationSyntax.Parent as BaseNamespaceDeclarationSyntax;
			if (namespaceSyntax != null)
			{
				sourceBuilder.AppendLine($"namespace {namespaceSyntax.Name}").AppendLine('{').Indent();
			}
			var className = classDeclarationSyntax.Identifier.ValueText;

			sourceBuilder.Append(
				$$""""
				  {{classAccessModifier}} {{(isClassStatic ? "static" : string.Empty)}} partial class {{className}}
				  {
				  	private static void ExecuteInput(global::System.String? input)
				  	{
				  		if (global::System.String.IsNullOrEmpty(input))
				  		{
				  		    return;
				  		}
				  
				  		var commandAndArguments = new global::System.Collections.Generic.List<global::System.String>();
				  		foreach (global::System.Text.RegularExpressions.Match match in global::System.Text.RegularExpressions.Regex.Matches(input, @"[^\s""]+|""([^""]|(\\""))*"""))
				  		{
				  		    var part = match.Value;
				  		    part = global::System.Text.RegularExpressions.Regex.Replace(part, @"^""|""$", "").Replace("\\\"", "\""){{(caseSensitive ? "" : ".ToLower()")}};
				  		    commandAndArguments.Add(part);
				  		}
				  
				  		if (commandAndArguments.Count == 0)
				  		{
				  		    return;
				  		}
				  
				  		switch (commandAndArguments[0])
				  		{
				  {{caseBuilder}}
				  		    default:
				  		    {
				  				throw new global::System.ArgumentException($"Command \"{commandAndArguments[0]}\" not found.");
				  		    }
				  		}
				  	}
				  }
				  """");

			if (namespaceSyntax == null)
			{
				context.AddSource($"{className}.g.cs", sourceBuilder.ToString());
			}
			else
			{
				context.AddSource($"{namespaceSyntax.Name}.{className}.g.cs", sourceBuilder.OutDent().Append('}').ToString());
			}
		}

		context.AddSource(
			$"{Global.Namespace}.Converters.g.cs", 
			converterBuilder.OutDent().AppendLine("}").OutDent().AppendLine("}").ToString());
	}
}