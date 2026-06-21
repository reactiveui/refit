// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Refit.Generator;

namespace Refit.GeneratorTests;

/// <summary>Focused tests for parser helper paths that are awkward to reach through snapshot tests.</summary>
public sealed class ParserCoverageTests
{
    /// <summary>Verifies parser argument validation.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GenerateInterfaceStubsRejectsNullCompilation()
    {
        await Assert.That(
                () => Parser.GenerateInterfaceStubs(
                    null!,
                    null,
                    generatedRequestBuilding: true,
                    emitGeneratedCodeMarkers: true,
                    [],
                    [],
                    CancellationToken.None))
            .ThrowsExactly<ArgumentNullException>();
    }

    /// <summary>Verifies parser diagnostics and namespace normalization when Refit is not referenced.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GenerateInterfaceStubsReportsMissingRefitReference()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("public interface IUnused { }");
        var compilation = CSharpCompilation.Create("no-refit", [syntaxTree]);

        var (diagnostics, model) = Parser.GenerateInterfaceStubs(
            compilation,
            "bad-name@thing-",
            generatedRequestBuilding: true,
            emitGeneratedCodeMarkers: true,
            [],
            [],
            CancellationToken.None);

        await Assert.That(diagnostics.Count).IsEqualTo(1);
        await Assert.That(model.RefitInternalNamespace).IsEqualTo("bad_name_thing_RefitInternalGenerated");
        await Assert.That(model.Interfaces).IsEmpty();
    }

    /// <summary>Verifies namespace normalization handles blank segments from MSBuild properties.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GenerateInterfaceStubsSanitizesInternalNamespaceSegments()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("public interface IUnused { }");
        var compilation = CSharpCompilation.Create("no-refit", [syntaxTree]);

        var (_, leadingDotModel) = Parser.GenerateInterfaceStubs(
            compilation,
            ".Application",
            generatedRequestBuilding: true,
            emitGeneratedCodeMarkers: true,
            [],
            [],
            CancellationToken.None);
        var (_, digitModel) = Parser.GenerateInterfaceStubs(
            compilation,
            "123-App",
            generatedRequestBuilding: true,
            emitGeneratedCodeMarkers: true,
            [],
            [],
            CancellationToken.None);
        var (_, dottedModel) = Parser.GenerateInterfaceStubs(
            compilation,
            "One.Two",
            generatedRequestBuilding: true,
            emitGeneratedCodeMarkers: true,
            [],
            [],
            CancellationToken.None);
        var (_, invalidStartModel) = Parser.GenerateInterfaceStubs(
            compilation,
            "!.Application",
            generatedRequestBuilding: true,
            emitGeneratedCodeMarkers: true,
            [],
            [],
            CancellationToken.None);

        await Assert.That(leadingDotModel.RefitInternalNamespace).IsEqualTo("ApplicationRefitInternalGenerated");
        await Assert.That(digitModel.RefitInternalNamespace).IsEqualTo("_123_AppRefitInternalGenerated");
        await Assert.That(dottedModel.RefitInternalNamespace).IsEqualTo("One.TwoRefitInternalGenerated");
        await Assert.That(invalidStartModel.RefitInternalNamespace).IsEqualTo("__.ApplicationRefitInternalGenerated");
    }

    /// <summary>Verifies well-known type lookup caching and failure paths.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task WellKnownTypesCachesResolvedAndMissingSymbols()
    {
        var compilation = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText("namespace Test { public sealed class Sample { } }"));
        var wellKnownTypes = new WellKnownTypes(compilation);

        var first = wellKnownTypes.Get(typeof(string));
        var second = wellKnownTypes.TryGet("System.String");
        var missingFirst = wellKnownTypes.TryGet("Missing.Type");
        var missingSecond = wellKnownTypes.TryGet("Missing.Type");
        var openGenericParameter = typeof(List<>).GetGenericArguments()[0];

        await Assert.That(second).IsSameReferenceAs(first);
        await Assert.That(missingFirst).IsNull();
        await Assert.That(missingSecond).IsNull();
        await Assert.That(() => wellKnownTypes.Get(null!)).ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => wellKnownTypes.Get(openGenericParameter)).ThrowsExactly<InvalidOperationException>();
    }

    /// <summary>Verifies parser and generator helper fallback paths that are easier to exercise directly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task InternalHelpersHandleFallbackPaths()
    {
        var emptyOptions = new DictionaryAnalyzerConfigOptions(new Dictionary<string, string>());
        var buildOptions = new DictionaryAnalyzerConfigOptions(
            new Dictionary<string, string>
            {
                ["build_property.RefitOption"] = "build",
                ["AnalyzerOption"] = "analyzer"
            });

        var defaultConstant = default(TypedConstant);

        await Assert.That(Parser.TryGetBodyBufferedValue(in defaultConstant, out var buffered)).IsFalse();
        await Assert.That(buffered).IsFalse();
        await Assert.That(InterfaceStubGeneratorV2.TryGetGlobalOption(emptyOptions, "Missing", out var missingValue)).IsFalse();
        await Assert.That(missingValue).IsNull();
        await Assert.That(InterfaceStubGeneratorV2.TryGetGlobalOption(buildOptions, "RefitOption", out var buildValue)).IsTrue();
        await Assert.That(buildValue).IsEqualTo("build");
        await Assert.That(InterfaceStubGeneratorV2.TryGetGlobalOption(buildOptions, "AnalyzerOption", out var analyzerValue)).IsTrue();
        await Assert.That(analyzerValue).IsEqualTo("analyzer");
    }

    /// <summary>Verifies type-symbol inheritance helpers with base classes and interfaces.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task TypeSymbolInheritanceHelpersHandleBaseClassesAndInterfaces()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(
            """
            namespace Symbols;
            public interface IMarker { }
            public class Base { }
            public class Derived : Base, IMarker { }
            public class Other { }
            """);
        var compilation = Fixture.CreateLibrary(syntaxTree);
        var derived = compilation.GetTypeByMetadataName("Symbols.Derived")!;
        var @base = compilation.GetTypeByMetadataName("Symbols.Base")!;
        var marker = compilation.GetTypeByMetadataName("Symbols.IMarker")!;
        var other = compilation.GetTypeByMetadataName("Symbols.Other")!;

        await Assert.That(derived.InheritsFromOrEquals(@base)).IsTrue();
        await Assert.That(derived.InheritsFromOrEquals(derived, includeInterfaces: true)).IsTrue();
        await Assert.That(derived.InheritsFromOrEquals(marker, includeInterfaces: true)).IsTrue();
        await Assert.That(derived.InheritsFromOrEquals(marker, includeInterfaces: false)).IsFalse();
        await Assert.That(derived.InheritsFromOrEquals(other, includeInterfaces: true)).IsFalse();
    }

    /// <summary>Verifies parser generation skips non-Refit methods while preserving Refit request metadata.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GenerateInterfaceStubsHandlesMixedRefitAndNonRefitMethods()
    {
        const int ExpectedMethodCount = 5;
        const int GenericApiResponseMethodIndex = 2;
        const int BodyMethodIndex = 3;
        const int GenericMethodIndex = 4;

        var syntaxTree = CSharpSyntaxTree.ParseText(
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                Task NoAttribute();

                [Get("/string")]
                Task<string> GetString();

                [Get("/api")]
                Task<IApiResponse> GetApiResponse();

                [Get("/generic-api")]
                Task<ApiResponse<int>> GetGenericApiResponse();

                [Post("/body")]
                Task Body([Body(BodySerializationMethod.Serialized, false)] string body);

                [Get("/generic")]
                Task Generic<TOne, TTwo>();
            }
            """);
        var root = await syntaxTree.GetRootAsync();
        var candidateMethods = root.DescendantNodes().OfType<MethodDeclarationSyntax>().ToImmutableArray();
        var candidateInterfaces = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().ToImmutableArray();
        var compilation = Fixture.CreateLibrary(syntaxTree);

        var (_, model) = Parser.GenerateInterfaceStubs(
            compilation,
            "mixed",
            generatedRequestBuilding: true,
            emitGeneratedCodeMarkers: true,
            candidateMethods,
            candidateInterfaces,
            CancellationToken.None);

        var methods = model.Interfaces.AsArray()[0].RefitMethods.AsArray();

        await Assert.That(methods.Length).IsEqualTo(ExpectedMethodCount);
        await Assert.That(methods[0].Name).IsEqualTo("GetString");
        await Assert.That(methods[1].Request.IsApiResponse).IsTrue();
        await Assert.That(methods[GenericApiResponseMethodIndex].Request.DeserializedResultType).IsEqualTo("int");
        await Assert.That(methods[BodyMethodIndex].Request.Parameters.AsArray()[0].BodyBufferMode)
            .IsEqualTo(BodyBufferMode.Streaming);
        await Assert.That(methods[GenericMethodIndex].DeclaredMethod).IsEqualTo("Generic<TOne, TTwo>");
    }

    /// <summary>Verifies parser generation under a language version that does not support nullable directives.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GenerateInterfaceStubsUsesNoNullabilityWhenLanguageVersionDoesNotSupportIt()
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7_3);
        var syntaxTree = CSharpSyntaxTree.ParseText(
            """
            using System.Threading.Tasks;
            using Refit;

            public interface ILegacyClient
            {
                [Get("/legacy")]
                Task<string> Get();
            }
            """,
            parseOptions);
        var root = await syntaxTree.GetRootAsync();
        var candidateMethods = root.DescendantNodes().OfType<MethodDeclarationSyntax>().ToImmutableArray();
        var candidateInterfaces = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().ToImmutableArray();
        var compilation = Fixture.CreateLibrary(syntaxTree);

        var (_, model) = Parser.GenerateInterfaceStubs(
            compilation,
            "legacy",
            generatedRequestBuilding: true,
            emitGeneratedCodeMarkers: true,
            candidateMethods,
            candidateInterfaces,
            CancellationToken.None);

        await Assert.That(model.Interfaces.AsArray()[0].Nullability).IsEqualTo(Nullability.None);
    }

    /// <summary>Analyzer config options backed by a dictionary for direct helper tests.</summary>
    /// <param name="values">The option values.</param>
    private sealed class DictionaryAnalyzerConfigOptions(IReadOnlyDictionary<string, string> values)
        : AnalyzerConfigOptions
    {
        /// <inheritdoc/>
        public override bool TryGetValue(string key, out string value) => values.TryGetValue(key, out value!);
    }
}
