// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;

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
            await Assert.That(Parser.GetBodySerializationMethodName(0)).IsEqualTo("Default");
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
