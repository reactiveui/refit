// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SystemTextJsonSerializer = System.Text.Json.JsonSerializer;

namespace Refit.Tests;

/// <summary>Tests covering Refit's multipart form upload support.</summary>
public class MultipartTests
{
    /// <summary>The base address used by the multipart test clients.</summary>
    private const string BaseAddress = "https://api/";

    /// <summary>The embedded test PDF resource path.</summary>
    private const string TestFilePath = "Test Files/Test.pdf";

    /// <summary>The PDF media type used by multipart parts.</summary>
    private const string PdfMediaType = "application/pdf";

    /// <summary>The JSON media type used by multipart parts.</summary>
    private const string JsonMediaType = "application/json";

    /// <summary>The multipart name used for stream parts.</summary>
    private const string StreamName = "stream";

    /// <summary>The multipart name used for file-info parts.</summary>
    private const string FileInfosName = "fileInfos";

    /// <summary>The multipart name used for object collection parts.</summary>
    private const string TheObjectsName = "theObjects";

    /// <summary>The file name used for stream parts.</summary>
    private const string StreamPartFileName = "test-streampart.pdf";

    /// <summary>The expected integer value uploaded in the mixed-types test.</summary>
    private const int ExpectedIntValue = 42;

    /// <summary>Verifies a raw stream is uploaded as a single multipart part.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartUploadShouldWorkWithStream()
    {
        var handler = new MockHttpMessageHandler
        {
            Asserts = async content =>
            {
                var parts = content.ToList();

                await Assert.That(parts).HasSingleItem();

                await Assert.That(parts[0].Headers.ContentDisposition!.Name).IsEqualTo(StreamName);
                await Assert.That(parts[0].Headers.ContentDisposition!.FileName).IsEqualTo(StreamName);

                await using var str = await parts[0].ReadAsStreamAsync();
                await using var src = GetTestFileStream(TestFilePath);
                await Assert.That(StreamsEqual(src, str)).IsTrue();
            }
        };

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => handler };

