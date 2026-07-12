// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Refit.Tests;

/// <summary>Serialized-content tests that exercise the Newtonsoft content serializer.</summary>
public class SerializedContentNewtonsoftTests
{
    /// <summary>The base address used when creating Refit clients for these tests.</summary>
    private const string NewtonsoftBaseAddress = "https://api/";

    /// <summary>Verifies that a request requiring a serialized body completes without deadlocking under Newtonsoft.</summary>
    /// <param name="contentSerializerType">The content serializer implementation under test.</param>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    [Arguments(typeof(NewtonsoftJsonContentSerializer))]
    public async Task WhenARequestRequiresABodyThenItDoesNotDeadlock(Type contentSerializerType)
    {
        if (
            Activator.CreateInstance(contentSerializerType) is not IHttpContentSerializer serializer)
        {
            throw new ArgumentException(
                $"{contentSerializerType.FullName} does not implement {nameof(IHttpContentSerializer)}");
        }

        var handler = new MockPushStreamContentHttpMessageHandler
        {
            Asserts = async content =>
                new StringContent(await content.ReadAsStringAsync().ConfigureAwait(false))
        };

        var settings = new RefitSettings(serializer) { HttpMessageHandlerFactory = () => handler };

        var fixture = RestService.For<IGitHubApi>(NewtonsoftBaseAddress, settings);

        var fixtureTask = await RunTaskWithATimeLimit(fixture.CreateUser(new()))
            .ConfigureAwait(false);
        await Assert.That(fixtureTask.IsCompleted).IsTrue();
        await Assert.That(fixtureTask.Status).IsEqualTo(TaskStatus.RanToCompletion);
    }

    /// <summary>Verifies that a request body is serialized and round-trips back to the original model under Newtonsoft.</summary>
    /// <param name="contentSerializerType">The content serializer implementation under test.</param>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    [Arguments(typeof(NewtonsoftJsonContentSerializer))]
    public async Task WhenARequestRequiresABodyThenItIsSerialized(Type contentSerializerType)
    {
        if (
            Activator.CreateInstance(contentSerializerType) is not IHttpContentSerializer serializer)
        {
            throw new ArgumentException(
                $"{contentSerializerType.FullName} does not implement {nameof(IHttpContentSerializer)}");
        }

        const int sampleCreatedYear = 1949;
        const int sampleCreatedMonth = 9;
        const int sampleCreatedDay = 16;
        var model = new User
        {
            Name = "Wile E. Coyote",
            CreatedAt = new DateOnly(sampleCreatedYear, sampleCreatedMonth, sampleCreatedDay).ToString(),
            Company = "ACME",
        };

        var handler = new MockPushStreamContentHttpMessageHandler
        {
            Asserts = async content =>
            {
                var stringContent = new StringContent(
                    await content.ReadAsStringAsync().ConfigureAwait(false));
                var user = await serializer
                    .FromHttpContentAsync<User>(content)
                    .ConfigureAwait(false);
                await Assert.That(user).IsNotSameReferenceAs(model);
                await Assert.That(user!.Name).IsEqualTo(model.Name);
                await Assert.That(user.CreatedAt).IsEqualTo(model.CreatedAt);
                await Assert.That(user.Company).IsEqualTo(model.Company);

                // Returns some content so that the serializer does not complain.
                return stringContent;
            }
        };

        var settings = new RefitSettings(serializer) { HttpMessageHandlerFactory = () => handler };

        var fixture = RestService.For<IGitHubApi>(NewtonsoftBaseAddress, settings);

        var fixtureTask = await RunTaskWithATimeLimit(fixture.CreateUser(model))
            .ConfigureAwait(false);

        await Assert.That(fixtureTask.IsCompleted).IsTrue();
    }

    /// <summary>Verifies the Newtonsoft content serializer can be assigned as the <see cref="RefitSettings"/> content serializer.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task VerityNewtonsoftSerializer()
    {
        var settings = new RefitSettings(new NewtonsoftJsonContentSerializer());

        await Assert.That(settings.ContentSerializer).IsNotNull();
        await Assert.That(settings.ContentSerializer).IsTypeOf<NewtonsoftJsonContentSerializer>();
    }

