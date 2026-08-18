// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Refit.Generator;

namespace Refit.GeneratorTests;

/// <summary>Direct unit tests for the source generator parser request helpers.</summary>
public static partial class GeneratorComponentTests
{
    /// <summary>Tests for direct parser request helpers.</summary>
    public class ParserRequestHelperTests
    {
        /// <summary>The simple path used by parser helper assertions.</summary>
        private const string SimplePath = "/path";

        /// <summary>The shared prefix used by the path-prefix join assertions.</summary>
        private const string PrefixRoute = "/api/v2";

        /// <summary>A route template used by the path-prefix join assertions.</summary>
        private const string UsersRoute = "/users";

        /// <summary>The expected result of joining <see cref="PrefixRoute"/> with <see cref="UsersRoute"/>.</summary>
        private const string PrefixedUsers = "/api/v2/users";

        /// <summary>The number of characters checked in whitespace assertions.</summary>
        private const int WhitespaceLength = 2;

        /// <summary>The enum value for URL encoded body serialization.</summary>
        private const int UrlEncodedSerializationValue = 2;

        /// <summary>The enum value for serialized body serialization.</summary>
        private const int SerializedSerializationValue = 3;

        /// <summary>The enum value for JSON Lines body serialization.</summary>
        private const int JsonLinesSerializationValue = 4;

        /// <summary>An unsupported body serialization enum value.</summary>
        private const int UnsupportedSerializationValue = 99;

        /// <summary>The name of the member each mapping helper falls back to.</summary>
        private const string DefaultMemberName = "Default";

        /// <summary>The enum value for gzip request compression.</summary>
        private const int GZipCompressionValue = 2;

        /// <summary>The enum value for Brotli request compression.</summary>
        private const int BrotliCompressionValue = 3;

        /// <summary>The enum value for Zstandard request compression.</summary>
        private const int ZstandardCompressionValue = 4;

        /// <summary>The enum value for the level that stores without compressing.</summary>
        private const int NoCompressionLevelValue = 2;

        /// <summary>The enum value for the smallest-size compression level.</summary>
        private const int SmallestSizeLevelValue = 3;

        /// <summary>An enum value outside every member the generator knows.</summary>
        private const int UnrecognizedEnumValue = 99;

        /// <summary>Verifies inline path normalization and constant path classification.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task InlinePathHelpers_NormalizeAndClassifyPaths()
        {
            await Assert.That(Parser.NormalizeConstantPathForInline(SimplePath)).IsEqualTo(SimplePath);
            await Assert.That(Parser.NormalizeConstantPathForInline("/path?")).IsEqualTo(SimplePath);
            await Assert.That(Parser.NormalizeConstantPathForInline("/path?& \t =drop")).IsEqualTo(SimplePath);
            await Assert.That(Parser.NormalizeConstantPathForInline("/path?one=1&&two=2#fragment")).IsEqualTo("/path?one=1&two=2");
            await Assert.That(Parser.IsPathSupported(string.Empty)).IsTrue();
            await Assert.That(Parser.IsPathSupported(SimplePath)).IsTrue();

            // A no-leading-slash path is supported: it resolves against the base under RFC 3986 and throws under legacy.
            await Assert.That(Parser.IsPathSupported("relative")).IsTrue();
            await Assert.That(Parser.IsPathSupported("/{id}")).IsTrue();
            await Assert.That(Parser.IsPathSupported("/id}")).IsFalse();
            await Assert.That(Parser.IsPathSupported("/line\nbreak")).IsFalse();
            await Assert.That(Parser.IsPathSupported("/line\rbreak")).IsFalse();
            await Assert.That(Parser.IsWhiteSpace(" \t", 0, WhitespaceLength)).IsTrue();
            await Assert.That(Parser.IsWhiteSpace(" a", 0, WhitespaceLength)).IsFalse();
        }

        /// <summary>Verifies the interface path-prefix join normalizes slashes and treats an empty prefix as a no-op.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task CombinePathPrefix_NormalizesSlashesAndNoOpsEmptyPrefix()
        {
            // An empty or whitespace prefix is a no-op.
            await Assert.That(Parser.CombinePathPrefix(string.Empty, UsersRoute)).IsEqualTo(UsersRoute);
            await Assert.That(Parser.CombinePathPrefix("   ", UsersRoute)).IsEqualTo(UsersRoute);

            // A prefix made only of slashes collapses to a no-op.
            await Assert.That(Parser.CombinePathPrefix("/", UsersRoute)).IsEqualTo(UsersRoute);

            // Exactly one slash joins the prefix and route regardless of the slashes either side carries.
            await Assert.That(Parser.CombinePathPrefix(PrefixRoute, UsersRoute)).IsEqualTo(PrefixedUsers);
            await Assert.That(Parser.CombinePathPrefix("/api/v2/", UsersRoute)).IsEqualTo(PrefixedUsers);
            await Assert.That(Parser.CombinePathPrefix(PrefixRoute, "users")).IsEqualTo(PrefixedUsers);
            await Assert.That(Parser.CombinePathPrefix("api/v2", "users")).IsEqualTo("api/v2/users");

            // An empty route collapses to the trimmed prefix.
            await Assert.That(Parser.CombinePathPrefix("/api/v2/", "/")).IsEqualTo(PrefixRoute);
            await Assert.That(Parser.CombinePathPrefix(PrefixRoute, string.Empty)).IsEqualTo(PrefixRoute);
        }

