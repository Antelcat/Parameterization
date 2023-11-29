using System.Reflection;
using System.Runtime.InteropServices;
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
            var runtimeFiles = Directory.EnumerateFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll");
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
        
        var parameterizationDriver = CSharpGeneratorDriver.Create(new ParameterizationGenerator());
        parameterizationDriver.RunGeneratorsAndUpdateCompilation(compilation, out compilation, out var diagnostics);
        
        var runErrors = diagnostics
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