// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Refit.Generator;

namespace Refit.GeneratorTests;

/// <summary>Helpers for compiling sources and verifying the Refit source generator output.</summary>
[UnconditionalSuppressMessage(
    "SingleFile",
    "IL3000:Avoid accessing Assembly file path when publishing as a single file",
    Justification = "Compiles generator inputs against on-disk assemblies; never run as a single-file app.")]
public static class Fixture
{
    /// <summary>The metadata reference for the Refit assembly with documentation.</summary>
    private static readonly MetadataReference _refitAssembly = MetadataReference.CreateFromFile(
        typeof(GetAttribute).Assembly.Location,
        documentation: XmlDocumentationProvider.CreateFromFile(
            Path.ChangeExtension(typeof(GetAttribute).Assembly.Location, ".xml")));

    /// <summary>Types whose assemblies must be referenced during code generation.</summary>
    private static readonly Type[] _importantAssemblies =
    [
        typeof(Binder),
        typeof(GetAttribute),
        typeof(System.Reactive.Unit),
        typeof(Enumerable),
        typeof(Newtonsoft.Json.JsonConvert),
        typeof(TestAttribute),
        typeof(HttpContent),
        typeof(Attribute)
    ];

    /// <summary>Verifies generator output for an interface body snippet, ignoring non-interface results.</summary>
    /// <param name="body">The interface member body source.</param>
    /// <returns>A task representing the verification.</returns>
    public static Task VerifyForBody(string body) => VerifyForBody(body, true);

    /// <summary>Verifies generator output for an interface body snippet.</summary>
    /// <param name="body">The interface member body source.</param>
    /// <param name="ignoreNonInterfaces">Whether to ignore non-interface generated results.</param>
    /// <returns>A task representing the verification.</returns>
    public static Task VerifyForBody(string body, bool ignoreNonInterfaces)
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

        return VerifyGenerator(source, ignoreNonInterfaces);
    }

    /// <summary>Verifies generator output for type declarations within a namespace.</summary>
    /// <param name="declarations">The type declarations source.</param>
    /// <returns>A task representing the verification.</returns>
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

    /// <summary>Verifies generator output for top-level declarations.</summary>
    /// <param name="declarations">The declarations source.</param>
    /// <returns>A task representing the verification.</returns>
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

    /// <summary>Creates a compilation from the given syntax trees with the required references.</summary>
    /// <param name="source">The syntax trees to compile.</param>
    /// <returns>The created compilation.</returns>
    public static CSharpCompilation CreateLibrary(params SyntaxTree[] source)
    {
        var references = new List<MetadataReference>();
        foreach (var assembly in GetAssemblyReferencesForCodegen())
        {
            if (!assembly.IsDynamic)
            {
                references.Add(MetadataReference.CreateFromFile(assembly.Location));
            }
        }

        references.Add(_refitAssembly);
        return CSharpCompilation.Create(
            "compilation",
            source,
            references,
            new(OutputKind.ConsoleApplication));
    }

    /// <summary>Gets the assemblies referenced when compiling generated code.</summary>
    /// <returns>The distinct, non-dynamic assemblies to reference.</returns>
    private static Assembly[] GetAssemblyReferencesForCodegen() =>
        [
            .. AppDomain.CurrentDomain
                .GetAssemblies()
                .Concat(_importantAssemblies.Select(x => x.Assembly))
                .Distinct()
                .Where(a => !a.IsDynamic)
        ];

    /// <summary>Creates a compilation by parsing the given source strings.</summary>
    /// <param name="source">The source strings to parse and compile.</param>
    /// <returns>The created compilation.</returns>
    private static CSharpCompilation CreateLibrary(params string[] source) =>
        CreateLibrary(source.Select(s => CSharpSyntaxTree.ParseText(s)).ToArray());

    /// <summary>Runs the generator over the source and returns the verification result.</summary>
    /// <param name="source">The source to compile and generate from.</param>
    /// <param name="ignoreNonInterfaces">Whether to ignore non-interface generated results.</param>
    /// <returns>A task producing the verification result.</returns>
    private static Task<VerifyResult> VerifyGenerator(string source, bool ignoreNonInterfaces = true)
    {
        var compilation = CreateLibrary(source);

        var generator = new InterfaceStubGeneratorV2();
        var driver = CSharpGeneratorDriver.Create(generator);

        var ranDriver = driver.RunGenerators(compilation);
        var settings = new VerifySettings();
        if (ignoreNonInterfaces)
        {
            settings.IgnoreGeneratedResult(x =>
                x.HintName.Contains("PreserveAttribute.g.cs", StringComparison.Ordinal));
            settings.IgnoreGeneratedResult(x => x.HintName.Contains("Generated.g.cs", StringComparison.Ordinal));
        }

        var verify = Verify(ranDriver, settings);
        return verify.ToTask();
    }
}