        /// <summary>Verifies static header merging behavior.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task AddStaticHeader_SkipsBlankAndReplacesExistingValues()
        {
            const int ExpectedHeaderCount = 2;
            var headers = new List<HeaderModel>();

            Parser.AddStaticHeader(headers, " ");
            Parser.AddStaticHeader(headers, "X-One");
            Parser.AddStaticHeader(headers, "X-Two: two");
            Parser.AddStaticHeader(headers, "X-One: replaced");

            await Assert.That(headers.Count).IsEqualTo(ExpectedHeaderCount);
            await Assert.That(headers[0].Name).IsEqualTo("X-One");
            await Assert.That(headers[0].Value).IsEqualTo("replaced");
            await Assert.That(headers[1].Name).IsEqualTo("X-Two");
            await Assert.That(headers[1].Value).IsEqualTo("two");
        }

        /// <summary>Verifies body serialization, inline-body eligibility, and response disposal helpers.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task BodyAndDisposalHelpers_ClassifySupportedValues()
        {
            await Assert.That(Parser.GetBodySerializationMethodName(0)).IsEqualTo(DefaultMemberName);
            await Assert.That(Parser.GetBodySerializationMethodName(1)).IsEqualTo("Json");
            await Assert.That(Parser.GetBodySerializationMethodName(UrlEncodedSerializationValue)).IsEqualTo(UrlEncodedSerializationMethod);
            await Assert.That(Parser.GetBodySerializationMethodName(SerializedSerializationValue)).IsEqualTo("Serialized");
            await Assert.That(Parser.GetBodySerializationMethodName(JsonLinesSerializationValue)).IsEqualTo("JsonLines");
            await Assert.That(Parser.GetBodySerializationMethodName(UnsupportedSerializationValue)).IsEqualTo(string.Empty);
            await Assert.That(Parser.IsSupportedInlineBody(ImmutableEquatableArray<RequestParameterModel>.Empty)).IsTrue();
            await Assert.That(Parser.IsSupportedInlineBody(new([CreateHeaderParameter()]))).IsTrue();
            await Assert.That(Parser.IsSupportedInlineBody(new([CreateBody(string.Empty)]))).IsFalse();
            await Assert.That(Parser.IsSupportedInlineBody(new([CreateBody(UrlEncodedSerializationMethod)]))).IsTrue();
            await Assert.That(Parser.IsSupportedInlineBody(new([CreateBody("Serialized")]))).IsTrue();
            await Assert.That(Parser.IsSupportedInlineBody(new([CreateBody("JsonLines")]))).IsTrue();
            await Assert.That(Parser.ShouldDisposeResponse("global::System.Net.Http.HttpResponseMessage")).IsFalse();
            await Assert.That(Parser.ShouldDisposeResponse("global::System.Net.Http.HttpContent")).IsFalse();
            await Assert.That(Parser.ShouldDisposeResponse("global::System.IO.Stream")).IsFalse();
            await Assert.That(Parser.ShouldDisposeResponse("global::System.String")).IsTrue();
        }

        /// <summary>Verifies the request-compression and compression-level enum members map to their names, and that an
        /// unknown value maps to the member the runtime treats as unset rather than emitting an unresolvable name.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task CompressionHelpers_MapEnumValuesToMemberNames()
        {
            await Assert.That(Parser.GetRequestCompressionName(0)).IsEqualTo(DefaultMemberName);
            await Assert.That(Parser.GetRequestCompressionName(1)).IsEqualTo("None");
            await Assert.That(Parser.GetRequestCompressionName(GZipCompressionValue)).IsEqualTo("GZip");
            await Assert.That(Parser.GetRequestCompressionName(BrotliCompressionValue)).IsEqualTo("Brotli");
            await Assert.That(Parser.GetRequestCompressionName(ZstandardCompressionValue)).IsEqualTo("Zstandard");
            await Assert.That(Parser.GetRequestCompressionName(UnrecognizedEnumValue)).IsEqualTo(DefaultMemberName);

            await Assert.That(Parser.GetCompressionLevelName(0)).IsEqualTo("Optimal");
            await Assert.That(Parser.GetCompressionLevelName(1)).IsEqualTo("Fastest");
            await Assert.That(Parser.GetCompressionLevelName(NoCompressionLevelValue)).IsEqualTo("NoCompression");
            await Assert.That(Parser.GetCompressionLevelName(SmallestSizeLevelValue)).IsEqualTo("SmallestSize");
            await Assert.That(Parser.GetCompressionLevelName(UnrecognizedEnumValue)).IsEqualTo("Optimal");
        }

