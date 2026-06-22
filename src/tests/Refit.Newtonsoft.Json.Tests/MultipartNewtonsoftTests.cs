// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>Multipart upload tests that exercise the Newtonsoft content serializer.</summary>
public class MultipartNewtonsoftTests
{
    /// <summary>The base address used by the multipart test clients.</summary>
    private const string BaseAddress = "https://api/";

    /// <summary>Verifies a single object is serialized to multipart content by the Newtonsoft serializer.</summary>
    /// <param name="contentSerializerType">The serializer type to exercise.</param>
    /// <param name="mediaType">The expected media type produced by the serializer.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments(typeof(NewtonsoftJsonContentSerializer), "application/json")]
    public async Task MultipartUploadShouldWorkWithAnObject(
        Type contentSerializerType,
        string mediaType)
    {
        if (Activator.CreateInstance(contentSerializerType) is not IHttpContentSerializer serializer)
        {
            throw new ArgumentException(
                $"{contentSerializerType.FullName} does not implement {nameof(IHttpContentSerializer)}");
        }

        var model1 = new ModelObject { Property1 = "M1.prop1", Property2 = "M1.prop2" };

        var handler = new MockHttpMessageHandler
        {
            Asserts = async content =>
            {
                var parts = content.ToList();

                await Assert.That(parts).HasSingleItem();

                await Assert.That(parts[0].Headers.ContentDisposition!.Name).IsEqualTo("theObject");
                await Assert.That(parts[0].Headers.ContentDisposition!.FileName).IsNull();
                await Assert.That(parts[0].Headers.ContentType!.MediaType).IsEqualTo(mediaType);
                var result0 = await serializer
                    .FromHttpContentAsync<ModelObject>(parts[0])
                    .ConfigureAwait(false);
                await Assert.That(result0!.Property1).IsEqualTo(model1.Property1);
                await Assert.That(result0!.Property2).IsEqualTo(model1.Property2);
            }
        };

        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => handler,
            ContentSerializer = serializer
        };

        var fixture = RestService.For<IRunscopeApi>(BaseAddress, settings);
        await fixture.UploadJsonObject(model1);
    }

    /// <summary>Verifies multiple objects are serialized to separate multipart parts by the Newtonsoft serializer.</summary>
    /// <param name="contentSerializerType">The serializer type to exercise.</param>
    /// <param name="mediaType">The expected media type produced by the serializer.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments(typeof(NewtonsoftJsonContentSerializer), "application/json")]
    public async Task MultipartUploadShouldWorkWithObjects(
        Type contentSerializerType,
        string mediaType)
    {
        if (Activator.CreateInstance(contentSerializerType) is not IHttpContentSerializer serializer)
        {
            throw new ArgumentException(
                $"{contentSerializerType.FullName} does not implement {nameof(IHttpContentSerializer)}");
        }

        var model1 = new ModelObject { Property1 = "M1.prop1", Property2 = "M1.prop2" };

        var model2 = new ModelObject { Property1 = "M2.prop1" };

        var handler = new MockHttpMessageHandler
        {
            Asserts = async content =>
            {
                var parts = content.ToList();

                await Assert.That(parts.Count).IsEqualTo(2);

                await Assert.That(parts[0].Headers.ContentDisposition!.Name).IsEqualTo("theObjects");
                await Assert.That(parts[0].Headers.ContentDisposition!.FileName).IsNull();
                await Assert.That(parts[0].Headers.ContentType!.MediaType).IsEqualTo(mediaType);
                var result0 = await serializer
                    .FromHttpContentAsync<ModelObject>(parts[0])
                    .ConfigureAwait(false);
                await Assert.That(result0!.Property1).IsEqualTo(model1.Property1);
                await Assert.That(result0!.Property2).IsEqualTo(model1.Property2);

                await Assert.That(parts[1].Headers.ContentDisposition!.Name).IsEqualTo("theObjects");
                await Assert.That(parts[1].Headers.ContentDisposition!.FileName).IsNull();
                await Assert.That(parts[1].Headers.ContentType!.MediaType).IsEqualTo(mediaType);
                var result1 = await serializer
                    .FromHttpContentAsync<ModelObject>(parts[1])
                    .ConfigureAwait(false);
                await Assert.That(result1!.Property1).IsEqualTo(model2.Property1);
                await Assert.That(result1!.Property2).IsEqualTo(model2.Property2);
            }
        };

        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => handler,
            ContentSerializer = serializer
        };

        var fixture = RestService.For<IRunscopeApi>(BaseAddress, settings);
        await fixture.UploadJsonObjects([model1, model2]);
    }

    /// <summary>An <see cref="HttpMessageHandler"/> that asserts against the captured multipart request.</summary>
    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        /// <summary>Gets or sets the assertions run against the multipart form content.</summary>
        public Func<MultipartFormDataContent, Task>? Asserts { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var content = request.Content as MultipartFormDataContent;
            await Assert.That(content).IsTypeOf<MultipartFormDataContent>();
            await Assert.That(Asserts).IsNotNull();

            await Asserts!(content!);

            return new(HttpStatusCode.OK);
        }
    }
}
