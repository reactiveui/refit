using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Refit.Generator;

namespace Refit.Benchmarks;

/// <summary>
/// Shared setup for the source-generator benchmarks: builds a compilation that references Refit
/// plus the current app domain, and creates a driver running <see cref="InterfaceStubGeneratorV2"/>.
/// </summary>
internal static class GeneratorBenchmarkHarness
{
    private static readonly MetadataReference RefitAssembly = MetadataReference.CreateFromFile(
        typeof(GetAttribute).Assembly.Location,
        documentation: XmlDocumentationProvider.CreateFromFile(
            Path.ChangeExtension(typeof(GetAttribute).Assembly.Location, ".xml")
        )
    );

    private static readonly Type[] ImportantAssemblies =
    {
        typeof(Binder),
        typeof(GetAttribute),
        typeof(Enumerable),
        typeof(Newtonsoft.Json.JsonConvert),
        typeof(HttpContent),
        typeof(Attribute),
    };

    private static Assembly[] AssemblyReferencesForCodegen =>
        AppDomain
            .CurrentDomain.GetAssemblies()
            .Concat(ImportantAssemblies.Select(x => x.Assembly))
            .Distinct()
            .Where(a => !a.IsDynamic)
            .ToArray();

    /// <summary>
    /// Creates a compilation for <paramref name="sourceText"/> and a generator driver for it.
    /// </summary>
    public static (Compilation Compilation, CSharpGeneratorDriver Driver) Create(string sourceText)
    {
        var references = new List<MetadataReference>();
        foreach (var assembly in AssemblyReferencesForCodegen)
        {
            if (!assembly.IsDynamic)
                references.Add(MetadataReference.CreateFromFile(assembly.Location));
        }

        references.Add(RefitAssembly);

        var compilation = CSharpCompilation.Create(
            "compilation",
            [CSharpSyntaxTree.ParseText(sourceText)],
            references,
            new CSharpCompilationOptions(OutputKind.ConsoleApplication)
        );

        var driver = CSharpGeneratorDriver.Create(
            new InterfaceStubGeneratorV2().AsSourceGenerator()
        );
        return (compilation, driver);
    }

    /// <summary>
    /// Runs the generator once and then adds an unrelated syntax tree, leaving the driver primed
    /// for an incremental (cached) re-run.
    /// </summary>
    public static (Compilation Compilation, CSharpGeneratorDriver Driver) CreatePrimedForCachedRun(
        string sourceText
    )
    {
        var (compilation, driver) = Create(sourceText);
        driver = (CSharpGeneratorDriver)
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
        compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText("struct MyValue {}"));
        return (compilation, driver);
    }
}
