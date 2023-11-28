using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Antelcat.Parameterization.SourceGenerators.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Antelcat.Parameterization.SourceGenerators.Extensions;

public static class SyntaxExtension
{
	public static IEnumerable<AttributeSyntax> GetAllAttributes(this SyntaxNode syntax)
	{
		var attributeLists = syntax switch
		{
			MemberDeclarationSyntax member => member.AttributeLists,
			BaseParameterSyntax parameter => parameter.AttributeLists,
			LambdaExpressionSyntax lambda => lambda.AttributeLists,
			StatementSyntax statementSyntax => statementSyntax.AttributeLists,
			CompilationUnitSyntax compilationUnitSyntax => compilationUnitSyntax.AttributeLists,
			_ => throw new NotSupportedException($"{syntax.GetType().Name} is not supported for GetAllAttributes.")
		};
		return attributeLists.SelectMany(attributeList => attributeList.Attributes);
	}

	public static IEnumerable<AttributeSyntax> GetSpecifiedAttributes(
		this SyntaxNode syntax,
		SemanticModel semanticModel,
		string attributeFullName,
		CancellationToken cancellationToken = default)
	{
		foreach (var attributeSyntax in syntax.GetAllAttributes())
		{
			if (cancellationToken.IsCancellationRequested) yield break;
			if (semanticModel.GetSymbolInfo(attributeSyntax, cancellationToken).Symbol is not IMethodSymbol attributeSymbol) continue;
			var attributeName = attributeSymbol.ContainingType.ToDisplayString();
			if (attributeName == attributeFullName) yield return attributeSyntax;
		}
	}

	/// <summary>
	/// 获取一个Type下面的所有指定的Attribute
	/// </summary>
	/// <param name="syntax"></param>
	/// <param name="semanticModel"></param>
	/// <param name="cancellationToken"></param>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public static IEnumerable<AttributeProxy> GetSpecifiedAttributes<T>(
		this SyntaxNode syntax,
		SemanticModel semanticModel,
		CancellationToken cancellationToken = default)
		where T : Attribute
	{
		var attributeType = typeof(T);
		var attributeFullName = attributeType.FullName;
		if (attributeFullName == null) yield break;
		foreach (var attribute in syntax.GetSpecifiedAttributes(semanticModel, attributeFullName, cancellationToken))
		{
			yield return new AttributeProxy(attribute);
		}
	}

	public static bool IsDerivedFrom<T>(this ITypeSymbol? typeSymbol)
	{
		var targetTypeFullName = typeof(T).FullName;
		while (typeSymbol != null)
		{
			if (typeSymbol.ToDisplayString() == targetTypeFullName) return true;
			typeSymbol = typeSymbol.BaseType;
		}

		return false;
	}

	public static string? ToDisplayName(this SyntaxNode? syntax, SemanticModel semanticModel, SymbolDisplayFormat? symbolDisplayFormat = null)
	{
		return syntax == null ? null : semanticModel.GetSymbolInfo(syntax).Symbol?.ToDisplayString(symbolDisplayFormat);
	}
}