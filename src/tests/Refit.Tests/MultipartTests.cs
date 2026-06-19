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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Refit.Tests;

/// <summary>Tests covering Refit's multipart form upload support.</summary>
[RequiresUnreferencedCode("Refit's reflection-based serialization and request building are exercised by these tests.")]
[RequiresDynamicCode("Refit's reflection-based serialization and request building are exercised by these tests.")]
public class MultipartTests
{
    /// <summary>The base address used by the multipart test clients.</summary>
    private const string BaseAddress = "https://api/";

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

                await Assert.That(parts[0].Headers.ContentDisposition!.Name).IsEqualTo("stream");
                await Assert.That(parts[0].Headers.ContentDisposition!.FileName).IsEqualTo("stream");

                await using var str = await parts[0].ReadAsStreamAsync();
                await using var src = GetTestFileStream("Test Files/Test.pdf");
                await Assert.That(StreamsEqual(src, str)).IsTrue();
            }
        };

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => handler };

        await using var stream = GetTestFileStream("Test Files/Test.pdf");
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

                await Assert.That(parts[0].Headers.ContentDisposition!.Name).IsEqualTo("stream");
                await Assert.That(parts[0].Headers.ContentDisposition!.FileName).IsEqualTo("stream");

                await using var str = await parts[0].ReadAsStreamAsync();
                await using var src = GetTestFileStream("Test Files/Test.pdf");
                await Assert.That(StreamsEqual(src, str)).IsTrue();
            }
        };

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => handler };

        await using (var stream = GetTestFileStream("Test Files/Test.pdf"))
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
                await using var src = GetTestFileStream("Test Files/Test.pdf");
                await Assert.That(StreamsEqual(src, str)).IsTrue();
            }
        };

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => handler };

        await using var stream = GetTestFileStream("Test Files/Test.pdf");
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
        var fileName = Path.GetTempFileName();
        var name = Path.GetFileName(fileName);

        var handler = new MockHttpMessageHandler
        {
            Asserts = async content =>
            {
                var parts = content.ToList();

                await Assert.That(parts.Count).IsEqualTo(3);

                await Assert.That(parts[0].Headers.ContentDisposition!.Name).IsEqualTo("fileInfos");
                await Assert.That(parts[0].Headers.ContentDisposition!.FileName).IsEqualTo(name);
                await Assert.That(parts[0].Headers.ContentType).IsNull();
                await using (var str = await parts[0].ReadAsStreamAsync())
                await using (var src = GetTestFileStream("Test Files/Test.pdf"))
                {
                    await Assert.That(StreamsEqual(src, str)).IsTrue();
                }

                await Assert.That(parts[1].Headers.ContentDisposition!.Name).IsEqualTo("fileInfos");
                await Assert.That(parts[1].Headers.ContentDisposition!.FileName).IsEqualTo(name);
                await Assert.That(parts[1].Headers.ContentType).IsNull();
                await using (var str = await parts[1].ReadAsStreamAsync())
                await using (var src = GetTestFileStream("Test Files/Test.pdf"))
                {
                    await Assert.That(StreamsEqual(src, str)).IsTrue();
                }

                await Assert.That(parts[2].Headers.ContentDisposition!.Name).IsEqualTo("anotherFile");
                await Assert.That(parts[2].Headers.ContentDisposition!.FileName).IsEqualTo(name);
                await Assert.That(parts[2].Headers.ContentType).IsNull();
                await using (var str = await parts[2].ReadAsStreamAsync())
                await using (var src = GetTestFileStream("Test Files/Test.pdf"))
                {
                    await Assert.That(StreamsEqual(src, str)).IsTrue();
                }
            }
        };

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => handler };

        try
        {
            await using var stream = GetTestFileStream("Test Files/Test.pdf");
            await using var outStream = File.OpenWrite(fileName);
            await stream.CopyToAsync(outStream);
            await outStream.FlushAsync();
            outStream.Close();

            var fixture = RestService.For<IRunscopeApi>(BaseAddress, settings);
            await fixture.UploadFileInfo(
                [new FileInfo(fileName), new FileInfo(fileName)],
                new FileInfo(fileName));
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

                await Assert.That(parts[0].Headers.ContentDisposition!.Name).IsEqualTo("stream");
                await Assert.That(parts[0].Headers.ContentDisposition!.FileName).IsEqualTo("test-streampart.pdf");
                await Assert.That(parts[0].Headers.ContentType!.MediaType).IsEqualTo("application/pdf");

                await using var str = await parts[0].ReadAsStreamAsync();
                await using var src = GetTestFileStream("Test Files/Test.pdf");
                await Assert.That(StreamsEqual(src, str)).IsTrue();
            }
        };

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => handler };

        await using var stream = GetTestFileStream("Test Files/Test.pdf");
        var fixture = RestService.For<IRunscopeApi>(BaseAddress, settings);
        await fixture.UploadStreamPart(
            new StreamPart(stream, "test-streampart.pdf", "application/pdf"));
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
                await Assert.That(parts[0].Headers.ContentDisposition!.FileName).IsEqualTo("test-streampart.pdf");
                await Assert.That(parts[0].Headers.ContentType!.MediaType).IsEqualTo("application/pdf");

                await using var str = await parts[0].ReadAsStreamAsync();
                await using var src = GetTestFileStream("Test Files/Test.pdf");
                await Assert.That(StreamsEqual(src, str)).IsTrue();
            }
        };

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => handler };

        await using var stream = GetTestFileStream("Test Files/Test.pdf");
        var fixture = RestService.For<IRunscopeApi>(BaseAddress, settings);
        await fixture.UploadStreamPart(
            new StreamPart(stream, "test-streampart.pdf", "application/pdf", "test-stream"));
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

                await Assert.That(parts[0].Headers.ContentDisposition!.Name).IsEqualTo("stream");
                await Assert.That(parts[0].Headers.ContentDisposition!.FileName).IsEqualTo("test-streampart.pdf");
                await Assert.That(parts[0].Headers.ContentType!.MediaType).IsEqualTo("application/pdf");

                await using var str = await parts[0].ReadAsStreamAsync();
                await using var src = GetTestFileStream("Test Files/Test.pdf");
                await Assert.That(StreamsEqual(src, str)).IsTrue();
            }
        };

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => handler };

        await using var stream = GetTestFileStream("Test Files/Test.pdf");
        var fixture = RestService.For<IRunscopeApi>(BaseAddress, settings);
        await fixture.UploadStreamPart(
            new ModelObject { Property1 = "test", Property2 = "test2" },
            new StreamPart(stream, "test-streampart.pdf", "application/pdf"));
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
                await Assert.That(parts[0].Headers.ContentType!.MediaType).IsEqualTo("application/pdf");

                await using var str = await parts[0].ReadAsStreamAsync();
                await using var src = GetTestFileStream("Test Files/Test.pdf");
                await Assert.That(StreamsEqual(src, str)).IsTrue();
            }
        };

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => handler };

        await using var stream = GetTestFileStream("Test Files/Test.pdf");
        using var reader = new BinaryReader(stream);
        var bytes = reader.ReadBytes((int)stream.Length);

        var fixture = RestService.For<IRunscopeApi>(BaseAddress, settings);
        await fixture.UploadBytesPart(
            new ByteArrayPart(bytes, "test-bytearraypart.pdf", "application/pdf"));
    }

    /// <summary>Verifies a collection of file parts plus an extra part keep their names and content types.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartUploadShouldWorkWithFileInfoPart()
    {
        var fileName = Path.GetTempFileName();

        var handler = new MockHttpMessageHandler
        {
            Asserts = async content =>
            {
                var parts = content.ToList();

                await Assert.That(parts.Count).IsEqualTo(3);

                await Assert.That(parts[0].Headers.ContentDisposition!.Name).IsEqualTo("fileInfos");
                await Assert.That(parts[0].Headers.ContentDisposition!.FileName).IsEqualTo("test-fileinfopart.pdf");
                await Assert.That(parts[0].Headers.ContentType!.MediaType).IsEqualTo("application/pdf");
                await using (var str = await parts[0].ReadAsStreamAsync())
                await using (var src = GetTestFileStream("Test Files/Test.pdf"))
                {
                    await Assert.That(StreamsEqual(src, str)).IsTrue();
                }

                await Assert.That(parts[1].Headers.ContentDisposition!.Name).IsEqualTo("fileInfos");
                await Assert
                    .That(parts[1].Headers.ContentDisposition!.FileName)
                    .IsEqualTo("test-fileinfopart2.pdf");
                await Assert.That(parts[1].Headers.ContentType).IsNull();
                await using (var str = await parts[1].ReadAsStreamAsync())
                await using (var src = GetTestFileStream("Test Files/Test.pdf"))
                {
                    await Assert.That(StreamsEqual(src, str)).IsTrue();
                }

                await Assert.That(parts[2].Headers.ContentDisposition!.Name).IsEqualTo("anotherFile");
                await Assert.That(parts[2].Headers.ContentDisposition!.FileName).IsEqualTo("additionalfile.pdf");
                await Assert.That(parts[2].Headers.ContentType!.MediaType).IsEqualTo("application/pdf");
                await using (var str = await parts[2].ReadAsStreamAsync())
                await using (var src = GetTestFileStream("Test Files/Test.pdf"))
                {
                    await Assert.That(StreamsEqual(src, str)).IsTrue();
                }
            }
        };

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => handler };

        try
        {
            await using var stream = GetTestFileStream("Test Files/Test.pdf");
            await using var outStream = File.OpenWrite(fileName);
            await stream.CopyToAsync(outStream);
            await outStream.FlushAsync();
            outStream.Close();

            var fixture = RestService.For<IRunscopeApi>(BaseAddress, settings);
            await fixture.UploadFileInfoPart(
                [
                    new FileInfoPart(
                        new FileInfo(fileName),
                        "test-fileinfopart.pdf",
                        "application/pdf"),
                    new FileInfoPart(
                        new FileInfo(fileName),
                        "test-fileinfopart2.pdf",
                        contentType: null)
                ],
                new FileInfoPart(
                    new FileInfo(fileName),
                    fileName: "additionalfile.pdf",
                    contentType: "application/pdf"));
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
    [Arguments(typeof(NewtonsoftJsonContentSerializer), "application/json")]
    [Arguments(typeof(SystemTextJsonContentSerializer), "application/json")]
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

    /// <summary>Verifies multiple objects are serialized to separate multipart parts by each serializer.</summary>
    /// <param name="contentSerializerType">The serializer type to exercise.</param>
    /// <param name="mediaType">The expected media type produced by the serializer.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments(typeof(NewtonsoftJsonContentSerializer), "application/json")]
    [Arguments(typeof(SystemTextJsonContentSerializer), "application/json")]
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

    /// <summary>Verifies a mixture of object, file, enum, string and integer parts are uploaded correctly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartUploadShouldWorkWithMixedTypes()
    {
        var fileName = Path.GetTempFileName();
        var name = Path.GetFileName(fileName);

        var model1 = new ModelObject { Property1 = "M1.prop1", Property2 = "M1.prop2" };

        var model2 = new ModelObject { Property1 = "M2.prop1" };

        var anotherModel = new AnotherModel { Foos = ["bar1", "bar2"] };

        var handler = new MockHttpMessageHandler
        {
            Asserts = async content =>
            {
                var parts = content.ToList();

                await Assert.That(parts.Count).IsEqualTo(7);

                await Assert.That(parts[0].Headers.ContentDisposition!.Name).IsEqualTo("theObjects");
                await Assert.That(parts[0].Headers.ContentDisposition!.FileName).IsNull();
                await Assert.That(parts[0].Headers.ContentType!.MediaType).IsEqualTo("application/json");
                var result0 = JsonConvert.DeserializeObject<ModelObject>(
                    await parts[0].ReadAsStringAsync().ConfigureAwait(false));
                await Assert.That(result0!.Property1).IsEqualTo(model1.Property1);
                await Assert.That(result0!.Property2).IsEqualTo(model1.Property2);

                await Assert.That(parts[1].Headers.ContentDisposition!.Name).IsEqualTo("theObjects");
                await Assert.That(parts[1].Headers.ContentDisposition!.FileName).IsNull();
                await Assert.That(parts[1].Headers.ContentType!.MediaType).IsEqualTo("application/json");
                var result1 = JsonConvert.DeserializeObject<ModelObject>(
                    await parts[1].ReadAsStringAsync().ConfigureAwait(false));
                await Assert.That(result1!.Property1).IsEqualTo(model2.Property1);
                await Assert.That(result1!.Property2).IsEqualTo(model2.Property2);

                await Assert.That(parts[2].Headers.ContentDisposition!.Name).IsEqualTo("anotherModel");
                await Assert.That(parts[2].Headers.ContentDisposition!.FileName).IsNull();
                await Assert.That(parts[2].Headers.ContentType!.MediaType).IsEqualTo("application/json");
                var result2 = JsonConvert.DeserializeObject<AnotherModel>(
                    await parts[2].ReadAsStringAsync().ConfigureAwait(false));
                await Assert.That(result2!.Foos!.Length).IsEqualTo(2);
                await Assert.That(result2!.Foos![0]).IsEqualTo("bar1");
                await Assert.That(result2!.Foos![1]).IsEqualTo("bar2");

                await Assert.That(parts[3].Headers.ContentDisposition!.Name).IsEqualTo("aFile");
                await Assert.That(parts[3].Headers.ContentDisposition!.FileName).IsEqualTo(name);
                await Assert.That(parts[3].Headers.ContentType).IsNull();
                await using (var str = await parts[3].ReadAsStreamAsync())
                await using (var src = GetTestFileStream("Test Files/Test.pdf"))
                {
                    await Assert.That(StreamsEqual(src, str)).IsTrue();
                }

                await Assert.That(parts[4].Headers.ContentDisposition!.Name).IsEqualTo("anEnum");
                await Assert.That(parts[4].Headers.ContentDisposition!.FileName).IsNull();
                await Assert.That(parts[4].Headers.ContentType!.MediaType).IsEqualTo("application/json");
                var result4 = JsonConvert.DeserializeObject<AnEnum>(
                    await parts[4].ReadAsStringAsync().ConfigureAwait(false));
                await Assert.That(result4).IsEqualTo(AnEnum.Val2);

                await Assert.That(parts[5].Headers.ContentDisposition!.Name).IsEqualTo("aString");
                await Assert.That(parts[5].Headers.ContentDisposition!.FileName).IsNull();
                await Assert.That(parts[5].Headers.ContentType!.MediaType).IsEqualTo("text/plain");
                await Assert.That(parts[5].Headers.ContentType!.CharSet).IsEqualTo("utf-8");
                await Assert.That(await parts[5].ReadAsStringAsync()).IsEqualTo("frob");

                await Assert.That(parts[6].Headers.ContentDisposition!.Name).IsEqualTo("anInt");
                await Assert.That(parts[6].Headers.ContentDisposition!.FileName).IsNull();
                await Assert.That(parts[6].Headers.ContentType!.MediaType).IsEqualTo("application/json");
                var result6 = JsonConvert.DeserializeObject<int>(
                    await parts[6].ReadAsStringAsync().ConfigureAwait(false));
                await Assert.That(result6).IsEqualTo(42);
            }
        };

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => handler };

        try
        {
            await using var stream = GetTestFileStream("Test Files/Test.pdf");
            await using var outStream = File.OpenWrite(fileName);
            await stream.CopyToAsync(outStream);
            await outStream.FlushAsync();
            outStream.Close();

            var fixture = RestService.For<IRunscopeApi>(BaseAddress, settings);
            await fixture.UploadMixedObjects(
                [model1, model2],
                anotherModel,
                new FileInfo(fileName),
                AnEnum.Val2,
                "frob",
                42);
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
        httpContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
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
    public async Task MultiPartConstructorShouldThrowArgumentNullExceptionWhenNoFileName()
    {
        await Assert
            .That(() => _ = new ByteArrayPart([], null!, "application/pdf"))
            .ThrowsExactly<ArgumentNullException>();
    }

    /// <summary>Verifies the <see cref="FileInfoPart"/> constructor rejects a null file info.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task FileInfoPartConstructorShouldThrowArgumentNullExceptionWhenNoFileInfo()
    {
        await Assert
            .That(() => _ = new FileInfoPart(null!, "file.pdf", "application/pdf"))
            .ThrowsExactly<ArgumentNullException>();
    }

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
            .FirstOrDefault(name => name.EndsWith(relativeName, StringComparison.InvariantCulture));
        if (fullName is null)
        {
            throw new InvalidOperationException(
                $"Unable to find resource for path \"{relativeFilePath}\". Resource with name ending on \"{relativeName}\" was not found in assembly.");
        }

        var stream = assembly.GetManifestResourceStream(fullName);
        if (stream is null)
        {
            throw new InvalidOperationException(
                $"Unable to find resource for path \"{relativeFilePath}\". Resource named \"{fullName}\" was not found in assembly.");
        }

        return stream;
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

        if (a is null || b is null)
        {
            throw new ArgumentNullException(a is null ? "a" : "b");
        }

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

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
