// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Refit.GeneratorTests;

/// <summary>Verifies the generated helper types are named per assembly, so two assemblies linked by
/// <c>[InternalsVisibleTo]</c> no longer emit identically named internal types that the second compilation
/// sees twice and reports as conflicting (issue #2254), while the runtime reflection resolution still matches
/// the container name the generator emits.</summary>
public sealed class InternalsVisibleToGenerationTests
{
    /// <summary>The compiler diagnostic raised when a source type conflicts with an imported type of the same name.</summary>
    private const string ConflictingImportedTypeDiagnosticId = "CS0436";

    /// <summary>Verifies a downstream assembly that can see an upstream assembly's Refit internals through
    /// <c>[InternalsVisibleTo]</c> generates its own client without a type-conflict diagnostic.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GeneratedHelperTypesDoNotConflictAcrossInternalsVisibleToBoundary()
    {
        const string upstream =
            """
            using System.Runtime.CompilerServices;
            using System.Threading.Tasks;
            using Refit;

            [assembly: InternalsVisibleTo("Downstream")]

            namespace Upstream;

            public interface IUpstreamApi
            {
                [Get("/upstream")]
                Task<string> GetUpstream();
            }
            """;

        // The upstream assembly exposes its internal generated helpers to "Downstream", so the downstream compile
        // imports them. Before the fix, both assemblies emitted Refit.Implementation.Generated and
        // RefitInternalGenerated.PreserveAttribute, and the downstream compile saw its own copy plus the imported
        // one, raising CS0436.
        var upstreamCompilation = Fixture.CreateNamedLibrary("Upstream", CSharpSyntaxTree.ParseText(upstream));
        var upstreamResult = Fixture.RunGenerator(upstreamCompilation, generatedRequestBuilding: true, false, null);
        var upstreamReference = upstreamResult.OutputCompilation.ToMetadataReference();

        const string downstream =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace Downstream;

            public interface IDownstreamApi
            {
                [Get("/downstream")]
                Task<string> GetDownstream();
            }
            """;

        var downstreamCompilation = Fixture
            .CreateNamedLibrary("Downstream", CSharpSyntaxTree.ParseText(downstream))
            .AddReferences(upstreamReference);
        var downstreamResult = Fixture.RunGenerator(downstreamCompilation, generatedRequestBuilding: true, false, null);

        var conflicts = downstreamResult.OutputCompilation
            .GetDiagnostics()
            .Where(static diagnostic => diagnostic.Id == ConflictingImportedTypeDiagnosticId)
            .Select(static diagnostic => diagnostic.ToString())
            .ToArray();

        await Assert.That(conflicts).IsEmpty();
    }

    /// <summary>Verifies the runtime reconstructs the exact container name the generator emitted, so the reflection
    /// resolution path for a generic interface still finds the generated type after the rename.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [RequiresUnreferencedCode("Loads an emitted test assembly to resolve a generated type by reflection.")]
    [RequiresDynamicCode("Closes a generic interface with MakeGenericType to reconstruct the generated container name.")]
    public async Task ReflectionResolutionMatchesGeneratedContainerNameForGenericInterface()
    {
        const string source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace ReflectionProbe;

            public interface IGenericProbeApi<T>
            {
                [Get("/probe")]
                Task<T> Fetch();
            }
            """;

        // The dotted assembly name also exercises the identifier sanitization: the dot becomes an underscore in the
        // container name, and the runtime must apply the same mapping for the reflection lookup to line up.
        var compilation = Fixture.CreateNamedLibrary("Reflection.Probe", CSharpSyntaxTree.ParseText(source));
        var result = Fixture.RunGenerator(compilation, generatedRequestBuilding: true, false, null);
        var (assembly, context) = Fixture.EmitAndLoad(result);
        using (context)
        {
            var interfaceType = assembly.GetType("ReflectionProbe.IGenericProbeApi`1", throwOnError: true)!;
            var closedInterface = interfaceType.MakeGenericType(typeof(string));

            // UniqueName.ForType produces the exact assembly-qualified string the reflection path feeds to
            // Type.GetType, so its container-definition portion must name a real type in the emitted assembly.
            var uniqueName = UniqueName.ForType(closedInterface);
            var containerDefinition = uniqueName.Substring(0, uniqueName.IndexOf('['));
            var resolved = assembly.GetType(containerDefinition, throwOnError: false);

            await Assert.That(resolved).IsNotNull();
            await Assert.That(containerDefinition)
                .Contains("Refit.Implementation.GeneratedReflection_Probe+");
        }
    }

    /// <summary>Verifies an unnamed compilation falls back to the historical single-assembly container name, so
    /// the generated output still compiles when no assembly name is available.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UnnamedCompilationEmitsUnscopedContainerName()
    {
        const string source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace UnnamedProbe;

            public interface IUnnamedApi
            {
                [Get("/unnamed")]
                Task<string> GetUnnamed();
            }
            """;

        var compilation = Fixture.CreateNamedLibrary((string?)null, CSharpSyntaxTree.ParseText(source));
        var result = Fixture.RunGenerator(compilation, generatedRequestBuilding: true, false, null);

        // Each generated source keeps its own line endings (CRLF on Windows), so normalize to LF before asserting
        // against a newline-anchored needle; otherwise the "...Generated\n" match fails on Windows runners.
        var generated = string.Join("\n", result.GeneratedSources.Values).Replace("\r\n", "\n");

        await Assert.That(generated).Contains("typeof(global::Refit.Implementation.Generated))");
        await Assert.That(generated).Contains("namespace RefitInternalGenerated\n");
    }
}
