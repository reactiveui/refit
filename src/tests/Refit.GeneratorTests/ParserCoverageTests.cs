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
    /// <summary>A minimal interface source with no Refit methods.</summary>
    private const string UnusedInterfaceSource = "public interface IUnused { }";

    /// <summary>The metadata name of the Refit HTTP method base attribute.</summary>
    private const string HttpMethodAttributeMetadataName = "Refit.HttpMethodAttribute";

    /// <summary>The metadata name of <c>System.IFormattable</c>.</summary>
    private const string FormattableMetadataName = "System.IFormattable";

    /// <summary>The metadata name of the sample interface used by inline-eligibility tests.</summary>
    private const string SampleApiMetadataName = "RefitGeneratorTest.IApi";

    /// <summary>Verifies parser argument validation.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GenerateInterfaceStubsRejectsNullCompilation() =>
        await Assert.That(
                static () => Parser.GenerateInterfaceStubs(
                    null!,
                    null,
                    generatedRequestBuilding: true,
                    emitGeneratedCodeMarkers: true,
                    [],
                    [],
                    CancellationToken.None))
            .ThrowsExactly<ArgumentNullException>();

    /// <summary>Verifies parser diagnostics and namespace normalization when Refit is not referenced.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GenerateInterfaceStubsReportsMissingRefitReference()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(UnusedInterfaceSource);
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
        var syntaxTree = CSharpSyntaxTree.ParseText(UnusedInterfaceSource);
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

        // A constructed generic type's FullName is not resolvable by metadata name, so Get reports the missing type.
        await Assert.That(() => wellKnownTypes.Get(typeof(List<int>))).ThrowsExactly<InvalidOperationException>();
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

    /// <summary>Verifies inline eligibility is denied for a method with no HTTP method attribute.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CanBuildRequestInlineRejectsMethodWithoutHttpAttribute()
    {
        var compilation = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(
            """
            using System.Threading.Tasks;
            using Refit;
            namespace RefitGeneratorTest;
            public interface IApi { Task<string> Plain(); }
            """));
        var httpBase = compilation.GetTypeByMetadataName(HttpMethodAttributeMetadataName)!;
        var formattable = compilation.GetTypeByMetadataName(FormattableMetadataName);
        var method = compilation.GetTypeByMetadataName(SampleApiMetadataName)!
            .GetMembers("Plain").OfType<IMethodSymbol>().First();

        await Assert.That(Parser.CanBuildRequestInline(method, httpBase, formattable)).IsFalse();
    }

    /// <summary>Verifies inline return-shape classification for non-named and plain named return types.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CanBuildRequestInlineClassifiesNonNamedAndNamedReturnShapes()
    {
        var compilation = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(
            """
            using Refit;
            namespace RefitGeneratorTest;
            public interface IApi
            {
                [Get("/a")] int[] ArrayReturn();
                [Get("/b")] string StringReturn();
            }
            """));
        var httpBase = compilation.GetTypeByMetadataName(HttpMethodAttributeMetadataName)!;
        var formattable = compilation.GetTypeByMetadataName(FormattableMetadataName);
        var api = compilation.GetTypeByMetadataName(SampleApiMetadataName)!;
        var arrayReturn = api.GetMembers("ArrayReturn").OfType<IMethodSymbol>().First();
        var stringReturn = api.GetMembers("StringReturn").OfType<IMethodSymbol>().First();

        // Neither is inline-eligible; the point is that classifying the non-named (array) and the plain named
        // (string) declared result type both run here.
        await Assert.That(Parser.CanBuildRequestInline(arrayReturn, httpBase, formattable)).IsFalse();
        await Assert.That(Parser.CanBuildRequestInline(stringReturn, httpBase, formattable)).IsFalse();
    }

    /// <summary>Verifies adapter discovery returns nothing when the adapter interface is unresolved.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DiscoverReturnTypeAdaptersReturnsEmptyWhenInterfaceUnresolved()
    {
        var compilation = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(UnusedInterfaceSource));

        var adapters = Parser.DiscoverReturnTypeAdapters(compilation, null, CancellationToken.None);

        await Assert.That(adapters).IsEmpty();
    }

    /// <summary>Verifies the declared base name strips an explicit interface qualifier.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BuildDeclaredBaseNameStripsExplicitInterfaceQualifier()
    {
        var compilation = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(
            """
            namespace Explicit;
            public interface IFoo { System.Threading.Tasks.Task Bar(); }
            public class Impl : IFoo
            {
                System.Threading.Tasks.Task IFoo.Bar() => System.Threading.Tasks.Task.CompletedTask;
            }
            """));
        var explicitImplementation = compilation.GetTypeByMetadataName("Explicit.Impl")!
            .GetMembers()
            .OfType<IMethodSymbol>()
            .First(static method => method.Name.Contains('.'));

        await Assert.That(Parser.BuildDeclaredBaseName(explicitImplementation)).IsEqualTo("Bar");
    }

    /// <summary>Verifies the Refit-method predicate rejects a null method symbol.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task IsRefitMethodReturnsFalseForNullMethod()
    {
        var compilation = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(UnusedInterfaceSource));
        var httpMethodBase = compilation.GetTypeByMetadataName(HttpMethodAttributeMetadataName)!;

        await Assert.That(Parser.IsRefitMethod(null, httpMethodBase)).IsFalse();
    }

    /// <summary>Verifies the inline return-shape classifier resolves the <c>IAsyncEnumerable&lt;T&gt;</c> shape.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CanBuildRequestInlineClassifiesAsyncEnumerableReturnShape()
    {
        var compilation = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(
            """
            using System.Collections.Generic;
            using Refit;
            namespace RefitGeneratorTest;
            public interface IApi { [Get("/a")] IAsyncEnumerable<string> Enumerate(); }
            """));
        var httpBase = compilation.GetTypeByMetadataName(HttpMethodAttributeMetadataName)!;
        var formattable = compilation.GetTypeByMetadataName(FormattableMetadataName);
        var enumerate = compilation.GetTypeByMetadataName(SampleApiMetadataName)!
            .GetMembers("Enumerate").OfType<IMethodSymbol>().First();

        await Assert.That(Parser.CanBuildRequestInline(enumerate, httpBase, formattable)).IsTrue();
    }

    /// <summary>Verifies interface collection captures a child that only inherits Refit methods and skips a candidate
    /// interface with no Refit methods, while a property member on the child is ignored during base-method exclusion.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CollectRefitInterfacesHandlesInheritedAndEmptyCandidates()
    {
        const int ExpectedInterfaceCount = 2;
        var syntaxTree = CSharpSyntaxTree.ParseText(
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IParent
            {
                [Get("/p")]
                Task<string> P();

                void NonRefitBase();
            }

            public interface IChild : IParent
            {
                string Prop { get; }
            }

            public interface IEmpty
            {
                void Plain();
            }
            """);
        var root = await syntaxTree.GetRootAsync();
        var candidateMethods = root.DescendantNodes().OfType<MethodDeclarationSyntax>().ToImmutableArray();
        var candidateInterfaces = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().ToImmutableArray();
        var compilation = Fixture.CreateLibrary(syntaxTree);

        var (_, model) = Parser.GenerateInterfaceStubs(
            compilation,
            "inherited",
            generatedRequestBuilding: true,
            emitGeneratedCodeMarkers: true,
            candidateMethods,
            candidateInterfaces,
            CancellationToken.None);

        var interfaceNames = model.Interfaces.AsArray().Select(static i => i.InterfaceDisplayName).ToArray();

        // IParent (declares a Refit method) and IChild (inherits one); IEmpty has none and is dropped.
        await Assert.That(model.Interfaces.AsArray().Length).IsEqualTo(ExpectedInterfaceCount);
        await Assert.That(interfaceNames).Contains(static name => name.Contains("IChild", StringComparison.Ordinal));
        await Assert.That(interfaceNames).DoesNotContain(static name => name.Contains("IEmpty", StringComparison.Ordinal));
    }

    /// <summary>Verifies the inline return-shape classifier resolves every recognized async wrapper and treats a
    /// same-named type in a foreign namespace as a plain return shape.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CanBuildRequestInlineClassifiesAsyncReturnShapesAndForeignLookalikes()
    {
        var compilation = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Refit;

            namespace Foreign
            {
                public sealed class Task { }
                public sealed class ValueTask<T> { }
                public interface IAsyncEnumerable<T> { }
                public interface IObservable<T> { }
            }

            namespace RefitGeneratorTest
            {
                public interface IApi
                {
                    [Get("/a")] System.Threading.Tasks.Task AsyncVoid();
                    [Get("/b")] System.Threading.Tasks.ValueTask<string> ValueResult();
                    [Get("/c")] System.IObservable<string> Observe();
                    [Get("/d")] Foreign.Task ForeignTask();
                    [Get("/e")] Foreign.ValueTask<string> ForeignValueTask();
                    [Get("/f")] Foreign.IAsyncEnumerable<string> ForeignAsyncEnumerable();
                    [Get("/g")] Foreign.IObservable<string> ForeignObservable();
                }
            }
            """));
        var httpBase = compilation.GetTypeByMetadataName(HttpMethodAttributeMetadataName)!;
        var formattable = compilation.GetTypeByMetadataName(FormattableMetadataName);
        var api = compilation.GetTypeByMetadataName(SampleApiMetadataName)!;

        static IMethodSymbol Method(INamedTypeSymbol type, string name) =>
            type.GetMembers(name).OfType<IMethodSymbol>().First();

        await Assert.That(Parser.CanBuildRequestInline(Method(api, "AsyncVoid"), httpBase, formattable)).IsTrue();
        await Assert.That(Parser.CanBuildRequestInline(Method(api, "ValueResult"), httpBase, formattable)).IsTrue();
        await Assert.That(Parser.CanBuildRequestInline(Method(api, "Observe"), httpBase, formattable)).IsTrue();
        await Assert.That(Parser.CanBuildRequestInline(Method(api, "ForeignTask"), httpBase, formattable)).IsFalse();
        await Assert.That(Parser.CanBuildRequestInline(Method(api, "ForeignValueTask"), httpBase, formattable)).IsFalse();
        await Assert.That(Parser.CanBuildRequestInline(Method(api, "ForeignAsyncEnumerable"), httpBase, formattable)).IsFalse();
        await Assert.That(Parser.CanBuildRequestInline(Method(api, "ForeignObservable"), httpBase, formattable)).IsFalse();
    }

    /// <summary>Verifies the interface nullable-context flag is set from the compilation-level nullable option even when
    /// the method's own span reports a disabled context.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GenerateInterfaceStubsHonorsCompilationLevelNullableContext()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Get("/a")]
                Task<string> Get();
            }
            """);
        var root = await syntaxTree.GetRootAsync();
        var candidateMethods = root.DescendantNodes().OfType<MethodDeclarationSyntax>().ToImmutableArray();
        var candidateInterfaces = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().ToImmutableArray();
        var compilation = (CSharpCompilation)Fixture.CreateLibrary(syntaxTree)
            .WithOptions(new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));

        var (_, model) = Parser.GenerateInterfaceStubs(
            compilation,
            "nullable",
            generatedRequestBuilding: true,
            emitGeneratedCodeMarkers: true,
            candidateMethods,
            candidateInterfaces,
            CancellationToken.None);

        await Assert.That(model.Interfaces.AsArray()[0].Nullability).IsNotEqualTo(Nullability.None);
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
