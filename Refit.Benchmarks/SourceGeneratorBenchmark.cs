using System.Reflection;

using BenchmarkDotNet.Attributes;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Refit.Generator;

namespace Refit.Benchmarks;

[MemoryDiagnoser]
public class SourceGeneratorBenchmark
{
    #region SourceText
    private const string SmallInterface =
        """
        using System;
        using System.Collections.Generic;
        using System.Linq;
        using System.Net.Http;
        using System.Text;
        using System.Threading;
        using System.Threading.Tasks;
        using Refit;

        namespace RefitGeneratorTest;

        public interface IReallyExcitingCrudApi<T, in TKey> where T : class
        {
            [Post("")]
            Task<T> Create([Body] T payload);

            [Get("")]
            Task<List<T>> ReadAll();

            [Get("/{key}")]
            Task<T> ReadOne(TKey key);

            [Put("/{key}")]
            Task Update(TKey key, [Body]T payload);

            [Delete("/{key}")]
            Task Delete(TKey key);
        }
        """;
    #endregion

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
    public void SetupSmall() => Setup(SmallInterface);

    [Benchmark]
    public GeneratorDriver Compile()
    {
        return driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
    }

    [GlobalSetup(Target = nameof(Cached))]
    public void SetupCached()
    {
        Setup(SmallInterface);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
        compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText("struct MyValue {}"));
    }

    [Benchmark]
    public GeneratorDriver Cached()
    {
        return driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
    }
}
