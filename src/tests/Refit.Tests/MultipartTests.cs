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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
namespace Refit.Tests;

/// <summary>Tests covering Refit's multipart form upload support.</summary>
public partial class MultipartTests
{
    /// <summary>The base address used by the multipart test clients.</summary>
    private const string BaseAddress = "https://api/";

    /// <summary>The embedded test PDF resource path.</summary>
    private const string TestFilePath = "Test Files/Test.pdf";

    /// <summary>The PDF media type used by multipart parts.</summary>
    private const string PdfMediaType = "application/pdf";

    /// <summary>The JSON media type used by multipart parts.</summary>
    private const string JsonMediaType = "application/json";

    /// <summary>A custom media type used to verify explicit content types survive multipart assembly.</summary>
    private const string CustomMediaType = "application/custom";

    /// <summary>The multipart name used for stream parts.</summary>
    private const string StreamName = "stream";

    /// <summary>The multipart name used for file-info parts.</summary>
    private const string FileInfosName = "fileInfos";

    /// <summary>The multipart name used for object collection parts.</summary>
    private const string TheObjectsName = "theObjects";

    /// <summary>The file name used for stream parts.</summary>
    private const string StreamPartFileName = "test-streampart.pdf";

    /// <summary>The plain text media type asserted for string multipart parts.</summary>
    private const string PlainTextMediaType = "text/plain";

    /// <summary>The character set asserted for string multipart parts.</summary>
    private const string Utf8CharSet = "utf-8";

    /// <summary>The first property value of the sample model object.</summary>
    private const string Model1Property1 = "M1.prop1";

    /// <summary>The second property value of the sample model object.</summary>
    private const string Model1Property2 = "M1.prop2";

    /// <summary>The expected integer value uploaded in the mixed-types test.</summary>
    private const int ExpectedIntValue = 42;

    /// <summary>The aliased name value uploaded in the form-object flattening tests.</summary>
    private const string FormObjectFullName = "Ada Lovelace";

    /// <summary>A role value uploaded in the form-object flattening tests.</summary>
    private const string FormObjectAdminRole = "admin";

    /// <summary>The age value uploaded in the form-object flattening tests.</summary>
    private const int FormObjectAge = 36;

    /// <summary>The expected plain-text rendering of <see cref="FormObjectAge"/>.</summary>
    private const string FormObjectAgeText = "36";

    /// <summary>Verifies a raw stream is uploaded as a single multipart part.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartUploadShouldWorkWithStream()
    {
        var handler = new MockHttpMessageHandler
        {
            Asserts = static async content =>
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
            Asserts = static async content =>
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
            input.GetMethods().First(static x => x.Name == "UploadStreamWithCustomBoundary"));
        await Assert.That(methodFixture.MultipartBoundary).IsEqualTo("-----SomeCustomBoundary");
    }

    /// <summary>Verifies a byte array is uploaded as a single multipart part.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartUploadShouldWorkWithByteArray()
    {
        var handler = new MockHttpMessageHandler
        {
            Asserts = static async content =>
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

                const int expectedPartCount = 3;
                const int additionalFilePartIndex = 2;

                await Assert.That(parts.Count).IsEqualTo(expectedPartCount);

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

                await Assert.That(parts[additionalFilePartIndex].Headers.ContentDisposition!.Name).IsEqualTo("anotherFile");
                await Assert.That(parts[additionalFilePartIndex].Headers.ContentDisposition!.FileName).IsEqualTo(name);
                await Assert.That(parts[additionalFilePartIndex].Headers.ContentType).IsNull();
                await using (var str = await parts[additionalFilePartIndex].ReadAsStreamAsync())
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
            Asserts = static async content =>
            {
                var parts = content.ToList();

                await Assert.That(parts).HasSingleItem();

                await Assert.That(parts[0].Headers.ContentDisposition!.Name).IsEqualTo("SomeStringAlias");
                await Assert.That(parts[0].Headers.ContentDisposition!.FileName).IsNull();
                await Assert.That(parts[0].Headers.ContentType!.MediaType).IsEqualTo(PlainTextMediaType);
                await Assert.That(parts[0].Headers.ContentType!.CharSet).IsEqualTo(Utf8CharSet);
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

                const int expectedPartCount = 2;

                await Assert.That(parts.Count).IsEqualTo(expectedPartCount);

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
            Asserts = static async content =>
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
            RequestAsserts = static async message =>
            {
                // The source-generated inline path attaches the interface type, the method name, the raw route
                // template, and the [Property] option, but not the reflection-only RestMethodInfo option, so it
                // carries one fewer request option than the reflection builder (which additionally stores its
                // RestMethodInfoInternal). This is generated-path behavior shared by every method, not specific to
                // multipart.
                const int expectedRequestPropertyCount = 4;

                await Assert.That(message.Headers.Authorization!.ToString()).IsEqualTo(someHeader);

#if NET6_0_OR_GREATER
                await Assert.That(message.Options.Count()).IsEqualTo(expectedRequestPropertyCount);
                await Assert
                    .That(((IDictionary<string, object?>)message.Options)["SomeProperty"])
                    .IsEqualTo(someProperty);
#endif

#pragma warning disable CS0618 // Type or member is obsolete
                await Assert.That(message.Properties.Count).IsEqualTo(expectedRequestPropertyCount);
                await Assert.That(message.Properties["SomeProperty"]).IsEqualTo(someProperty);
#pragma warning restore CS0618 // Type or member is obsolete
            },
            Asserts = static async content =>
            {
                var parts = content.ToList();

                await Assert.That(parts).HasSingleItem();

                await Assert.That(parts[0].Headers.ContentDisposition!.Name).IsEqualTo("SomeStringAlias");
                await Assert.That(parts[0].Headers.ContentDisposition!.FileName).IsNull();
                await Assert.That(parts[0].Headers.ContentType!.MediaType).IsEqualTo(PlainTextMediaType);
                await Assert.That(parts[0].Headers.ContentType!.CharSet).IsEqualTo(Utf8CharSet);
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
        var fullName = Array.Find(
            assembly.GetManifestResourceNames(),
            name => name.EndsWith(relativeName, StringComparison.InvariantCulture))
            ?? throw new InvalidOperationException(
                $"Unable to find resource for path \"{relativeFilePath}\". Resource with name ending on \"{relativeName}\" was not found in assembly.");

        return assembly.GetManifestResourceStream(fullName)
            ?? throw new InvalidOperationException(
                $"Unable to find resource for path \"{relativeFilePath}\". Resource named \"{fullName}\" was not found in assembly.");
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
            "Design",
            "SST2307:Generic method type parameters should be inferable from the parameters",
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
