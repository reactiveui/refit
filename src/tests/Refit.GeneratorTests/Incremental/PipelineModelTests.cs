// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Refit.Generator;

namespace Refit.GeneratorTests.Incremental;

/// <summary>Verifies semantic extraction leaves stable values in the incremental pipeline.</summary>
public class PipelineModelTests
{
    /// <summary>The interface used to inspect the tracked pipeline.</summary>
    private const string InterfaceSource = """
        using System.Threading.Tasks;
        using Refit;
        public interface IApi
        {
            [Get("/items/{id}")]
            Task<string> Get(int id);
        }
        """;

    /// <summary>Verifies cached model inputs do not retain a compilation, semantic model, symbol or syntax node.</summary>
    /// <param name="stepName">The tracked stage whose cached values are inspected.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments("ParserInputs")]
    [Arguments("ParseInterfaces")]
    [Arguments("BuildRefit")]
    public async Task CachedModels_DoNotRetainCompilerState(string stepName)
    {
        var compilation = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(InterfaceSource));
        var driver = TestHelper.GenerateTracked(compilation);
        var inspection = compilation.AddReferences(CreateGeneratorReference())
            .WithOptions(compilation.Options.WithMetadataImportOptions(MetadataImportOptions.All));
        var outputs = driver.GetRunResult().Results[0].TrackedSteps[stepName]
            .SelectMany(static step => step.Outputs).ToArray();

