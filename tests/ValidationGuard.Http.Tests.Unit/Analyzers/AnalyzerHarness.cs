using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using ValidationGuard.Http.Analyzers;

namespace ValidationGuard.Http.Tests.Unit.Analyzers;

internal static class AnalyzerHarness
{
    private static readonly CSharpCompilationOptions LibraryOptions = new(OutputKind.DynamicallyLinkedLibrary);

    private static readonly ImmutableArray<MetadataReference> References = LoadReferences();

    public static MetadataReference CompileLibrary(string assemblyName, string source)
    {
        var compilation = Compile(assemblyName, source);

        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));

        return MetadataReference.CreateFromImage(stream.ToArray());
    }

    public static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string source, params MetadataReference[] libraries)
    {
        var compilation = Compile("App", source, libraries);

        var compilerErrors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.True(compilerErrors.Length == 0, string.Join(Environment.NewLine, compilerErrors.Select(d => d.ToString())));

        return await compilation
            .WithAnalyzers([new HttpErrorCodeAnalyzer()])
            .GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);
    }

    private static CSharpCompilation Compile(string assemblyName, string source, params MetadataReference[] libraries) =>
        CSharpCompilation.Create(
            assemblyName,
            [CSharpSyntaxTree.ParseText(source)],
            References.AddRange(libraries),
            LibraryOptions);

    private static ImmutableArray<MetadataReference> LoadReferences()
    {
        var testAssembly = typeof(AnalyzerHarness).Assembly.Location;
        var paths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);

        return
        [
            .. paths
                .Where(path => !string.Equals(path, testAssembly, StringComparison.OrdinalIgnoreCase))
                .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path)),
        ];
    }
}
