using System.Reflection;
using Antelcat.Parameterization.SourceGenerators.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Mono.Cecil;

namespace Antelcat.Parameterization.Tests;

public class ParameterizationGeneratorTests
{
    private static bool TryReadAssembly(string assemblyPath)
    {
        try
        {
            AssemblyDefinition.ReadAssembly(assemblyPath).Dispose();
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    private IEnumerable<MetadataReference> RuntimeMetadataReferences
    {
        get
        {
            const string runtimePath = @"C:\Program Files\dotnet\shared\Microsoft.NETCore.App\6.0.25";
            var runtimeFiles = Directory.EnumerateFiles(runtimePath, "*.dll");
            foreach (var assemblyPath in runtimeFiles.Where(TryReadAssembly))
            {
                yield return MetadataReference.CreateFromFile(assemblyPath);
            }

            yield return MetadataReference.CreateFromFile(Assembly.Load($"{nameof(Antelcat)}.{nameof(Parameterization)}").Location);
        }
    }
    
    [Test]
    public void Test()
    {
        var compilation = CSharpCompilation.Create(
            nameof(ParameterizationGeneratorTests),
            new[] { CSharpSyntaxTree.ParseText(File.ReadAllText("./Resources/Program.cs")) },
            RuntimeMetadataReferences,
            new CSharpCompilationOptions(OutputKind.ConsoleApplication)) as Compilation;
        
        var commonDriver = CSharpGeneratorDriver.Create(new CommonGenerator());
        commonDriver.RunGeneratorsAndUpdateCompilation(compilation, out compilation, out var diagnostics);
        
        var runErrors = diagnostics
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => new Exception(diagnostic.ToString()))
            .ToList();

        if (runErrors.Count > 0)
        {
            throw new AggregateException($"{nameof(CommonGenerator)} Error", runErrors);
        }
        
        var parameterizationDriver = CSharpGeneratorDriver.Create(new ParameterizationGenerator());
        parameterizationDriver.RunGeneratorsAndUpdateCompilation(compilation, out compilation, out diagnostics);
        
        runErrors = diagnostics
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => new Exception(diagnostic.ToString()))
            .ToList();

        if (runErrors.Count > 0)
        {
            throw new AggregateException($"{nameof(ParameterizationGenerator)} Error", runErrors);
        }
        
        var compileErrors = compilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => new Exception(diagnostic.ToString()))
            .ToList();

        if (compileErrors.Count > 0)
        {
            throw new AggregateException("Compile Error", compileErrors);
        }
    }
}