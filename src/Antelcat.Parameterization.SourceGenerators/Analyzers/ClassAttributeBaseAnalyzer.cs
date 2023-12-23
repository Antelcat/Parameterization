// using System.Linq;
// using Microsoft.CodeAnalysis;
// using Microsoft.CodeAnalysis.Diagnostics;
//
// namespace Antelcat.Parameterization.SourceGenerators.Analyzers;
//
// public abstract class ClassAttributeBaseAnalyzer : DiagnosticAnalyzer
// {
//     public override void Initialize(AnalysisContext context)
//     {
//         context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
//         context.EnableConcurrentExecution();
//         
//         context.RegisterCompilationStartAction(static context =>
//         {
//             if (context.Compilation.GetTypeByMetadataName($"{Global.Namespace}.{nameof(ParameterizationAttribute)}") is not { } attribute)
//             {
//                 return;
//             }
//
//             context.RegisterSymbolAction(context =>
//             {
//                 if (context.Symbol is not INamedTypeSymbol typeSymbol) return;
//                 typeSymbol.GetMembers().OfType<IMethodSymbol>()
//                     .Where(m => m.GetAttributes().Any(a => a.))
//                 
//             }, SymbolKind.NamedType);
//         });
//     }
// }