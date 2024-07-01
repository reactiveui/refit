using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Refit;
using Refit.Generator;

namespace RefitGenerator.Tests;


public static class Fixture
{
    private const string MainTestFile = "Refit.Tests";

    static readonly MetadataReference RefitAssembly = MetadataReference.CreateFromFile(
        typeof(GetAttribute).Assembly.Location,
        documentation: XmlDocumentationProvider.CreateFromFile(
            Path.ChangeExtension(typeof(GetAttribute).Assembly.Location, ".xml")
        )
    );

    private static readonly Type[] ImportantAssemblies = new[]
    {
        typeof(Binder),
        typeof(GetAttribute),
        typeof(System.Reactive.Unit),
        typeof(Enumerable),
        typeof(Newtonsoft.Json.JsonConvert),
        typeof(FactAttribute),
        typeof(HttpContent),
        // typeof(ModelObject),
        typeof(Attribute)
    };

    public static Assembly[] AssemblyReferencesForCodegen =>
        AppDomain.CurrentDomain
            .GetAssemblies()
            .Concat(ImportantAssemblies.Select(x=>x.Assembly))
            .Distinct()
            .Where(a => !a.IsDynamic)
            .ToArray();

    public static Compilation CreateLibrary(params string[] source)
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
        var compilation = CSharpCompilation.Create(
            "compilation",
            source.Select(s => CSharpSyntaxTree.ParseText(s)),
            references,
            new CSharpCompilationOptions(OutputKind.ConsoleApplication)
        );

        return compilation;
    }

    public static Task VerifyGenerator(string source)
    {
        var compilation = CreateLibrary(source);

        var generator = new InterfaceStubGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        var ranDriver = driver.RunGenerators(compilation);
        var verify = VerifyXunit.Verifier.Verify(ranDriver);
        return verify.ToTask();
    }

    public static async Task<string> GetFileFromRefitTest(string file)
    {
#if NET481
        return File.ReadAllText(GetRefitTestPath(file));
#else
        return await File.ReadAllTextAsync(GetRefitTestPath(file)).ConfigureAwait(false);
#endif
    }

    private static string GetRefitTestPath(params string[] paths)
    {
        var ret = GetIntegrationTestRootDirectory();
        var start = ret.Split('\\');
        // ReSharper disable once UseIndexFromEndExpression
        start[start.Length - 1] = MainTestFile;
        ret = string.Join("\\", start);
        return (new FileInfo(paths.Aggregate(ret, Path.Combine))).FullName;
    }

    private static string GetIntegrationTestRootDirectory([CallerFilePath] string filePath = default)
    {
        // XXX: This is an evil hack, but it's okay for a unit test
        // We can't use Assembly.Location because unit test runners love
        // to move stuff to temp directories
        var di = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(filePath)));

        return di.FullName;
    }
}
