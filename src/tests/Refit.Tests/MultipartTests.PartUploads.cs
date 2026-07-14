// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using SystemTextJsonSerializer = System.Text.Json.JsonSerializer;

namespace Refit.Tests;

/// <summary>Tests covering Refit's multipart uploads of explicit part types, serialized objects and raw HTTP content.</summary>
public partial class MultipartTests
{
    /// <summary>Verifies a single stream part keeps its supplied file name and content type.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartUploadShouldWorkWithStreamPart()
    {
        var handler = new MockHttpMessageHandler
        {
            Asserts = static async content =>
            {
                var parts = content.ToList();

                await Assert.That(parts).HasSingleItem();

                await Assert.That(parts[0].Headers.ContentDisposition!.Name).IsEqualTo(StreamName);
                await Assert.That(parts[0].Headers.ContentDisposition!.FileName).IsEqualTo(StreamPartFileName);
                await Assert.That(parts[0].Headers.ContentType!.MediaType).IsEqualTo(PdfMediaType);

                await using var str = await parts[0].ReadAsStreamAsync();
                await using var src = GetTestFileStream(TestFilePath);
                await Assert.That(StreamsEqual(src, str)).IsTrue();
            }
        };

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => handler };

        await using var stream = GetTestFileStream(TestFilePath);
        var fixture = RestService.For<IRunscopeApi>(BaseAddress, settings);
        await fixture.UploadStreamPart(
            new(stream, StreamPartFileName, PdfMediaType));
    }

    /// <summary>Verifies a stream part with a named multipart uses the supplied name.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartUploadShouldWorkWithStreamPartWithNamedMultipart()
    {
        var handler = new MockHttpMessageHandler
        {
            Asserts = static async content =>
            {
                var parts = content.ToList();

                await Assert.That(parts).HasSingleItem();

                await Assert.That(parts[0].Headers.ContentDisposition!.Name).IsEqualTo("test-stream");
                await Assert.That(parts[0].Headers.ContentDisposition!.FileName).IsEqualTo(StreamPartFileName);
                await Assert.That(parts[0].Headers.ContentType!.MediaType).IsEqualTo(PdfMediaType);

                await using var str = await parts[0].ReadAsStreamAsync();
                await using var src = GetTestFileStream(TestFilePath);
                await Assert.That(StreamsEqual(src, str)).IsTrue();
            }
        };

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => handler };

        await using var stream = GetTestFileStream(TestFilePath);
        var fixture = RestService.For<IRunscopeApi>(BaseAddress, settings);
        await fixture.UploadStreamPart(
            new(stream, StreamPartFileName, PdfMediaType, "test-stream"));
    }

    /// <summary>Verifies a stream part can be combined with query parameters.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartUploadShouldWorkWithStreamPartAndQuery()
    {
        var handler = new MockHttpMessageHandler
        {
            RequestAsserts = static async request => await Assert.That(request.RequestUri!.Query).IsEqualTo("?Property1=test&Property2=test2"),
            Asserts = static async content =>
            {
                var parts = content.ToList();

                await Assert.That(parts).HasSingleItem();

                await Assert.That(parts[0].Headers.ContentDisposition!.Name).IsEqualTo(StreamName);
                await Assert.That(parts[0].Headers.ContentDisposition!.FileName).IsEqualTo(StreamPartFileName);
                await Assert.That(parts[0].Headers.ContentType!.MediaType).IsEqualTo(PdfMediaType);

                await using var str = await parts[0].ReadAsStreamAsync();
                await using var src = GetTestFileStream(TestFilePath);
                await Assert.That(StreamsEqual(src, str)).IsTrue();
            }
        };

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => handler };

        await using var stream = GetTestFileStream(TestFilePath);
        var fixture = RestService.For<IRunscopeApi>(BaseAddress, settings);
        await fixture.UploadStreamPart(
            new() { Property1 = "test", Property2 = "test2" },
            new(stream, StreamPartFileName, PdfMediaType));
    }

    /// <summary>Verifies a byte array part keeps its supplied alias, file name and content type.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartUploadShouldWorkWithByteArrayPart()
    {
        var handler = new MockHttpMessageHandler
        {
            Asserts = static async content =>
            {
                var parts = content.ToList();

                await Assert.That(parts).HasSingleItem();

                await Assert.That(parts[0].Headers.ContentDisposition!.Name).IsEqualTo("ByteArrayPartParamAlias");
                await Assert
                    .That(parts[0].Headers.ContentDisposition!.FileName)
                    .IsEqualTo("test-bytearraypart.pdf");
                await Assert.That(parts[0].Headers.ContentType!.MediaType).IsEqualTo(PdfMediaType);

                await using var str = await parts[0].ReadAsStreamAsync();
                await using var src = GetTestFileStream(TestFilePath);
                await Assert.That(StreamsEqual(src, str)).IsTrue();
            }
        };

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => handler };

        await using var stream = GetTestFileStream(TestFilePath);
        using var reader = new BinaryReader(stream);
        var bytes = reader.ReadBytes((int)stream.Length);

        var fixture = RestService.For<IRunscopeApi>(BaseAddress, settings);
        await fixture.UploadBytesPart(
            new(bytes, "test-bytearraypart.pdf", PdfMediaType));
    }

    /// <summary>Verifies a collection of file parts plus an extra part keep their names and content types.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartUploadShouldWorkWithFileInfoPart()
    {
        var fileName = CreateTempFile();

        var handler = new MockHttpMessageHandler
        {
            Asserts = AssertFileInfoParts
        };

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => handler };

        try
        {
            await using var stream = GetTestFileStream(TestFilePath);
            await using var outStream = File.OpenWrite(fileName);
            await stream.CopyToAsync(outStream);
            await outStream.FlushAsync();
            outStream.Close();

            var fixture = RestService.For<IRunscopeApi>(BaseAddress, settings);
            await fixture.UploadFileInfoPart(
                [
                    new(
                        new(fileName),
                        "test-fileinfopart.pdf",
                        PdfMediaType),
                    new(
                        new(fileName),
                        "test-fileinfopart2.pdf")
                ],
                new(
                    new(fileName),
                    fileName: "additionalfile.pdf",
                    contentType: PdfMediaType));
        }
        finally
        {
            File.Delete(fileName);
        }
    }

    /// <summary>Verifies a single object is serialized to multipart content by each serializer.</summary>
    /// <param name="contentSerializerType">The serializer type to exercise.</param>
    /// <param name="mediaType">The expected media type produced by the serializer.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments(typeof(SystemTextJsonContentSerializer), JsonMediaType)]
    [Arguments(typeof(XmlContentSerializer), "application/xml")]
    public async Task MultipartUploadShouldWorkWithAnObject(
        Type contentSerializerType,
        string mediaType)
    {
        if (Activator.CreateInstance(contentSerializerType) is not IHttpContentSerializer serializer)
        {
            throw new ArgumentException(
                $"{contentSerializerType.FullName} does not implement {nameof(IHttpContentSerializer)}");
        }

        var model1 = new ModelObject { Property1 = Model1Property1, Property2 = Model1Property2 };

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

    /// <summary>Verifies multipart object serialization failures are wrapped with a descriptive argument exception.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartUploadWithUnserializableObjectThrowsArgumentException()
    {
        var fixture = RestService.For<IRunscopeApi>(
            BaseAddress,
            new()
            {
                ContentSerializer = new ThrowingContentSerializer()
            });

        Task UploadJsonObject() => fixture.UploadJsonObject(new());

        var exception = await Assert
            .That(UploadJsonObject)
            .ThrowsExactly<ArgumentException>();

        await Assert.That(exception!.Message).Contains("Unexpected parameter type", StringComparison.Ordinal);
        await Assert.That(exception.InnerException).IsTypeOf<InvalidOperationException>();
    }

    /// <summary>Verifies multiple objects are serialized to separate multipart parts by each serializer.</summary>
    /// <param name="contentSerializerType">The serializer type to exercise.</param>
    /// <param name="mediaType">The expected media type produced by the serializer.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments(typeof(SystemTextJsonContentSerializer), JsonMediaType)]
    [Arguments(typeof(XmlContentSerializer), "application/xml")]
    public async Task MultipartUploadShouldWorkWithObjects(
        Type contentSerializerType,
        string mediaType)
    {
        if (Activator.CreateInstance(contentSerializerType) is not IHttpContentSerializer serializer)
        {
            throw new ArgumentException(
                $"{contentSerializerType.FullName} does not implement {nameof(IHttpContentSerializer)}");
        }

        var model1 = new ModelObject { Property1 = Model1Property1, Property2 = Model1Property2 };

        var model2 = new ModelObject { Property1 = "M2.prop1" };

        var handler = new MockHttpMessageHandler
        {
            Asserts = async content =>
            {
                var parts = content.ToList();

                const int expectedPartCount = 2;

                await Assert.That(parts.Count).IsEqualTo(expectedPartCount);

                await Assert.That(parts[0].Headers.ContentDisposition!.Name).IsEqualTo(TheObjectsName);
                await Assert.That(parts[0].Headers.ContentDisposition!.FileName).IsNull();
                await Assert.That(parts[0].Headers.ContentType!.MediaType).IsEqualTo(mediaType);
                var result0 = await serializer
                    .FromHttpContentAsync<ModelObject>(parts[0])
                    .ConfigureAwait(false);
                await Assert.That(result0!.Property1).IsEqualTo(model1.Property1);
                await Assert.That(result0!.Property2).IsEqualTo(model1.Property2);

                await Assert.That(parts[1].Headers.ContentDisposition!.Name).IsEqualTo(TheObjectsName);
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

    /// <summary>Verifies a mixture of object, file, enum, string and integer parts are uploaded correctly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartUploadShouldWorkWithMixedTypes()
    {
        var fileName = CreateTempFile();
        var name = Path.GetFileName(fileName);

        var model1 = new ModelObject { Property1 = Model1Property1, Property2 = Model1Property2 };

        var model2 = new ModelObject { Property1 = "M2.prop1" };

        var anotherModel = new AnotherModel { Foos = ["bar1", "bar2"] };

        var handler = new MockHttpMessageHandler
        {
            Asserts = content => AssertMixedParts(content, model1, model2, name)
        };

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => handler };

        try
        {
            await using var stream = GetTestFileStream(TestFilePath);
            await using var outStream = File.OpenWrite(fileName);
            await stream.CopyToAsync(outStream);
            await outStream.FlushAsync();
            outStream.Close();

            var fixture = RestService.For<IRunscopeApi>(BaseAddress, settings);
            await fixture.UploadMixedObjects(
                [model1, model2],
                anotherModel,
                new(fileName),
                AnEnum.Val2,
                "frob",
                ExpectedIntValue);
        }
        finally
        {
            File.Delete(fileName);
        }
    }

    /// <summary>Verifies arbitrary HTTP content is uploaded preserving its disposition and type.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartUploadShouldWorkWithHttpContent()
    {
        var httpContent = new StringContent("some text", Encoding.ASCII, CustomMediaType);
        httpContent.Headers.ContentDisposition = new("attachment")
        {
            Name = "myName",
            FileName = "myFileName",
        };

        var handler = new MockHttpMessageHandler
        {
            Asserts = static async content =>
            {
                var parts = content.ToList();

                await Assert.That(parts).HasSingleItem();

                await Assert.That(parts[0].Headers.ContentDisposition!.Name).IsEqualTo("myName");
                await Assert.That(parts[0].Headers.ContentDisposition!.FileName).IsEqualTo("myFileName");
                await Assert.That(parts[0].Headers.ContentType!.MediaType).IsEqualTo(CustomMediaType);
                var result0 = await parts[0].ReadAsStringAsync().ConfigureAwait(false);
                await Assert.That(result0).IsEqualTo("some text");
            }
        };

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => handler };

        var fixture = RestService.For<IRunscopeApi>(BaseAddress, settings);
        await fixture.UploadHttpContent(httpContent);
    }

    /// <summary>Verifies each element of an <see cref="IEnumerable{T}"/> of <see cref="HttpContent"/> is added directly as
    /// its own multipart part when the request is built through the reflection request builder.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartHttpContentCollectionAddsEachItemThroughReflectionBuilder()
    {
        var first = new StringContent("first", Encoding.UTF8, CustomMediaType);
        var second = new StringContent("second", Encoding.UTF8, CustomMediaType);

        // The parts are captured while the request is in flight because the HttpClient disposes the multipart content
        // (clearing its child parts) once the send completes.
        List<HttpContent>? capturedParts = null;
        var handler = new MockHttpMessageHandler
        {
            Asserts = content =>
            {
                capturedParts = content.ToList();
                return Task.CompletedTask;
            }
        };

        var fixture = new RequestBuilderImplementation<IRunscopeApi>();
        var factory = fixture.BuildRestResultFuncForMethod(nameof(IRunscopeApi.UploadHttpContents));
        using var client = new HttpClient(handler) { BaseAddress = new(BaseAddress) };

        var task = (Task)factory(client, [new List<HttpContent> { first, second }])!;
        await task;

        const int expectedPartCount = 2;
        await Assert.That(capturedParts!.Count).IsEqualTo(expectedPartCount);
        await Assert.That(capturedParts).Contains(first);
        await Assert.That(capturedParts).Contains(second);
    }

    /// <summary>Verifies an element of an enumerable multipart parameter that the serializer cannot serialize is wrapped in
    /// a descriptive argument exception, and that the request message is disposed and the failure rethrown when request
    /// building fails, exercised through the reflection request builder.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartUnserializableCollectionItemThroughReflectionBuilderThrowsArgumentException()
    {
        var settings = new RefitSettings { ContentSerializer = new ThrowingContentSerializer() };
        var fixture = new RequestBuilderImplementation<IRunscopeApi>(settings);
        var factory = fixture.BuildRequestFactoryForMethod(nameof(IRunscopeApi.UploadJsonObjects));

        Task Build() => factory([new List<ModelObject> { new() }]);

        var exception = await Assert.That(Build).ThrowsExactly<ArgumentException>();

        await Assert.That(exception!.Message).Contains("Unexpected parameter type", StringComparison.Ordinal);
        await Assert.That(exception.InnerException).IsTypeOf<InvalidOperationException>();
    }

    /// <summary>Verifies the <see cref="ByteArrayPart"/> constructor rejects a null file name.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultiPartConstructorShouldThrowArgumentNullExceptionWhenNoFileName() =>
        await Assert
            .That(static () => _ = new ByteArrayPart([], null!, PdfMediaType))
            .ThrowsExactly<ArgumentNullException>();

    /// <summary>Verifies the <see cref="FileInfoPart"/> constructor rejects a null file info.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task FileInfoPartConstructorShouldThrowArgumentNullExceptionWhenNoFileInfo() =>
        await Assert
            .That(static () => _ = new FileInfoPart(null!, "file.pdf", PdfMediaType))
            .ThrowsExactly<ArgumentNullException>();

    /// <summary>Asserts the parts produced by the file-info multipart upload keep their names and content types.</summary>
    /// <param name="content">The multipart content observed by the handler.</param>
    /// <returns>A task representing the assertion work.</returns>
    private static async Task AssertFileInfoParts(MultipartFormDataContent content)
    {
        var parts = content.ToList();

        const int expectedPartCount = 3;
        const int additionalFilePartIndex = 2;

        await Assert.That(parts.Count).IsEqualTo(expectedPartCount);

        await Assert.That(parts[0].Headers.ContentDisposition!.Name).IsEqualTo(FileInfosName);
        await Assert.That(parts[0].Headers.ContentDisposition!.FileName).IsEqualTo("test-fileinfopart.pdf");
        await Assert.That(parts[0].Headers.ContentType!.MediaType).IsEqualTo(PdfMediaType);
        await using (var str = await parts[0].ReadAsStreamAsync())
        await using (var src = GetTestFileStream(TestFilePath))
        {
            await Assert.That(StreamsEqual(src, str)).IsTrue();
        }

        await Assert.That(parts[1].Headers.ContentDisposition!.Name).IsEqualTo(FileInfosName);
        await Assert
            .That(parts[1].Headers.ContentDisposition!.FileName)
            .IsEqualTo("test-fileinfopart2.pdf");
        await Assert.That(parts[1].Headers.ContentType).IsNull();
        await using (var str = await parts[1].ReadAsStreamAsync())
        await using (var src = GetTestFileStream(TestFilePath))
        {
            await Assert.That(StreamsEqual(src, str)).IsTrue();
        }

        await Assert.That(parts[additionalFilePartIndex].Headers.ContentDisposition!.Name).IsEqualTo("anotherFile");
        await Assert.That(parts[additionalFilePartIndex].Headers.ContentDisposition!.FileName).IsEqualTo("additionalfile.pdf");
        await Assert.That(parts[additionalFilePartIndex].Headers.ContentType!.MediaType).IsEqualTo(PdfMediaType);
        await using (var str = await parts[additionalFilePartIndex].ReadAsStreamAsync())
        await using (var src = GetTestFileStream(TestFilePath))
        {
            await Assert.That(StreamsEqual(src, str)).IsTrue();
        }
    }

    /// <summary>Asserts the parts produced by the mixed-types multipart upload.</summary>
    /// <param name="content">The multipart content observed by the handler.</param>
    /// <param name="model1">The first expected model object.</param>
    /// <param name="model2">The second expected model object.</param>
    /// <param name="name">The expected file-part file name.</param>
    /// <returns>A task representing the assertion work.</returns>
    private static async Task AssertMixedParts(MultipartFormDataContent content, ModelObject model1, ModelObject model2, string name)
    {
        const int expectedPartCount = 7;
        const int anotherModelPartIndex = 2;
        const int filePartIndex = 3;
        const int enumPartIndex = 4;
        const int stringPartIndex = 5;
        const int intPartIndex = 6;
        const int expectedFooCount = 2;

        var parts = content.ToList();

        await Assert.That(parts.Count).IsEqualTo(expectedPartCount);

        await Assert.That(parts[0].Headers.ContentDisposition!.Name).IsEqualTo(TheObjectsName);
        await Assert.That(parts[0].Headers.ContentDisposition!.FileName).IsNull();
        await Assert.That(parts[0].Headers.ContentType!.MediaType).IsEqualTo(JsonMediaType);
        var result0 = await DeserializeMultipartPart<ModelObject>(parts[0]);
        await Assert.That(result0!.Property1).IsEqualTo(model1.Property1);
        await Assert.That(result0!.Property2).IsEqualTo(model1.Property2);

        await Assert.That(parts[1].Headers.ContentDisposition!.Name).IsEqualTo(TheObjectsName);
        await Assert.That(parts[1].Headers.ContentDisposition!.FileName).IsNull();
        await Assert.That(parts[1].Headers.ContentType!.MediaType).IsEqualTo(JsonMediaType);
        var result1 = await DeserializeMultipartPart<ModelObject>(parts[1]);
        await Assert.That(result1!.Property1).IsEqualTo(model2.Property1);
        await Assert.That(result1!.Property2).IsEqualTo(model2.Property2);

        await Assert.That(parts[anotherModelPartIndex].Headers.ContentDisposition!.Name).IsEqualTo("anotherModel");
        await Assert.That(parts[anotherModelPartIndex].Headers.ContentDisposition!.FileName).IsNull();
        await Assert.That(parts[anotherModelPartIndex].Headers.ContentType!.MediaType).IsEqualTo(JsonMediaType);
        var result2 = await DeserializeMultipartPart<AnotherModel>(parts[anotherModelPartIndex]);
        await Assert.That(result2!.Foos!.Length).IsEqualTo(expectedFooCount);
        await Assert.That(result2!.Foos![0]).IsEqualTo("bar1");
        await Assert.That(result2!.Foos![1]).IsEqualTo("bar2");

        await Assert.That(parts[filePartIndex].Headers.ContentDisposition!.Name).IsEqualTo("aFile");
        await Assert.That(parts[filePartIndex].Headers.ContentDisposition!.FileName).IsEqualTo(name);
        await Assert.That(parts[filePartIndex].Headers.ContentType).IsNull();
        await using (var str = await parts[filePartIndex].ReadAsStreamAsync())
        await using (var src = GetTestFileStream(TestFilePath))
        {
            await Assert.That(StreamsEqual(src, str)).IsTrue();
        }

        await Assert.That(parts[enumPartIndex].Headers.ContentDisposition!.Name).IsEqualTo("anEnum");
        await Assert.That(parts[enumPartIndex].Headers.ContentDisposition!.FileName).IsNull();
        await Assert.That(parts[enumPartIndex].Headers.ContentType!.MediaType).IsEqualTo(JsonMediaType);
        var result4 = await DeserializeMultipartPart<AnEnum>(parts[enumPartIndex]);
        await Assert.That(result4).IsEqualTo(AnEnum.Val2);

        await Assert.That(parts[stringPartIndex].Headers.ContentDisposition!.Name).IsEqualTo("aString");
        await Assert.That(parts[stringPartIndex].Headers.ContentDisposition!.FileName).IsNull();
        await Assert.That(parts[stringPartIndex].Headers.ContentType!.MediaType).IsEqualTo(PlainTextMediaType);
        await Assert.That(parts[stringPartIndex].Headers.ContentType!.CharSet).IsEqualTo(Utf8CharSet);
        await Assert.That(await parts[stringPartIndex].ReadAsStringAsync()).IsEqualTo("frob");

        await Assert.That(parts[intPartIndex].Headers.ContentDisposition!.Name).IsEqualTo("anInt");
        await Assert.That(parts[intPartIndex].Headers.ContentDisposition!.FileName).IsNull();
        await Assert.That(parts[intPartIndex].Headers.ContentType!.MediaType).IsEqualTo(JsonMediaType);
        var result6 = await DeserializeMultipartPart<int>(parts[intPartIndex]);
        await Assert.That(result6).IsEqualTo(ExpectedIntValue);
    }

    /// <summary>Deserializes a multipart part's string content with the default serializer options.</summary>
    /// <typeparam name="T">The type to deserialize the part into.</typeparam>
    /// <param name="part">The multipart part to read.</param>
    /// <returns>The deserialized value.</returns>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
        Justification = "The type argument selects the deserialization target and cannot be inferred from the content parameter.")]
    private static async Task<T?> DeserializeMultipartPart<T>(HttpContent part) =>
        SystemTextJsonSerializer.Deserialize<T>(
            await part.ReadAsStringAsync().ConfigureAwait(false),
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions());
}