    /// <summary>Verifies that the Newtonsoft content serializer never falls back to synchronous stream reads.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task StreamDeserialization_UsingNewtonsoftJsonContentSerializer_DoesNotUseSynchronousReads()
    {
        var serializer = new NewtonsoftJsonContentSerializer();
        var content = new AsyncOnlyJsonContent("{\"name\":\"Road Runner\"}");

        var result = await serializer.FromHttpContentAsync<User>(content);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("Road Runner");
    }

    /// <summary>Verifies the synchronous DeserializeFromString reads a value from a buffered string (#1591).</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task NewtonsoftJsonContentSerializer_DeserializeFromString_ReadsValue()
    {
        var serializer = new NewtonsoftJsonContentSerializer();

        var result = serializer.DeserializeFromString<User>("{\"name\":\"Road Runner\"}");

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("Road Runner");
    }

    /// <summary>Verifies that the Newtonsoft content serializer returns the default value for null content.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task StreamDeserialization_UsingNewtonsoftJsonContentSerializer_ReturnsDefaultForNullContent()
    {
        var serializer = new NewtonsoftJsonContentSerializer();

        var result = await serializer.FromHttpContentAsync<User>(null!);

        await Assert.That(result).IsNull();
    }

    /// <summary>Verifies that the Newtonsoft content serializer returns the JsonProperty name for a property.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task NewtonsoftJsonContentSerializer_GetFieldNameForProperty_ReturnsJsonPropertyName()
    {
        var serializer = new NewtonsoftJsonContentSerializer();
        var property = typeof(NewtonsoftFieldNameModel).GetProperty(
            nameof(NewtonsoftFieldNameModel.Name));

        var fieldName = serializer.GetFieldNameForProperty(property!);

        await Assert.That(fieldName).IsEqualTo("json_name");
    }

    /// <summary>Verifies that the Newtonsoft content serializer returns null when no JsonProperty attribute exists.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task NewtonsoftJsonContentSerializer_GetFieldNameForProperty_ReturnsNullWithoutJsonPropertyAttribute()
    {
        var serializer = new NewtonsoftJsonContentSerializer();
        var property = typeof(NewtonsoftFieldNameModel).GetProperty(
            nameof(NewtonsoftFieldNameModel.Unaliased));

        var fieldName = serializer.GetFieldNameForProperty(property!);

        await Assert.That(fieldName).IsNull();
    }

    /// <summary>Verifies that the Newtonsoft content serializer throws when the property argument is null.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task NewtonsoftJsonContentSerializer_GetFieldNameForProperty_ThrowsForNullProperty()
    {
        var serializer = new NewtonsoftJsonContentSerializer();

        var exception = await Assert.That(() => serializer.GetFieldNameForProperty(null!)).ThrowsExactly<ArgumentNullException>();

        await Assert.That(exception!.ParamName).IsEqualTo("propertyInfo");
    }

    /// <summary>Verifies quoted charsets are unwrapped before Newtonsoft deserialization resolves the encoding.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task NewtonsoftJsonContentSerializer_FromHttpContentAsync_UnwrapsQuotedCharset()
    {
        var serializer = new NewtonsoftJsonContentSerializer();
        var content = new StringContent(
            """{"Name":"Utf16 User"}""",
            Encoding.Unicode,
            "application/json");
        content.Headers.ContentType!.CharSet = "\"utf-16\"";

        var result = await serializer.FromHttpContentAsync<User>(content);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("Utf16 User");
    }

    /// <summary>Verifies invalid Newtonsoft response charsets fail with a clear operation exception.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task NewtonsoftJsonContentSerializer_FromHttpContentAsync_ThrowsForInvalidCharset()
    {
        var serializer = new NewtonsoftJsonContentSerializer();
        var content = new StringContent("""{"Name":"Invalid"}""", Encoding.UTF8, "application/json");
        content.Headers.ContentType!.CharSet = "not-a-real-charset";

        await Assert.That(() => serializer.FromHttpContentAsync<User>(content))
            .ThrowsExactly<InvalidOperationException>();
    }

