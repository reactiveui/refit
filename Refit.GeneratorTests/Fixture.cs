using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Refit.Generator;

namespace Refit.GeneratorTests;

public static class Fixture
{
    static readonly MetadataReference RefitAssembly = MetadataReference.CreateFromFile(
        typeof(GetAttribute).Assembly.Location,
        documentation: XmlDocumentationProvider.CreateFromFile(
            Path.ChangeExtension(typeof(GetAttribute).Assembly.Location, ".xml")
        )
    );

    private static readonly Type[] ImportantAssemblies = {
        typeof(Binder),
        typeof(GetAttribute),
        typeof(System.Reactive.Unit),
        typeof(Enumerable),
        typeof(Newtonsoft.Json.JsonConvert),
        typeof(FactAttribute),
        typeof(HttpContent),
        typeof(Attribute)
    };

    private static Assembly[] AssemblyReferencesForCodegen =>
        AppDomain.CurrentDomain
            .GetAssemblies()
            .Concat(ImportantAssemblies.Select(x=>x.Assembly))
            .Distinct()
            .Where(a => !a.IsDynamic)
            .ToArray();

    public static Task VerifyForBody(string body)
    {
        var source =
            $$"""
              using System;
              using System.Collections.Generic;
              using System.Linq;
              using System.Net.Http;
              using System.Text;
              using System.Threading;
              using System.Threading.Tasks;
              using Refit;

              namespace RefitGeneratorTest;

              public interface IGeneratedClient
              {
              {{body}}
              }
              """;

        return VerifyGenerator(source);
    }

    public static Task VerifyForType(string declarations)
    {
        var source =
            $$"""
              using System;
              using System.Collections.Generic;
              using System.Linq;
              using System.Net.Http;
              using System.Text;
              using System.Threading;
              using System.Threading.Tasks;
              using Refit;

              namespace RefitGeneratorTest;

              {{declarations}}
              """;

        return VerifyGenerator(source);
    }

    public static Task VerifyForDeclaration(string declarations)
    {
        var source =
            $$"""
              using System;
              using System.Collections.Generic;
              using System.Linq;
              using System.Net.Http;
              using System.Text;
              using System.Threading;
              using System.Threading.Tasks;
              using Refit;

              {{declarations}}
              """;

        return VerifyGenerator(source);
    }

    private static CSharpCompilation CreateLibrary(params string[] source)
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

    private static Task<VerifyResult> VerifyGenerator(string source)
    {
        var compilation = CreateLibrary(source);

        var generator = new InterfaceStubGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        var ranDriver = driver.RunGenerators(compilation);
        var settings = new VerifySettings();
        var verify = VerifyXunit.Verifier.Verify(ranDriver, settings);
        return verify.ToTask();
    }
}
