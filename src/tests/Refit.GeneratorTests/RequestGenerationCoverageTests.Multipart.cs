// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.GeneratorTests;

/// <summary>Request-generation coverage for inline multipart content emission branches.</summary>
public sealed partial class RequestGenerationCoverageTests
{
    /// <summary>Verifies a multipart method with statically-dispatchable parts builds its content inline.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartMethodGeneratesInline()
    {
        const string Source =
            """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Multipart]
                [Post("/upload")]
                Task<string> Upload([AliasAs("file")] IEnumerable<StreamPart> streams);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
        await Assert.That(generated).Contains("new global::System.Net.Http.MultipartFormDataContent(\"----MyGreatBoundary\")");
    }

    /// <summary>Verifies a multipart method with an <c>object</c>-typed part still falls back to the reflective builder.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartMethodWithObjectPartFallsBack()
    {
        const string Source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Multipart]
                [Post("/upload")]
                Task<string> Upload(object payload);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(generated).Contains(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies a multipart method with an <see cref="System.Net.Http.HttpContent"/> part adds the content directly and generates inline.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartHttpContentPartGeneratesInline()
    {
        const string Source =
            """
            using System.Net.Http;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Multipart]
                [Post("/upload")]
                Task<string> Upload([AliasAs("content")] HttpContent content);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
        await Assert.That(generated).Contains(".Add(@content)");
    }

    /// <summary>Verifies a <c>[Query]</c> parameter inside a multipart method feeds the query string rather than a part.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartQueryParameterFeedsQueryStringInline()
    {
        const string Source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Multipart]
                [Post("/upload")]
                Task<string> Upload([Query] string filter, [AliasAs("file")] StreamPart part);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
        await Assert.That(generated).Contains("MultipartFormDataContent");
    }

    /// <summary>Verifies multipart parts classified through the single-value and reference-enumerable element paths:
    /// a nullable formattable, an array of strings, an enumerable of byte arrays, and a list of strings.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartEnumerableAndNullableElementPartsGenerateInline()
    {
        const string Source =
            """
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Multipart]
                [Post("/a")]
                Task<string> UploadNullableId([AliasAs("id")] Guid? id);

                [Multipart]
                [Post("/b")]
                Task<string> UploadTags([AliasAs("tag")] string[] tags);

                [Multipart]
                [Post("/c")]
                Task<string> UploadBlobs([AliasAs("blob")] IEnumerable<byte[]> blobs);

                [Multipart]
                [Post("/d")]
                Task<string> UploadNames([AliasAs("name")] List<string> names);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }
}