        await using var stream = GetTestFileStream(TestFilePath);
        var fixture = RestService.For<IRunscopeApi>(BaseAddress, settings);
        await fixture.UploadStream(stream);
    }

    /// <summary>Verifies a raw stream is uploaded using a custom multipart boundary.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartUploadShouldWorkWithStreamAndCustomBoundary()
    {
        var handler = new MockHttpMessageHandler
        {
            Asserts = async content =>
            {
                var parts = content.ToList();

                await Assert.That(parts).HasSingleItem();

                await Assert.That(parts[0].Headers.ContentDisposition!.Name).IsEqualTo(StreamName);
                await Assert.That(parts[0].Headers.ContentDisposition!.FileName).IsEqualTo(StreamName);

                await using var str = await parts[0].ReadAsStreamAsync();
                await using var src = GetTestFileStream(TestFilePath);
                await Assert.That(StreamsEqual(src, str)).IsTrue();
            }
        };

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => handler };

        await using (var stream = GetTestFileStream(TestFilePath))
        {
            var fixture = RestService.For<IRunscopeApi>(BaseAddress, settings);
            await fixture.UploadStreamWithCustomBoundary(stream);
        }

        var input = typeof(IRunscopeApi);
        var methodFixture = new RestMethodInfoInternal(
            input,
            input.GetMethods().First(x => x.Name == "UploadStreamWithCustomBoundary"));
        await Assert.That(methodFixture.MultipartBoundary).IsEqualTo("-----SomeCustomBoundary");
    }

    /// <summary>Verifies a byte array is uploaded as a single multipart part.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartUploadShouldWorkWithByteArray()
    {
        var handler = new MockHttpMessageHandler
        {
            Asserts = async content =>
            {
                var parts = content.ToList();

                await Assert.That(parts).HasSingleItem();

                await Assert.That(parts[0].Headers.ContentDisposition!.Name).IsEqualTo("bytes");
                await Assert.That(parts[0].Headers.ContentDisposition!.FileName).IsEqualTo("bytes");
                await Assert.That(parts[0].Headers.ContentType).IsNull();
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
        await fixture.UploadBytes(bytes);
    }

    /// <summary>Verifies a collection of files plus an extra file are uploaded as separate parts.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartUploadShouldWorkWithFileInfo()
    {
        var fileName = CreateTempFile();
        var name = Path.GetFileName(fileName);

        var handler = new MockHttpMessageHandler
        {
            Asserts = async content =>
            {
                var parts = content.ToList();

                await Assert.That(parts.Count).IsEqualTo(3);

                await Assert.That(parts[0].Headers.ContentDisposition!.Name).IsEqualTo(FileInfosName);
                await Assert.That(parts[0].Headers.ContentDisposition!.FileName).IsEqualTo(name);
                await Assert.That(parts[0].Headers.ContentType).IsNull();
                await using (var str = await parts[0].ReadAsStreamAsync())
                await using (var src = GetTestFileStream(TestFilePath))
                {
                    await Assert.That(StreamsEqual(src, str)).IsTrue();
                }

                await Assert.That(parts[1].Headers.ContentDisposition!.Name).IsEqualTo(FileInfosName);
                await Assert.That(parts[1].Headers.ContentDisposition!.FileName).IsEqualTo(name);
                await Assert.That(parts[1].Headers.ContentType).IsNull();
                await using (var str = await parts[1].ReadAsStreamAsync())
                await using (var src = GetTestFileStream(TestFilePath))
                {
                    await Assert.That(StreamsEqual(src, str)).IsTrue();
                }

                await Assert.That(parts[2].Headers.ContentDisposition!.Name).IsEqualTo("anotherFile");
                await Assert.That(parts[2].Headers.ContentDisposition!.FileName).IsEqualTo(name);
                await Assert.That(parts[2].Headers.ContentType).IsNull();
                await using (var str = await parts[2].ReadAsStreamAsync())
                await using (var src = GetTestFileStream(TestFilePath))
                {
                    await Assert.That(StreamsEqual(src, str)).IsTrue();
                }
            }
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
            await fixture.UploadFileInfo(
                [new(fileName), new(fileName)],
                new(fileName));
        }
        finally
        {
            File.Delete(fileName);
        }
    }

    /// <summary>Verifies a string value is uploaded as a single text multipart part.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartUploadShouldWorkWithString()
    {
        const string text = "This is random text";

        var handler = new MockHttpMessageHandler
        {
            Asserts = async content =>
            {
                var parts = content.ToList();

                await Assert.That(parts).HasSingleItem();

                await Assert.That(parts[0].Headers.ContentDisposition!.Name).IsEqualTo("SomeStringAlias");
                await Assert.That(parts[0].Headers.ContentDisposition!.FileName).IsNull();
                await Assert.That(parts[0].Headers.ContentType!.MediaType).IsEqualTo("text/plain");
                await Assert.That(parts[0].Headers.ContentType!.CharSet).IsEqualTo("utf-8");
                var str = await parts[0].ReadAsStringAsync();
                await Assert.That(str).IsEqualTo(text);
            }
        };

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => handler };

        var fixture = RestService.For<IRunscopeApi>(BaseAddress, settings);
        await fixture.UploadString(text);
    }

    /// <summary>Verifies <see cref="Guid"/> and <see cref="DateTime"/> values are sent as plain text, not JSON-quoted.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartUploadShouldWorkWithFormattableValues()
    {
        var id = new Guid("12345678-1234-1234-1234-1234567890ab");
        var timestamp = new DateTimeOffset(2021, 12, 1, 8, 16, 16, TimeSpan.Zero);

        var handler = new MockHttpMessageHandler
        {
            Asserts = async content =>
            {
                var parts = content.ToList();

                await Assert.That(parts.Count).IsEqualTo(2);

                await Assert.That(parts[0].Headers.ContentDisposition!.Name).IsEqualTo("id");
                var idText = await parts[0].ReadAsStringAsync();
                await Assert.That(idText).IsEqualTo(id.ToString());

                await Assert.That(parts[1].Headers.ContentDisposition!.Name).IsEqualTo("timestamp");
                var timestampText = await parts[1].ReadAsStringAsync();
                await Assert
                    .That(timestampText)
                    .IsEqualTo(timestamp.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
        };

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => handler };

        var fixture = RestService.For<IRunscopeApi>(BaseAddress, settings);
        await fixture.UploadFormattableValues(id, timestamp);
    }

    /// <summary>Verifies a formatter returning null yields an empty multipart part rather than throwing.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartFormattableValueWithNullFormatterIsEmpty()
    {
        var handler = new MockHttpMessageHandler
        {
            Asserts = async content =>
            {
                var parts = content.ToList();
                await Assert.That(parts[0].Headers.ContentDisposition!.Name).IsEqualTo("id");
                await Assert.That(await parts[0].ReadAsStringAsync()).IsEqualTo(string.Empty);
            }
        };

        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => handler,
            FormUrlEncodedParameterFormatter = new NullReturningFormUrlEncodedParameterFormatter()
        };

        var fixture = RestService.For<IRunscopeApi>(BaseAddress, settings);
        await fixture.UploadFormattableValues(Guid.NewGuid(), DateTimeOffset.UnixEpoch);
    }

    /// <summary>Verifies a header and request property are sent alongside a multipart string upload.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartUploadShouldWorkWithHeaderAndRequestProperty()
    {
        const string text = "This is random text";
        const string someHeader = "someHeader";
        const string someProperty = "someProperty";

        var handler = new MockHttpMessageHandler
        {
            RequestAsserts = async message =>
            {
                await Assert.That(message.Headers.Authorization!.ToString()).IsEqualTo(someHeader);

#if NET6_0_OR_GREATER
                await Assert.That(message.Options.Count()).IsEqualTo(3);
                await Assert
                    .That(((IDictionary<string, object?>)message.Options)["SomeProperty"])
                    .IsEqualTo(someProperty);
#endif

#pragma warning disable CS0618 // Type or member is obsolete
                await Assert.That(message.Properties.Count).IsEqualTo(3);
                await Assert.That(message.Properties["SomeProperty"]).IsEqualTo(someProperty);
#pragma warning restore CS0618 // Type or member is obsolete
            },
            Asserts = async content =>
            {
                var parts = content.ToList();

                await Assert.That(parts).HasSingleItem();

                await Assert.That(parts[0].Headers.ContentDisposition!.Name).IsEqualTo("SomeStringAlias");
                await Assert.That(parts[0].Headers.ContentDisposition!.FileName).IsNull();
                await Assert.That(parts[0].Headers.ContentType!.MediaType).IsEqualTo("text/plain");
                await Assert.That(parts[0].Headers.ContentType!.CharSet).IsEqualTo("utf-8");
                var str = await parts[0].ReadAsStringAsync();
                await Assert.That(str).IsEqualTo(text);
            }
        };

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => handler };

        var fixture = RestService.For<IRunscopeApi>(BaseAddress, settings);
        await fixture.UploadStringWithHeaderAndRequestProperty(
            someHeader,
            someProperty,
            text);
    }

    /// <summary>Verifies a single stream part keeps its supplied file name and content type.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartUploadShouldWorkWithStreamPart()
    {
        var handler = new MockHttpMessageHandler
        {
            Asserts = async content =>
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
            Asserts = async content =>
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
            RequestAsserts = async request => await Assert.That(request.RequestUri!.Query).IsEqualTo("?Property1=test&Property2=test2"),
            Asserts = async content =>
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
            Asserts = async content =>
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
            Asserts = async content =>
            {
                var parts = content.ToList();

                await Assert.That(parts.Count).IsEqualTo(3);

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

                await Assert.That(parts[2].Headers.ContentDisposition!.Name).IsEqualTo("anotherFile");
                await Assert.That(parts[2].Headers.ContentDisposition!.FileName).IsEqualTo("additionalfile.pdf");
                await Assert.That(parts[2].Headers.ContentType!.MediaType).IsEqualTo(PdfMediaType);
                await using (var str = await parts[2].ReadAsStreamAsync())
                await using (var src = GetTestFileStream(TestFilePath))
                {
                    await Assert.That(StreamsEqual(src, str)).IsTrue();
                }
            }
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

        var model1 = new ModelObject { Property1 = "M1.prop1", Property2 = "M1.prop2" };

        var model2 = new ModelObject { Property1 = "M2.prop1" };

        var handler = new MockHttpMessageHandler
        {
            Asserts = async content =>
            {
                var parts = content.ToList();

                await Assert.That(parts.Count).IsEqualTo(2);

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

        var model1 = new ModelObject { Property1 = "M1.prop1", Property2 = "M1.prop2" };

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
        var httpContent = new StringContent("some text", Encoding.ASCII, "application/custom");
        httpContent.Headers.ContentDisposition = new("attachment")
        {
            Name = "myName",
            FileName = "myFileName",
        };

        var handler = new MockHttpMessageHandler
        {
            Asserts = async content =>
            {
                var parts = content.ToList();

                await Assert.That(parts).HasSingleItem();

                await Assert.That(parts[0].Headers.ContentDisposition!.Name).IsEqualTo("myName");
                await Assert.That(parts[0].Headers.ContentDisposition!.FileName).IsEqualTo("myFileName");
                await Assert.That(parts[0].Headers.ContentType!.MediaType).IsEqualTo("application/custom");
                var result0 = await parts[0].ReadAsStringAsync().ConfigureAwait(false);
                await Assert.That(result0).IsEqualTo("some text");
            }
        };

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => handler };

        var fixture = RestService.For<IRunscopeApi>(BaseAddress, settings);
        await fixture.UploadHttpContent(httpContent);
    }

    /// <summary>Verifies the <see cref="ByteArrayPart"/> constructor rejects a null file name.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultiPartConstructorShouldThrowArgumentNullExceptionWhenNoFileName() =>
        await Assert
            .That(() => _ = new ByteArrayPart([], null!, PdfMediaType))
            .ThrowsExactly<ArgumentNullException>();

    /// <summary>Verifies the <see cref="FileInfoPart"/> constructor rejects a null file info.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task FileInfoPartConstructorShouldThrowArgumentNullExceptionWhenNoFileInfo() =>
        await Assert
            .That(() => _ = new FileInfoPart(null!, "file.pdf", PdfMediaType))
            .ThrowsExactly<ArgumentNullException>();

    /// <summary>Loads an embedded test resource as a stream.</summary>
    /// <param name="relativeFilePath">The relative path of the embedded resource.</param>
    /// <returns>A stream over the embedded resource contents.</returns>
    internal static Stream GetTestFileStream(string relativeFilePath)
    {
        const char namespaceSeparator = '.';

        // get calling assembly
        var assembly = System.Reflection.Assembly.GetCallingAssembly();

        // compute resource name suffix
        var relativeName =
            "."
            + relativeFilePath
                .Replace('\\', namespaceSeparator)
                .Replace('/', namespaceSeparator)
                .Replace(' ', '_');

        // get resource stream
        var fullName = assembly
            .GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(relativeName, StringComparison.InvariantCulture))
            ?? throw new InvalidOperationException(
                $"Unable to find resource for path \"{relativeFilePath}\". Resource with name ending on \"{relativeName}\" was not found in assembly.");

        return assembly.GetManifestResourceStream(fullName)
            ?? throw new InvalidOperationException(
                $"Unable to find resource for path \"{relativeFilePath}\". Resource named \"{fullName}\" was not found in assembly.");
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
        var result0 = SystemTextJsonSerializer.Deserialize<ModelObject>(
            await parts[0].ReadAsStringAsync().ConfigureAwait(false),
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions());
        await Assert.That(result0!.Property1).IsEqualTo(model1.Property1);
        await Assert.That(result0!.Property2).IsEqualTo(model1.Property2);

        await Assert.That(parts[1].Headers.ContentDisposition!.Name).IsEqualTo(TheObjectsName);
        await Assert.That(parts[1].Headers.ContentDisposition!.FileName).IsNull();
        await Assert.That(parts[1].Headers.ContentType!.MediaType).IsEqualTo(JsonMediaType);
        var result1 = SystemTextJsonSerializer.Deserialize<ModelObject>(
            await parts[1].ReadAsStringAsync().ConfigureAwait(false),
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions());
        await Assert.That(result1!.Property1).IsEqualTo(model2.Property1);
        await Assert.That(result1!.Property2).IsEqualTo(model2.Property2);

        await Assert.That(parts[anotherModelPartIndex].Headers.ContentDisposition!.Name).IsEqualTo("anotherModel");
        await Assert.That(parts[anotherModelPartIndex].Headers.ContentDisposition!.FileName).IsNull();
        await Assert.That(parts[anotherModelPartIndex].Headers.ContentType!.MediaType).IsEqualTo(JsonMediaType);
        var result2 = SystemTextJsonSerializer.Deserialize<AnotherModel>(
            await parts[anotherModelPartIndex].ReadAsStringAsync().ConfigureAwait(false),
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions());
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
        var result4 = SystemTextJsonSerializer.Deserialize<AnEnum>(
            await parts[enumPartIndex].ReadAsStringAsync().ConfigureAwait(false),
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions());
        await Assert.That(result4).IsEqualTo(AnEnum.Val2);

        await Assert.That(parts[stringPartIndex].Headers.ContentDisposition!.Name).IsEqualTo("aString");
        await Assert.That(parts[stringPartIndex].Headers.ContentDisposition!.FileName).IsNull();
        await Assert.That(parts[stringPartIndex].Headers.ContentType!.MediaType).IsEqualTo("text/plain");
        await Assert.That(parts[stringPartIndex].Headers.ContentType!.CharSet).IsEqualTo("utf-8");
        await Assert.That(await parts[stringPartIndex].ReadAsStringAsync()).IsEqualTo("frob");

        await Assert.That(parts[intPartIndex].Headers.ContentDisposition!.Name).IsEqualTo("anInt");
        await Assert.That(parts[intPartIndex].Headers.ContentDisposition!.FileName).IsNull();
        await Assert.That(parts[intPartIndex].Headers.ContentType!.MediaType).IsEqualTo(JsonMediaType);
        var result6 = SystemTextJsonSerializer.Deserialize<int>(
            await parts[intPartIndex].ReadAsStringAsync().ConfigureAwait(false),
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions());
        await Assert.That(result6).IsEqualTo(ExpectedIntValue);
    }

    /// <summary>Creates an empty temporary file with a random, non-predictable name and returns its path.</summary>
    /// <returns>The full path to the newly created temporary file.</returns>
    private static string CreateTempFile()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        File.Create(path).Dispose();
        return path;
    }

    /// <summary>Determines whether two streams contain identical byte content.</summary>
    /// <param name="a">The first stream to compare.</param>
    /// <param name="b">The second stream to compare.</param>
    /// <returns><see langword="true"/> if both streams contain the same bytes; otherwise, <see langword="false"/>.</returns>
    private static bool StreamsEqual(Stream a, Stream b)
    {
        if (a is null && b is null)
        {
            return true;
        }

        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        if (a.Length < b.Length)
        {
            return false;
        }

        if (a.Length > b.Length)
        {
            return false;
        }

        for (var i = 0; i < a.Length; i++)
        {
            var byteFromA = a.ReadByte();
            var byteFromB = b.ReadByte();
            if (byteFromA != byteFromB)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>An <see cref="HttpMessageHandler"/> that asserts against the captured multipart request.</summary>
    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        /// <summary>Gets or sets the optional assertions run against the outgoing request message.</summary>
        public Func<HttpRequestMessage, Task>? RequestAsserts { get; set; }

        /// <summary>Gets or sets the assertions run against the multipart form content.</summary>
        public Func<MultipartFormDataContent, Task>? Asserts { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (RequestAsserts is not null)
            {
                await RequestAsserts(request);
            }

            var content = request.Content as MultipartFormDataContent;
            await Assert.That(content).IsTypeOf<MultipartFormDataContent>();
            await Assert.That(Asserts).IsNotNull();

            await Asserts!(content!);

            return new(HttpStatusCode.OK);
        }
    }

    /// <summary>An <see cref="IFormUrlEncodedParameterFormatter"/> that always returns null.</summary>
    private sealed class NullReturningFormUrlEncodedParameterFormatter : IFormUrlEncodedParameterFormatter
    {
        /// <inheritdoc />
        public string? Format(object? value, string? formatString) => null;
    }

    /// <summary>An <see cref="IHttpContentSerializer"/> that rejects all serialization calls.</summary>
    private sealed class ThrowingContentSerializer : IHttpContentSerializer
    {
        /// <inheritdoc />
        public HttpContent ToHttpContent<T>(T item) =>
            throw new InvalidOperationException("serialization failed");

        /// <inheritdoc />
        [SuppressMessage(
            "Major Code Smell",
            "S4018:Generic methods should provide type parameters",
            Justification = "The method implements Refit's published serializer interface.")]
        public Task<T?> FromHttpContentAsync<T>(
            HttpContent content,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(default(T));

        /// <inheritdoc />
        public string? GetFieldNameForProperty(PropertyInfo propertyInfo) =>
            null;
    }
}