    /// <summary>Runs the task to completion or until the timeout occurs.</summary>
    /// <param name="fixtureTask">The fixture task to run within the time limit.</param>
    /// <returns>The original fixture task once it completes or the timeout elapses.</returns>
    private static async Task<Task<User>> RunTaskWithATimeLimit(Task<User> fixtureTask)
    {
        const int circuitBreakerTimeoutSeconds = 30;
        var circuitBreakerTask = Task.Delay(TimeSpan.FromSeconds(circuitBreakerTimeoutSeconds));
        await Task.WhenAny(fixtureTask, circuitBreakerTask);
        return fixtureTask;
    }

    /// <summary>Model used to verify Newtonsoft field-name resolution.</summary>
    private sealed class NewtonsoftFieldNameModel
    {
        /// <summary>Gets or sets the aliased name property.</summary>
        [JsonProperty(PropertyName = "json_name")]
        public string? Name { get; set; }

        /// <summary>Gets or sets the unaliased property; present only so reflection can resolve a property without a JsonProperty attribute.</summary>
        public string? Unaliased { get; set; } = string.Empty;
    }

    /// <summary>Mock handler that asserts on the streamed request content and returns a configurable response.</summary>
    private sealed class MockPushStreamContentHttpMessageHandler : HttpMessageHandler
    {
        /// <summary>Gets or sets the delegate that asserts on the request content and produces the response content.</summary>
        public required Func<HttpContent, Task<HttpContent>> Asserts { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var content = request.Content;
            await Assert.That(content).IsNotNull();
            await Assert.That(Asserts).IsNotNull();

            var responseContent = await Asserts(content!).ConfigureAwait(false);

            return new(HttpStatusCode.OK) { Content = responseContent };
        }
    }

    /// <summary>HTTP content that only supports asynchronous reads, used to verify async-only deserialization.</summary>
    /// <param name="json">The JSON payload to serve as the content body.</param>
    private sealed class AsyncOnlyJsonContent(string json) : HttpContent
    {
        /// <summary>The UTF-8 encoded JSON payload.</summary>
        private readonly byte[] _bytes = Encoding.UTF8.GetBytes(json);

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            stream.WriteAsync(_bytes, 0, _bytes.Length);

        protected override bool TryComputeLength(out long length)
        {
            length = _bytes.Length;
            return true;
        }

        protected override Task<Stream> CreateContentReadStreamAsync() =>
            Task.FromResult<Stream>(new AsyncOnlyReadStream(_bytes));
    }

    /// <summary>Read-only stream that throws on synchronous reads to verify async-only consumption.</summary>
    /// <param name="bytes">The bytes the stream exposes for reading.</param>
    private sealed class AsyncOnlyReadStream(byte[] bytes) : Stream
    {
        /// <summary>The backing memory stream that supplies the bytes.</summary>
        private readonly MemoryStream _inner = new(bytes, writable: false);

        /// <inheritdoc/>
        public override bool CanRead => true;

        /// <inheritdoc/>
        public override bool CanSeek => _inner.CanSeek;

        /// <inheritdoc/>
        public override bool CanWrite => false;

        /// <inheritdoc/>
        public override long Length => _inner.Length;

        /// <inheritdoc/>
        public override long Position
        {
            get => _inner.Position;
            set => _inner.Position = value;
        }

        /// <inheritdoc/>
        public override void Flush() => _inner.Flush();

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException("Synchronous reads are intentionally not supported.");

        /// <inheritdoc/>
        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default) => _inner.ReadAsync(buffer, cancellationToken);

        /// <inheritdoc/>
        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken) => _inner.ReadAsync(buffer, offset, count, cancellationToken);

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

        /// <inheritdoc/>
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();
    }
}
