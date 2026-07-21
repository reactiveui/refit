// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Refit.Generator;

namespace Refit.Benchmarks;

/// <summary>Shared setup for the source-generator benchmarks: builds a compilation and a generator driver.</summary>
internal static class GeneratorBenchmarkHarness
{
    /// <summary>The assembly name given to the throwaway compilation the generator runs against.</summary>
    private const string CompilationAssemblyName = "compilation";

    /// <summary>The metadata reference to the Refit assembly, including its XML documentation.</summary>
    private static readonly MetadataReference _refitAssembly = MetadataReference.CreateFromFile(
        typeof(GetAttribute).Assembly.Location,
        documentation: XmlDocumentationProvider.CreateFromFile(
            Path.ChangeExtension(typeof(GetAttribute).Assembly.Location, ".xml")));

    /// <summary>The assemblies whose presence the generated code depends on.</summary>
    private static readonly Type[] _importantAssemblies =
    [
        typeof(Binder),
        typeof(GetAttribute),
        typeof(Enumerable),
        typeof(Newtonsoft.Json.JsonConvert),
        typeof(HttpContent),
        typeof(Attribute)
    ];

    /// <summary>Creates a compilation for the source text and a generator driver for it.</summary>
    /// <param name="sourceText">The source text to compile and feed to the generator.</param>
    /// <returns>The compilation and generator driver.</returns>
    internal static (Compilation Compilation, CSharpGeneratorDriver Driver) Create(string sourceText)
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

        var compilation = CSharpCompilation.Create(
            CompilationAssemblyName,
            [CSharpSyntaxTree.ParseText(sourceText)],
            references,
            new(OutputKind.ConsoleApplication));

        var driver = CSharpGeneratorDriver.Create(
            new InterfaceStubGeneratorV2().AsSourceGenerator());
        return (compilation, driver);
    }

    /// <summary>Runs the generator once and primes the driver for an incremental (cached) re-run.</summary>
    /// <param name="sourceText">The source text to compile and feed to the generator.</param>
    /// <returns>The compilation and primed generator driver.</returns>
    internal static (Compilation Compilation, CSharpGeneratorDriver Driver) CreatePrimedForCachedRun(
        string sourceText)
    {
        var (compilation, driver) = Create(sourceText);
        driver = (CSharpGeneratorDriver)
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
        compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText("struct MyValue {}"));
        return (compilation, driver);
    }

    /// <summary>Gets the distinct, non-dynamic assemblies to reference when compiling the benchmark source.</summary>
    /// <returns>The distinct, non-dynamic assemblies to reference.</returns>
    private static Assembly[] GetAssemblyReferencesForCodegen()
    {
        var assemblies = new HashSet<Assembly>();
        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (var i = 0; i < loadedAssemblies.Length; i++)
        {
            if (!loadedAssemblies[i].IsDynamic)
            {
                _ = assemblies.Add(loadedAssemblies[i]);
            }
        }

        for (var i = 0; i < _importantAssemblies.Length; i++)
        {
            var assembly = _importantAssemblies[i].Assembly;
            if (!assembly.IsDynamic)
            {
                _ = assemblies.Add(assembly);
            }
        }

        var references = new Assembly[assemblies.Count];
        assemblies.CopyTo(references);
        return references;
    }
}
