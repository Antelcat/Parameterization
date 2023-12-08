using System.Collections.Generic;
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
        if (targets.Length == 0) return;

        CommonGenerator.Execute(context);

        var convertersGenerator = new ConvertersGenerator();

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
            var isStaticClass = classDeclarationSyntax.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.StaticKeyword));

            IEnumerable<(MethodDeclarationSyntax, AttributeProxy)> GetCandidateMethods()
            {
                foreach (var method in classDeclarationSyntax.Members.OfType<MethodDeclarationSyntax>())
                {
                    // 如果类是static，那么方法也必须是static
                    if (method.Modifiers.All(modifier => !modifier.IsKind(SyntaxKind.StaticKeyword)) && isStaticClass) continue;
                    if (method.GetSpecifiedAttributes<CommandAttribute>(syntaxContext.SemanticModel, context.CancellationToken)
                            .FirstOrDefault() is not { } commandAttribute) continue;
                    yield return (method, commandAttribute);
                }
            }

            var candidateMethods = GetCandidateMethods().ToImmutableList();
            if (candidateMethods.Count == 0)
            {
                return;
            }

            var parameterTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
            var converterTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
            foreach (var (method, _) in candidateMethods)
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
                                    "AP0001",
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
                                    "AP0002",
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
                convertersGenerator.AddConverter(
                    parameterType,
                    $"{Global.TypeDescriptor}.GetConverter(typeof({parameterType}))");
            }
            foreach (var converterType in converterTypes)
            {
                convertersGenerator.AddConverter(
                    converterType,
                    $"new {converterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}()");
            }

            var isStatic = true;
            var isAsync = false;
            var caseBuilder = new SourceStringBuilder(initialIndentCount: 3);
            foreach (var (method, commandAttribute) in candidateMethods)
            {
                if (method.Modifiers.All(modifier => !modifier.IsKind(SyntaxKind.StaticKeyword)))
                {
                    isStatic = false;
                }
                
                {
                    var fullName = commandAttribute[nameof(CommandAttribute.FullName)] as string ?? method.Identifier.ValueText;
                    caseBuilder.AppendLine($"case \"{(caseSensitive ? fullName : fullName.ToLower())}\":");
                    if (commandAttribute[nameof(CommandAttribute.ShortName)] is string shortName)
                    {
                        caseBuilder.AppendLine($"case \"{(caseSensitive ? shortName : shortName.ToLower())}\":");
                    }
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

                    var parameterNames = new (string fullName, string? shortName)[methodParameters.Count];
                    for (var i = 0; i < parameterNames.Length; i++)
                    {
                        var fullName = parameterAttributes[i]?[nameof(ArgumentAttribute.FullName)] as string ??
                                       methodParameters[i].Identifier.ValueText;
                        var shortName = parameterAttributes[i]?[nameof(ArgumentAttribute.ShortName)] as char? ?? '\0';
                        if (shortName != '\0' && shortName is not (>= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9'))
                        {
                            var diagnostic = Diagnostic.Create(
                                new DiagnosticDescriptor(
                                    "AP0003",
                                    "Error",
                                    $"ShortName \"{shortName}\" must be a single letter or number.",
                                    nameof(ParameterizationAttribute),
                                    DiagnosticSeverity.Error,
                                    isEnabledByDefault: true
                                ),
                                methodParameters[i].GetLocation()
                            );
                            context.ReportDiagnostic(diagnostic);
                            return;
                        }

                        parameterNames[i] = (fullName, shortName == '\0' ? null : shortName.ToString());
                    }

                    var parameterNamesDictionary = new Dictionary<string, ParameterSyntax>();
                    for (var i = 0; i < parameterNames.Length; i++)
                    {
                        var (fullName, shortName) = parameterNames[i];
                        if (parameterNamesDictionary.TryGetValue(fullName, out var conflict) ||
                            shortName != null && parameterNamesDictionary.TryGetValue(shortName, out conflict))
                        {
                            var diagnostic = Diagnostic.Create(
                                new DiagnosticDescriptor(
                                    "AP0004",
                                    "Error",
                                    $"Parameter \"{methodParameters[i].Identifier.ValueText}\" conflicts with parameter \"{conflict.Identifier.ValueText}\".",
                                    nameof(ParameterizationAttribute),
                                    DiagnosticSeverity.Error,
                                    isEnabledByDefault: true
                                ),
                                conflict.GetLocation()
                            );
                            context.ReportDiagnostic(diagnostic);
                            return;
                        }
                        
                        parameterNamesDictionary.Add(fullName, methodParameters[i]);
                        if (shortName != null)
                        {
                            parameterNamesDictionary.Add(shortName, methodParameters[i]);
                        }
                    }

                    caseBuilder
                        .AppendLine($"var parsedArguments = new {Global.GlobalNamespace}.ParsedArgument[] {{ {string.Join(", ",
                            methodParameters
                                .Select(p => p.Type.NotNull().IsArray(syntaxContext.SemanticModel) ? 
                                    $"new {Global.GlobalNamespace}.ParsedArrayArgument(\"{p.Identifier.ValueText}\")" : 
                                    $"new {Global.GlobalNamespace}.ParsedArgument(\"{p.Identifier.ValueText}\")"))} }};")
                        .AppendLine($"var argumentNames = new {Global.ValueTuple}<{Global.String}, {Global.String}?>[] {{ {string.Join(", ",
                            parameterNames
                                .Select(x => $"new {Global.ValueTuple}<{Global.String}, {Global.String}?>({x.Item1.Escape()}, {x.Item2.Escape()})"))} }};")
                        .AppendLine($"var defaultValues = new {Global.String}?[] {{ {string.Join(", ",
                            parameterAttributes
                                .Select(x => x?[nameof(ArgumentAttribute.DefaultValue)] as string)
                                .Select(x => x == null ? "null" : $"\"{x}\""))} }};")
                        .AppendLine($"var argumentConverters = new {Global.TypeConverter}[] {{ {string.Join(", ",
                            parameterAttributes.Zip(methodParameters)
                                .Select(x => x.Item1?[nameof(ArgumentAttribute.Converter)] is TypeSyntax converterType
                                    ? convertersGenerator.ConvertersMap[syntaxContext.SemanticModel.GetSymbolInfo(converterType).Symbol.NotNull<ITypeSymbol>()]
                                    : convertersGenerator.ConvertersMap[syntaxContext.SemanticModel.GetSymbolInfo(x.Item2.Type.NotNull()).Symbol.NotNull<ITypeSymbol>()])
                                .Select(name => $"{Global.GlobalNamespace}.Converters.{name}Converter"))} }};")
                        .AppendLine(
                            $"{Global.GlobalNamespace}.Common.ParseArguments(parsedArguments, arguments, argumentNames, defaultValues, argumentConverters);");

                    if (method.ReturnType.IsAwaitable(syntaxContext.SemanticModel))
                    {
                        isAsync = true;
                        caseBuilder.Append("await ");
                    }
                    caseBuilder
                        .AppendLine($"{method.Identifier.ValueText}({string.Join(", ",
                            methodParameters
                                .Select(p =>
                                (
                                    typeName: p.Type.NotNull().ToDisplayName(syntaxContext.SemanticModel),
                                    defaultValue: p.Default?.Value.ToString()
                                ))
                                .WithIndex()
                                .Select(x =>
                                    $"{Global.GlobalNamespace}.Common.ConvertArgument<{x.value.typeName}>(parsedArguments[{x.index}]{(
                                        x.value.defaultValue == null ? string.Empty : $", {x.value.defaultValue}")})"))});");
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
                $$"""
                  {{classAccessModifier}} {{(isStaticClass ? "static" : string.Empty)}} partial class {{className}}
                  {
                      private {{"static ".If(isStatic)}}{{(isAsync ? Global.ValueTask : "void")}} ExecuteInput{{"Async".If(isAsync)}}({{Global.String}}? input)
                      {
                          if ({{Global.String}}.IsNullOrEmpty(input))
                          {
                              return{{" default".If(isAsync)}};
                          }
                  
                          var arguments = new {{Global.GenericList}}<{{Global.String}}>();
                          foreach ({{Global.Match}} match in {{Global.GlobalNamespace}}.Common.CommandRegex.Matches(input))
                          {
                              var part = match.Value;
                              part = {{Global.GlobalNamespace}}.Common.QuotationRegex.Replace(part, "").Replace("\\\"", "\""){{(caseSensitive ? "" : ".ToLower()")}};
                              arguments.Add(part);
                          }
                  
                          {{"return ".If(isAsync)}}ExecuteArguments{{"Async".If(isAsync)}}(arguments);
                      }
                      
                      private {{"static ".If(isStatic)}}{{(isAsync ? $"async {Global.ValueTask}" : "void")}} ExecuteArguments{{"Async".If(isAsync)}}({{Global.GenericIReadonlyList}}<{{Global.String}}> arguments)
                      {
                          if (arguments.Count == 0)
                          {
                              return;
                          }
                  
                          switch (arguments[0])
                          {
                  {{caseBuilder}}
                              default:
                              {
                                  throw new {{Global.ArgumentException}}($"Command \"{arguments[0]}\" not found.");
                              }
                          }
                      }
                  }
                  """);

            if (namespaceSyntax == null)
            {
                context.AddSource($"{className}.g.cs", sourceBuilder.ToString());
            }
            else
            {
                context.AddSource($"{namespaceSyntax.Name}.{className}.g.cs", sourceBuilder.OutDent().Append('}').ToString());
            }
        }

        convertersGenerator.Execute(context);
    }
}