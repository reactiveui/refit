using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Refit;
using Refit.Generator;

namespace RefitGenerator.Tests;


public static class Fixture
{
    public static readonly Type[] ImportantAssemblies = new[]
    {
        typeof(Binder),
        typeof(GetAttribute),
        typeof(System.Reactive.Unit),
        typeof(System.Linq.Enumerable),
        typeof(Newtonsoft.Json.JsonConvert),
        typeof(Xunit.FactAttribute),
        typeof(System.Net.Http.HttpContent),
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
        foreach (Assembly assembly in assemblies)
        {
            if (!assembly.IsDynamic)
            {
                references.Add(MetadataReference.CreateFromFile(assembly.Location));
            }
        }

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

    public static Task<string> SourceFromResourceFile(string file) =>
        File.ReadAllTextAsync(Path.Combine("resources", file));
}