        /// <summary>Verifies containing-namespace matching handles a null symbol, exact matches, tail matches, and mismatches.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task IsInNamespace_HandlesNullSymbolAndNamespaceBoundaries()
        {
            // A null symbol has no containing namespace, so the walk never runs and the match fails.
            await Assert.That(Parser.IsInNamespace(null, "System")).IsFalse();

            var compilation = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(
                """
                namespace System.Threading.Tasks { public sealed class DeepMarker { } }
                namespace Tasks { public sealed class ShallowMarker { } }
                public sealed class RootMarker { }
                """));
            var deep = compilation.GetTypeByMetadataName("System.Threading.Tasks.DeepMarker")!;
            var shallow = compilation.GetTypeByMetadataName("Tasks.ShallowMarker")!;
            var root = compilation.GetTypeByMetadataName("RootMarker")!;

            // An exact segment-for-segment match that consumes the whole dotted name and ends at the global namespace.
            await Assert.That(Parser.IsInNamespace(deep, "System.Threading.Tasks")).IsTrue();

            // The symbol's namespace matches the dotted tail but reaches the global namespace before the name is consumed.
            await Assert.That(Parser.IsInNamespace(shallow, "Threading.Tasks")).IsFalse();

            // A first-segment mismatch bails without walking further.
            await Assert.That(Parser.IsInNamespace(deep, "System.Collections.Generic")).IsFalse();

            // A type in the global namespace matches no dotted name.
            await Assert.That(Parser.IsInNamespace(root, "System")).IsFalse();
        }

        /// <summary>Verifies URL-safe span formatting checks symbol availability, format strings, and special-type bounds.</summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Test]
        public async Task ComputeSpanFormattableTiers_ClassifiesSupportedTypesAndFormats()
        {
            var compilation = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(string.Empty));
            var httpMethodAttribute = compilation.GetTypeByMetadataName("Refit.HttpMethodAttribute")!;
            var context = new InterfaceGenerationContext(
                [],
                string.Empty,
                string.Empty,
                null,
                httpMethodAttribute,
                compilation.GetTypeByMetadataName("System.IFormattable"),
                null,
                SupportsSpanEscape: true,
                GeneratedRequestBuilding: true,
                EmitGeneratedCodeMarkers: false,
                SupportsNullable: true,
                SupportsStaticLambdas: true,
                SupportsCollectionExpressions: true,
                compilation,
                null,
                [],
                [],
                new(SymbolEqualityComparer.Default),
                new(SymbolEqualityComparer.Default),
                new(SymbolEqualityComparer.Default));
            var noSpanSymbol = Parser.ComputeSpanFormattableTiers(
                compilation.GetSpecialType(SpecialType.System_Int32),
                null,
                implementsSpanFormattable: true,
                context);
            var spanContext = context with { SpanFormattableSymbol = compilation.GetTypeByMetadataName("System.ISpanFormattable") };
            var supported = Parser.ComputeSpanFormattableTiers(
                compilation.GetSpecialType(SpecialType.System_Int32),
                null,
                implementsSpanFormattable: true,
                spanContext);
            var formatted = Parser.ComputeSpanFormattableTiers(
                compilation.GetSpecialType(SpecialType.System_Int32),
                "X",
                implementsSpanFormattable: true,
                spanContext);
            var aboveNumericRange = Parser.ComputeSpanFormattableTiers(
                compilation.GetSpecialType(SpecialType.System_Decimal),
                null,
                implementsSpanFormattable: true,
                spanContext);
            var belowNumericRange = Parser.ComputeSpanFormattableTiers(
                compilation.GetSpecialType(SpecialType.System_String),
                null,
                implementsSpanFormattable: true,
                spanContext);

            await Assert.That(noSpanSymbol).IsEqualTo((false, true));
            await Assert.That(supported).IsEqualTo((true, true));
            await Assert.That(formatted).IsEqualTo((false, true));
            await Assert.That(aboveNumericRange).IsEqualTo((false, true));
            await Assert.That(belowNumericRange).IsEqualTo((false, true));
        }

        /// <summary>Creates a non-body parameter model.</summary>
        /// <returns>The request parameter model.</returns>
        private static RequestParameterModel CreateHeaderParameter() =>
            new("query", "string", null, ImmutableEquatableArray<ParameterAttributeModel>.Empty, RequestParameterKind.Header, true, string.Empty, string.Empty, string.Empty, BodyBufferMode.None);

        /// <summary>Creates a body parameter model.</summary>
        /// <param name="serializationMethod">The serialization method name.</param>
        /// <returns>The request parameter model.</returns>
        private static RequestParameterModel CreateBody(string serializationMethod) =>
            new(
                "body",
                "string",
                null,
                ImmutableEquatableArray<ParameterAttributeModel>.Empty,
                RequestParameterKind.Body,
                false,
                string.Empty,
                string.Empty,
                serializationMethod,
                BodyBufferMode.Buffered);
    }
}
