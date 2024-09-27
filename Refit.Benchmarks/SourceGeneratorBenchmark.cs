using System.Reflection;

using BenchmarkDotNet.Attributes;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Refit.Generator;

namespace Refit.Benchmarks;

[MemoryDiagnoser]
public class SourceGeneratorBenchmark
{
    static readonly MetadataReference RefitAssembly = MetadataReference.CreateFromFile(
        typeof(GetAttribute).Assembly.Location,
        documentation: XmlDocumentationProvider.CreateFromFile(
            Path.ChangeExtension(typeof(GetAttribute).Assembly.Location, ".xml")
        )
    );
    static readonly Type[] ImportantAssemblies = {
        typeof(Binder),
        typeof(GetAttribute),
        typeof(Enumerable),
        typeof(Newtonsoft.Json.JsonConvert),
        typeof(HttpContent),
        typeof(Attribute)
    };

    static Assembly[] AssemblyReferencesForCodegen =>
        AppDomain.CurrentDomain
            .GetAssemblies()
            .Concat(ImportantAssemblies.Select(x=>x.Assembly))
            .Distinct()
            .Where(a => !a.IsDynamic)
            .ToArray();

    private Compilation compilation;
    private CSharpGeneratorDriver driver;

    private void Setup(string sourceText)
    {
        var references = new List<MetadataReference>();
        var assemblies = AssemblyReferencesForCodegen;
        foreach (var assembly in assemblies)
        {
            if (!assembly.IsDynamic)
            {
                references.Add(MetadataReference.CreateFromFile(assembly.Location));
            }
        }

        references.Add(RefitAssembly);
        compilation = CSharpCompilation.Create(
            "compilation",
            [CSharpSyntaxTree.ParseText(sourceText)],
            references,
            new CSharpCompilationOptions(OutputKind.ConsoleApplication)
        );

        var generator = new InterfaceStubGeneratorV2().AsSourceGenerator();
        driver = CSharpGeneratorDriver.Create(generator);
    }

    [GlobalSetup(Target = nameof(Compile))]
    public void SetupSmall() => Setup(SourceGeneratorBenchmarksProjects.SmallInterface);

    [Benchmark]
    public GeneratorDriver Compile()
    {
        return driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
    }

    [GlobalSetup(Target = nameof(Cached))]
    public void SetupCached()
    {
        Setup(SourceGeneratorBenchmarksProjects.SmallInterface);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
        compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText("struct MyValue {}"));
    }

    [Benchmark]
    public GeneratorDriver Cached()
    {
        return driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
    }

    [GlobalSetup(Target = nameof(CompileMany))]
    public void SetupMany() => Setup(SourceGeneratorBenchmarksProjects.ManyInterfaces);

    [Benchmark]
    public GeneratorDriver CompileMany()
    {
        return driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
    }

    [GlobalSetup(Target = nameof(CachedMany))]
    public void SetupCachedMany()
    {
        Setup(SourceGeneratorBenchmarksProjects.ManyInterfaces);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
        compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText("struct MyValue {}"));
    }

    [Benchmark]
    public GeneratorDriver CachedMany()
    {
        return driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
    }
}