        await Assert.That(outputs).IsNotEmpty();
        foreach (var output in outputs)
        {
            await Assert.That(ContainsCompilerState(output.Value, inspection)).IsFalse();
        }
    }

    /// <summary>Verifies semantic transforms return only compiler-independent values.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SemanticExtraction_DoesNotRetainCompilerState()
    {
        var compilation = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(InterfaceSource));
        var driver = TestHelper.GenerateTracked(compilation);
        var inspection = compilation.AddReferences(CreateGeneratorReference())
            .WithOptions(compilation.Options.WithMetadataImportOptions(MetadataImportOptions.All));
        var steps = driver.GetRunResult().Results[0].TrackedSteps
            .Where(static step => step.Key.StartsWith("Extract", StringComparison.Ordinal)).ToArray();

        await Assert.That(steps).IsNotEmpty();
        foreach (var step in steps)
        {
            foreach (var output in step.Value.SelectMany(static run => run.Outputs))
            {
                await Assert.That(ContainsCompilerState(output.Value, inspection)).IsFalse();
            }
        }
    }

    /// <summary>Verifies an unrelated edit does not invalidate semantic extraction's cached output.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ParseInterfaces_UnrelatedEdit_OutputIsUnchanged()
    {
        var compilation = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(InterfaceSource));
        var driver = TestHelper.GenerateTracked(compilation);
        var updated = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText("public class Unrelated { }"));
        driver = driver.RunGenerators(updated);
        var outputs = driver.GetRunResult().Results[0].TrackedSteps["ParseInterfaces"]
            .SelectMany(static step => step.Outputs).ToArray();

        await Assert.That(outputs).IsNotEmpty();
        foreach (var output in outputs)
        {
            await Assert.That(output.Reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged).IsTrue();
        }
    }

    /// <summary>Verifies contract errors are not reported or cached by source generation.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ContractDiagnostics_AreNotInTheGeneratorPipeline()
    {
        var compilation = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText("""
            using System.Threading.Tasks;
            using Refit;
            public interface IApi
            {
                [Get("/items")]
                [Paged]
                Task<string> Get();
            }
            """));
        var result = TestHelper.GenerateTracked(compilation).GetRunResult().Results[0];

        await Assert.That(result.Diagnostics).IsEmpty();
        await Assert.That(result.TrackedSteps.ContainsKey("ReportDiagnostics")).IsFalse();
    }

    /// <summary>Verifies an undiscovered custom attribute cannot block standard attributes later in a tree.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SemanticExtraction_CustomAttributeWithStandardName_DoesNotBlockStandardInterface()
    {
        var compilation = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText("""
            using System.Threading.Tasks;
            public interface ICustomApi
            {
                [Custom.Get("/custom")]
                Task<string> Get();
            }
            public interface IStandardApi
            {
                [Refit.Get("/standard")]
                Task<string> Get();
            }
            namespace Custom
            {
                public sealed class GetAttribute : Refit.HttpMethodAttribute
                {
                    public GetAttribute(string path) : base(path) { }
                    public override System.Net.Http.HttpMethod Method => System.Net.Http.HttpMethod.Get;
                }
            }
            """));
        var result = TestHelper.GenerateTracked(compilation).GetRunResult().Results[0];

        await Assert.That(result.GeneratedSources.Any(static source => source.HintName == "IStandardApi.g.cs")).IsTrue();
    }

    /// <summary>References the generator's metadata from the test output directory.</summary>
    /// <returns>The generator metadata reference.</returns>
    private static PortableExecutableReference CreateGeneratorReference() =>
        MetadataReference.CreateFromFile(Path.Combine(
            AppContext.BaseDirectory,
            $"{typeof(InterfaceStubGeneratorV2).Assembly.GetName().Name}.dll"));

    /// <summary>Checks cached data members without inspecting diagnostic locations or framework implementation state.</summary>
    /// <param name="value">The cached value.</param>
    /// <param name="compilation">The metadata inspection compilation.</param>
    /// <returns>Whether compiler state was retained.</returns>
    private static bool ContainsCompilerState(object? value, Compilation compilation)
    {
        if (value is null or string)
        {
            return false;
        }

        if (value is IEnumerable sequence)
        {
            foreach (var item in sequence)
            {
                if (ContainsCompilerState(item, compilation))
                {
                    return true;
                }
            }

            return false;
        }

        var type = compilation.GetTypeByMetadataName(value.GetType().FullName!);
        return type is not null && ContainsCompilerType(type, [with(SymbolEqualityComparer.Default)]);
    }

    /// <summary>Walks the cached value's type graph through Roslyn rather than runtime reflection.</summary>
    /// <param name="type">The value's metadata type.</param>
    /// <param name="visited">The types already inspected.</param>
    /// <returns>Whether the type graph contains retained compiler state.</returns>
    private static bool ContainsCompilerType(ITypeSymbol type, HashSet<ISymbol> visited)
    {
        if (!visited.Add(type))
        {
            return false;
        }

        if (type is IArrayTypeSymbol array)
        {
            return ContainsCompilerType(array.ElementType, visited);
        }

        if (type is not INamedTypeSymbol named)
        {
            return false;
        }

        if (IsCompilerType(named))
        {
            return true;
        }

        foreach (var argument in named.TypeArguments)
        {
            if (ContainsCompilerType(argument, visited))
            {
                return true;
            }
        }

        if (named.ContainingAssembly.Name != typeof(InterfaceStubGeneratorV2).Assembly.GetName().Name)
        {
            return false;
        }

        return ContainsCompilerMembers(named, visited);
    }

    /// <summary>Inspects the cached model's instance properties and fields.</summary>
    /// <param name="named">The cached model type.</param>
    /// <param name="visited">The types already inspected.</param>
    /// <returns>Whether an instance member retains compiler state.</returns>
    private static bool ContainsCompilerMembers(INamedTypeSymbol named, HashSet<ISymbol> visited)
    {
        foreach (var member in named.GetMembers().OfType<IPropertySymbol>())
        {
            if (!member.IsStatic && ContainsCompilerType(member.Type, visited))
            {
                return true;
            }
        }

        foreach (var member in named.GetMembers().OfType<IFieldSymbol>())
        {
            if (!member.IsStatic && ContainsCompilerType(member.Type, visited))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Identifies Roslyn compilation graphs, semantic models, symbols and syntax nodes.</summary>
    /// <param name="type">The metadata type to classify.</param>
    /// <returns>Whether the type retains compiler state.</returns>
    private static bool IsCompilerType(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (IsCompilerStateDefinition(current))
            {
                return true;
            }
        }

        return type.AllInterfaces.Any(static iface => iface.Name == nameof(ISymbol)
            && iface.ContainingNamespace.ToDisplayString() == "Microsoft.CodeAnalysis");
    }

    /// <summary>Identifies the Roslyn types that own compiler state.</summary>
    /// <param name="type">The metadata type to classify.</param>
    /// <returns>Whether the type is a compiler-state definition.</returns>
    private static bool IsCompilerStateDefinition(INamedTypeSymbol type) =>
        type.ContainingNamespace.ToDisplayString() == "Microsoft.CodeAnalysis"
        && type.Name is nameof(Compilation) or nameof(SemanticModel) or nameof(SyntaxNode) or nameof(ISymbol)
            or nameof(Diagnostic) or nameof(Location) or nameof(SyntaxTree) or nameof(SyntaxReference);
}
