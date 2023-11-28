using System.Collections.Immutable;
using System.Linq;
using Antelcat.Parameterization.SourceGenerators.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Antelcat.Parameterization.SourceGenerators.Generators;

public abstract class ClassAttributeBaseGenerator : IIncrementalGenerator
{
	protected abstract string AttributeName { get; }

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
			AttributeName,
			static (syntax, _) => syntax is ClassDeclarationSyntax,
			(syntaxContext, token) =>
			{
				var node = (ClassDeclarationSyntax)syntaxContext.TargetNode;
				return (syntaxContext, node.GetSpecifiedAttributes(syntaxContext.SemanticModel, AttributeName, token).FirstOrDefault());
			}
		).Where(x => x.Item2 != null);

		context.RegisterSourceOutput(provider.Collect(), GenerateCode);
	}

	protected abstract void GenerateCode(
		SourceProductionContext context,
		ImmutableArray<(GeneratorAttributeSyntaxContext, AttributeSyntax)> targets);
}