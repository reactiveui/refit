// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Refit;

namespace Refit.Generator.Benchmarks;

/// <summary>Shared setup for the generator benchmarks: builds compilations, drivers, and parsed models.</summary>
internal static class GeneratorHarness
{
    /// <summary>The assembly name given to the throwaway compilation the generator runs against.</summary>
    private const string CompilationAssemblyName = "compilation";

    /// <summary>The metadata name of the Refit base HTTP method attribute.</summary>
    private const string HttpMethodAttributeMetadataName = "Refit.HttpMethodAttribute";

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

    /// <summary>Builds a compilation for the source text.</summary>
    /// <param name="sourceText">The source text to compile.</param>
    /// <returns>The compilation.</returns>
    internal static CSharpCompilation BuildCompilation(string sourceText)
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
            CompilationAssemblyName,
            [CSharpSyntaxTree.ParseText(sourceText)],
            references,
            new(OutputKind.ConsoleApplication));
    }

    /// <summary>Creates a compilation and a fresh (cold) generator driver for the source text.</summary>
    /// <param name="sourceText">The source text to compile and feed to the generator.</param>
    /// <returns>The compilation and generator driver.</returns>
    internal static (Compilation Compilation, CSharpGeneratorDriver Driver) CreateColdState(string sourceText)
    {
        var compilation = BuildCompilation(sourceText);
        var driver = CSharpGeneratorDriver.Create(new InterfaceStubGeneratorV2().AsSourceGenerator());
        return (compilation, driver);
    }

    /// <summary>Runs the generator once and primes the driver for an incremental (cached) re-run.</summary>
    /// <param name="sourceText">The source text to compile and feed to the generator.</param>
    /// <returns>The compilation and primed generator driver, with an unrelated type added to the compilation.</returns>
    internal static (Compilation Compilation, CSharpGeneratorDriver Driver) CreatePrimedState(string sourceText)
    {
        var (compilation, driver) = CreateColdState(sourceText);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

        // Add a trivial unrelated type: this mirrors an unrelated keystroke edit, so the re-run measures the
        // incremental pipeline (candidate re-collection + cache comparison), not a full cold re-parse.
        compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText("struct BenchUnrelated { }"));
        return (compilation, driver);
    }

    /// <summary>Collects the syntactic candidates the generator's providers would select from a compilation.</summary>
    /// <param name="compilation">The compilation to scan.</param>
    /// <returns>The candidate interface methods and candidate interfaces.</returns>
    internal static (ImmutableArray<MethodDeclarationSyntax> Methods, ImmutableArray<InterfaceDeclarationSyntax> Interfaces)
        CollectCandidates(CSharpCompilation compilation)
    {
        var methods = ImmutableArray.CreateBuilder<MethodDeclarationSyntax>();
        var interfaces = ImmutableArray.CreateBuilder<InterfaceDeclarationSyntax>();
        foreach (var tree in compilation.SyntaxTrees)
        {
            foreach (var node in tree.GetRoot().DescendantNodes())
            {
                switch (node)
                {
                    case MethodDeclarationSyntax { Parent: InterfaceDeclarationSyntax, AttributeLists.Count: > 0 } method:
                    {
                        methods.Add(method);
                        break;
                    }

                    case InterfaceDeclarationSyntax { BaseList: not null } iface:
                    {
                        interfaces.Add(iface);
                        break;
                    }
                }
            }
        }

        return (methods.ToImmutable(), interfaces.ToImmutable());
    }

    /// <summary>Runs the parser transform directly, returning the generation model.</summary>
    /// <param name="compilation">The compilation to parse.</param>
    /// <param name="candidates">The pre-collected syntactic candidates.</param>
    /// <returns>The context generation model produced by the parser.</returns>
    internal static ContextGenerationModel Parse(
        CSharpCompilation compilation,
        (ImmutableArray<MethodDeclarationSyntax> Methods, ImmutableArray<InterfaceDeclarationSyntax> Interfaces) candidates) =>
        Parser.GenerateInterfaceStubs(
            compilation,
            refitInternalNamespace: null,
            generatedRequestBuilding: true,
            emitGeneratedCodeMarkers: true,
            candidates.Methods,
            candidates.Interfaces,
            CancellationToken.None).contextGenerationSpec;

    /// <summary>Resolves the Refit base HTTP method attribute symbol for a compilation.</summary>
    /// <param name="compilation">The compilation to resolve against.</param>
    /// <returns>The resolved attribute symbol.</returns>
    internal static INamedTypeSymbol GetHttpMethodBaseAttributeSymbol(CSharpCompilation compilation) =>
        compilation.GetTypeByMetadataName(HttpMethodAttributeMetadataName)!;

    /// <summary>Resolves <c>System.IFormattable</c> for a compilation.</summary>
    /// <param name="compilation">The compilation to resolve against.</param>
    /// <returns>The resolved symbol, or null when unavailable.</returns>
    internal static INamedTypeSymbol? GetFormattableSymbol(CSharpCompilation compilation) =>
        compilation.GetTypeByMetadataName("System.IFormattable");

    /// <summary>Collects the Refit method symbols declared across a compilation.</summary>
    /// <param name="compilation">The compilation to scan.</param>
    /// <returns>The Refit method symbols in declaration order.</returns>
    internal static List<IMethodSymbol> GetRefitMethodSymbols(CSharpCompilation compilation)
    {
        var httpAttribute = GetHttpMethodBaseAttributeSymbol(compilation);
        var (methods, _) = CollectCandidates(compilation);
        var result = new List<IMethodSymbol>();
        foreach (var method in methods)
        {
            var model = compilation.GetSemanticModel(method.SyntaxTree);
            var symbol = model.GetDeclaredSymbol(method);
            if (Parser.IsRefitMethod(symbol, httpAttribute))
            {
                result.Add(symbol!);
            }
        }

        return result;
    }

    /// <summary>Gets the distinct, non-dynamic assemblies to reference when compiling the benchmark source.</summary>
    /// <returns>The distinct, non-dynamic assemblies to reference.</returns>
    private static Assembly[] GetAssemblyReferencesForCodegen()
    {
        var assemblies = new HashSet<Assembly>();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!assembly.IsDynamic)
            {
                _ = assemblies.Add(assembly);
            }
        }

        foreach (var marker in _importantAssemblies)
        {
            if (!marker.Assembly.IsDynamic)
            {
                _ = assemblies.Add(marker.Assembly);
            }
        }

        var references = new Assembly[assemblies.Count];
        assemblies.CopyTo(references);
        return references;
    }
}
