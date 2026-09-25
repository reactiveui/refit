// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Refit.Tests;

/// <summary>
/// Tests for <see cref="JsonLinesContent{T}"/> and the typed generated JSON Lines body factories
/// <see cref="GeneratedRequestRunner.CreateTypedJsonLinesBodyContent{TElement}"/> and
/// <see cref="GeneratedRequestRunner.CreateAsyncJsonLinesBodyContent{TElement}"/>.
/// </summary>
public class JsonLinesContentTypedTests
{
    /// <summary>The number of elements produced by the sample sequences used to exercise ordering and disposal.</summary>
    private const int SampleElementCount = 3;

    /// <summary>The number of sends performed by the re-enumeration test.</summary>
    private const int TwoSends = 2;

    /// <summary>The name of the first element produced by a two-element gated sequence.</summary>
    private const string FirstElementName = "first";

    /// <summary>The name of the second element produced by a two-element gated sequence.</summary>
    private const string SecondElementName = "second";

    /// <summary>The serializer used directly by these tests.</summary>
    private static readonly SystemTextJsonContentSerializer Serializer = new();

    /// <summary>The options used by the "converter handles object" fallback test: a converter that reports it can convert <see cref="object"/>.</summary>
    private static readonly System.Text.Json.JsonSerializerOptions ObjectConverterOptions = new() { Converters = { new ObjectConverter() } };

    /// <summary>The timeout used by every wait in the flush ordering test, so a regression fails instead of hanging.</summary>
    private static readonly TimeSpan FlushWaitTimeout = TimeSpan.FromSeconds(10);

    /// <summary>Sample integer elements used to exercise the value-type typed factory path.</summary>
    private static readonly int[] SampleIntElements = [1, 2, SampleElementCount];

    /// <summary>Verifies the synchronous constructor rejects a null item sequence.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SyncConstructorRejectsNullItems() =>
        await Assert
            .That(static () => new JsonLinesContent<SealedRecord>((IEnumerable<SealedRecord>)null!, Serializer))
            .ThrowsExactly<ArgumentNullException>();

    /// <summary>Verifies the synchronous constructor rejects a null serializer.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SyncConstructorRejectsNullSerializer() =>
        await Assert
            .That(static () => new JsonLinesContent<SealedRecord>((IEnumerable<SealedRecord>)[], null!))
            .ThrowsExactly<ArgumentNullException>();

    /// <summary>Verifies the asynchronous constructor rejects a null item sequence.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AsyncConstructorRejectsNullItems() =>
        await Assert
            .That(static () => new JsonLinesContent<SealedRecord>((IAsyncEnumerable<SealedRecord>)null!, Serializer))
            .ThrowsExactly<ArgumentNullException>();

    /// <summary>Verifies the asynchronous constructor rejects a null serializer.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AsyncConstructorRejectsNullSerializer() =>
        await Assert
            .That(static () => new JsonLinesContent<SealedRecord>(EmptyAsync(), null!))
            .ThrowsExactly<ArgumentNullException>();

    /// <summary>Verifies a synchronous typed content writes byte-identical output to the non-generic content.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SyncOutputMatchesNonGenericContent()
    {
        SealedRecord[] items = [new("1", "a"), new("2", "b")];

        using var typed = new JsonLinesContent<SealedRecord>(items, Serializer);
        using var untyped = new JsonLinesContent(items, Serializer);

        await Assert.That(await typed.ReadAsStringAsync()).IsEqualTo(await untyped.ReadAsStringAsync());
    }

    /// <summary>Verifies an asynchronous source writes one JSON line per element, in order.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AsyncOutputWritesOneLinePerElement()
    {
        using var content = new JsonLinesContent<SealedRecord>(ProduceAsync(SampleElementCount), Serializer);

        var body = await content.ReadAsStringAsync();

        await Assert.That(body).IsEqualTo("{\"id\":\"0\",\"name\":\"n0\"}\n{\"id\":\"1\",\"name\":\"n1\"}\n{\"id\":\"2\",\"name\":\"n2\"}\n");
    }

    /// <summary>Verifies a synchronous source is re-enumerated on every send.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SyncSourceReEnumeratesOnEverySend()
    {
        var enumerations = 0;
        using var content = new JsonLinesContent<SealedRecord>(CountedItems(), Serializer);

        // HttpContent.ReadAsStringAsync buffers after the first call and would serve the second call from that
        // buffer without re-serializing, so send twice through CopyToAsync (what a real retry does) instead.
        await using (var first = new MemoryStream())
        {
            await content.CopyToAsync(first);
        }

        await using (var second = new MemoryStream())
        {
            await content.CopyToAsync(second);
        }

        await Assert.That(enumerations).IsEqualTo(TwoSends);

        IEnumerable<SealedRecord> CountedItems()
        {
            enumerations++;
            yield return new("1", "a");
        }
    }

    /// <summary>Verifies sending content built from an async source a second time throws.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AsyncSourceThrowsOnSecondSend()
    {
        using var content = new JsonLinesContent<SealedRecord>(ProduceAsync(1), Serializer);

        await using (var first = new MemoryStream())
        {
            await content.CopyToAsync(first);
        }

        await Assert
            .That(async () =>
            {
                await using var second = new MemoryStream();
                await content.CopyToAsync(second);
            })
            .ThrowsExactly<InvalidOperationException>();
    }

    /// <summary>Verifies the async enumerator is disposed once enumeration completes normally.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EnumeratorDisposedOnCompletion()
    {
        var tracker = new DisposeTracker();
        using var content = new JsonLinesContent<SealedRecord>(TrackedAsync(tracker, throwAt: -1), Serializer);

        _ = await content.ReadAsStringAsync();

        await Assert.That(tracker.Disposed).IsTrue();
    }

    /// <summary>Verifies the async enumerator is disposed when the producer throws.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EnumeratorDisposedOnProducerException()
    {
        var tracker = new DisposeTracker();
        using var content = new JsonLinesContent<SealedRecord>(TrackedAsync(tracker, throwAt: 1), Serializer);

        await Assert.That(async () =>
        {
            await using var target = new MemoryStream();
            await content.CopyToAsync(target);
        }).Throws<InvalidOperationException>();

        await Assert.That(tracker.Disposed).IsTrue();
    }

#if NET8_0_OR_GREATER
    /// <summary>Verifies the async enumerator is disposed when the send is cancelled.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EnumeratorDisposedOnCancellation()
    {
        var tracker = new DisposeTracker();
        using var cts = new CancellationTokenSource();
        using var content = new JsonLinesContent<SealedRecord>(CancellingAsync(tracker, cts), Serializer);

        await Assert.That(async () =>
        {
            await using var target = new MemoryStream();
            await content.CopyToAsync(target, cts.Token);
        }).Throws<OperationCanceledException>();

        await Assert.That(tracker.Disposed).IsTrue();
    }

    /// <summary>Verifies the send's cancellation token reaches the producer through <see cref="EnumeratorCancellationAttribute"/>.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CancellationTokenReachesProducer()
    {
        using var cts = new CancellationTokenSource();
        CancellationToken? observed = null;
        using var content = new JsonLinesContent<SealedRecord>(CaptureTokenAsync(t => observed = t), Serializer);

        await using var target = new MemoryStream();
        await content.CopyToAsync(target, cts.Token);

        await Assert.That(observed).IsEqualTo(cts.Token);
    }

    /// <summary>Verifies lines already written are flushed to the stream before waiting on a producer that has not yielded its next element.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task FlushesBeforeWaitingOnAPendingProducer()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var flushedBeforeSecondItem = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var recording = new FlushRecordingStream(flushedBeforeSecondItem);
        var content = new JsonLinesContent<SealedRecord>(GatedAsync(gate), Serializer);
        try
        {
            var copy = content.CopyToAsync(recording, CancellationToken.None);

            // The producer is now suspended waiting on the gate after yielding the first element; the flush that
            // precedes the wait must already have run.
            await Assert.That(await flushedBeforeSecondItem.Task.WaitAsync(FlushWaitTimeout)).IsTrue();

            gate.SetResult();
            await copy.WaitAsync(FlushWaitTimeout);
        }
        finally
        {
            content.Dispose();
        }

        static async IAsyncEnumerable<SealedRecord> GatedAsync(TaskCompletionSource gate)
        {
            yield return new("1", FirstElementName);
            await gate.Task;
            yield return new("2", SecondElementName);
        }
    }

    /// <summary>Verifies a flush failure that occurs while the producer's next step is still pending is rethrown
    /// only after that step completes, so the enumerator is disposed once, after the pending step finishes rather
    /// than while it is still in flight.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task FlushFailurePropagatesAfterThePendingProducerStepCompletes()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var flushInvoked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tracker = new DisposeTracker();
        var stream = new FlushThrowingStream(flushInvoked);
        var content = new JsonLinesContent<SealedRecord>(GatedTrackedAsync(gate, tracker), Serializer);
        try
        {
            var copy = content.CopyToAsync(stream, CancellationToken.None);

            // The flush that precedes the wait on the still-pending second element has already run (and failed) by
            // the time this point is reached: CopyToAsync only suspends once it starts awaiting that pending step.
            await flushInvoked.Task.WaitAsync(FlushWaitTimeout);
            await Assert.That(tracker.Disposed).IsFalse();

            gate.SetResult();

            // HttpContent.CopyToAsync wraps a stream-copy failure in HttpRequestException; the flush failure
            // survives as its InnerException.
            var exception = await Assert.That(async () => await copy).ThrowsExactly<HttpRequestException>();

            await Assert.That(exception!.InnerException).IsTypeOf<IOException>();
        }
        finally
        {
            content.Dispose();
        }

        await Assert.That(tracker.Disposed).IsTrue();
    }

    /// <summary>
    /// Verifies the token-less serialize override, the entry point on .NET Framework where the cancellable overload
    /// does not exist, writes the same body as a normal send.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Security",
        "SES1406:Avoid reaching non-public members through reflection",
        Justification = "HttpContent on .NET 8+ always calls the cancellable overload; reflection is the only way to exercise the .NET Framework entry point here.")]
    public async Task TokenlessSerializeOverrideWritesTheSameBody()
    {
        using var content = new JsonLinesContent<SealedRecord>([new("1", "a"), new("2", "b")], Serializer);
        var tokenless = typeof(JsonLinesContent<SealedRecord>).GetMethod(
            "SerializeToStreamAsync",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            [typeof(Stream), typeof(System.Net.TransportContext)])!;

        await using var direct = new MemoryStream();
        await (Task)tokenless.Invoke(content, [direct, null])!;
        await using var sent = new MemoryStream();
        await content.CopyToAsync(sent);

        await Assert.That(direct.ToArray()).IsEquivalentTo(sent.ToArray());
    }
#endif

    /// <summary>Verifies the typed factory falls back to the untyped content for a non-<see cref="SystemTextJsonContentSerializer"/> serializer.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CreateTypedFallsBackForNonSystemTextJsonSerializer()
    {
        var settings = new RefitSettings(new PlainSerializer());

        var content = GeneratedRequestRunner.CreateTypedJsonLinesBodyContent(settings, (IEnumerable<SealedRecord>)[new("1", "a")]);

        await Assert.That(content).IsTypeOf<JsonLinesContent>();
    }

    /// <summary>Verifies the typed factory falls back to the untyped content when a converter can handle <see cref="object"/>.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CreateTypedFallsBackWhenAConverterHandlesObject()
    {
        var settings = new RefitSettings(new SystemTextJsonContentSerializer(ObjectConverterOptions));

        var content = GeneratedRequestRunner.CreateTypedJsonLinesBodyContent(settings, (IEnumerable<SealedRecord>)[new("1", "a")]);

        await Assert.That(content).IsTypeOf<JsonLinesContent>();
    }

    /// <summary>Verifies the typed factory falls back to the untyped content for a non-sealed reference element type.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CreateTypedFallsBackForNonSealedElement()
    {
        var content = GeneratedRequestRunner.CreateTypedJsonLinesBodyContent(new(), (IEnumerable<NonSealedRecord>)[new NonSealedRecord()]);

        await Assert.That(content).IsTypeOf<JsonLinesContent>();
    }

    /// <summary>Verifies the typed factory falls back to the untyped content for a <see langword="null"/> body.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CreateTypedFallsBackForNullBody()
    {
        var content = GeneratedRequestRunner.CreateTypedJsonLinesBodyContent<SealedRecord>(new(), null);

        await Assert.That(content).IsTypeOf<JsonLinesContent>();
        await Assert.That(await content.ReadAsStringAsync()).IsEqualTo("null");
    }

    /// <summary>Verifies the typed factory selects <see cref="JsonLinesContent{T}"/> for a sealed reference element type with the built-in serializer.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CreateTypedSelectsTypedContentForSealedElement()
    {
        var content = GeneratedRequestRunner.CreateTypedJsonLinesBodyContent(new(), (IEnumerable<SealedRecord>)[new("1", "a")]);

        await Assert.That(content).IsTypeOf<JsonLinesContent<SealedRecord>>();
    }

    /// <summary>Verifies the typed factory selects <see cref="JsonLinesContent{T}"/> for a value-type element.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CreateTypedSelectsTypedContentForValueTypeElement()
    {
        var content = GeneratedRequestRunner.CreateTypedJsonLinesBodyContent(new(), (IEnumerable<int>)SampleIntElements);

        await Assert.That(content).IsTypeOf<JsonLinesContent<int>>();
    }

    /// <summary>Verifies the async factory writes a single <c>null</c> line for a <see langword="null"/> body.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CreateAsyncWritesSingleNullLineForNullBody()
    {
        var content = GeneratedRequestRunner.CreateAsyncJsonLinesBodyContent<SealedRecord>(new(), null);

        await Assert.That(content).IsTypeOf<JsonLinesContent>();
        await Assert.That(await content.ReadAsStringAsync()).IsEqualTo("null");
    }

    /// <summary>Verifies the async factory selects the typed content for a real async source.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CreateAsyncSelectsTypedContentForAsyncSource()
    {
        var content = GeneratedRequestRunner.CreateAsyncJsonLinesBodyContent(new(), ProduceAsync(1));

        await Assert.That(content).IsTypeOf<JsonLinesContent<SealedRecord>>();
    }

    /// <summary>Verifies each element is serialized as the declared element type, not its runtime type: a derived
    /// instance passed through a base-typed content writes only the base's members.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ElementsAreSerializedAsTheDeclaredType()
    {
        BaseRecord[] items = [new DerivedRecord { Id = "1", Extra = "hidden" }];

        using var content = new JsonLinesContent<BaseRecord>(items, Serializer);

        var body = await content.ReadAsStringAsync();

        await Assert.That(body).IsEqualTo("{\"id\":\"1\"}");
        await Assert.That(body).DoesNotContain("hidden");
    }

    /// <summary>Produces a fixed number of elements asynchronously.</summary>
    /// <param name="count">The number of elements to produce.</param>
    /// <returns>The asynchronous sequence.</returns>
    private static async IAsyncEnumerable<SealedRecord> ProduceAsync(int count)
    {
        for (var i = 0; i < count; i++)
        {
            await Task.Yield();
            yield return new(i.ToString(), $"n{i}");
        }
    }

    /// <summary>An empty asynchronous sequence, used only to reach the constructor null checks.</summary>
    /// <returns>The empty asynchronous sequence.</returns>
    private static async IAsyncEnumerable<SealedRecord> EmptyAsync()
    {
        await Task.CompletedTask;
        yield break;
    }

    /// <summary>Produces elements while recording enumerator disposal, optionally throwing before a given index.</summary>
    /// <param name="tracker">Records whether the enumerator was disposed.</param>
    /// <param name="throwAt">The zero-based index to throw at, or -1 to never throw.</param>
    /// <returns>The asynchronous sequence.</returns>
    /// <exception cref="InvalidOperationException">Thrown from within the sequence at index <paramref name="throwAt"/>.</exception>
    private static async IAsyncEnumerable<SealedRecord> TrackedAsync(DisposeTracker tracker, int throwAt)
    {
        try
        {
            for (var i = 0; i < SampleElementCount; i++)
            {
                if (i == throwAt)
                {
                    throw new InvalidOperationException("producer failure");
                }

                await Task.Yield();
                yield return new(i.ToString(), $"n{i}");
            }
        }
        finally
        {
            tracker.Disposed = true;
        }
    }

#if NET8_0_OR_GREATER
    /// <summary>Produces elements, cancelling the token source after the first one, and records enumerator disposal.</summary>
    /// <param name="tracker">Records whether the enumerator was disposed.</param>
    /// <param name="cts">The token source to cancel after the first element.</param>
    /// <param name="cancellationToken">The token bound to the enumerator, used to observe the cancellation.</param>
    /// <returns>The asynchronous sequence.</returns>
    private static async IAsyncEnumerable<SealedRecord> CancellingAsync(
        DisposeTracker tracker,
        CancellationTokenSource cts,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        try
        {
            yield return new("1", FirstElementName);
            await cts.CancelAsync();
            cancellationToken.ThrowIfCancellationRequested();
            yield return new("2", SecondElementName);
        }
        finally
        {
            tracker.Disposed = true;
        }
    }

    /// <summary>Produces one element and records the cancellation token bound to the enumerator.</summary>
    /// <param name="capture">Invoked with the token bound by <see cref="EnumeratorCancellationAttribute"/>.</param>
    /// <param name="cancellationToken">The token bound to the enumerator.</param>
    /// <returns>The asynchronous sequence.</returns>
    private static async IAsyncEnumerable<SealedRecord> CaptureTokenAsync(
        Action<CancellationToken> capture,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        capture(cancellationToken);
        await Task.CompletedTask;
        yield return new("1", "a");
    }

    /// <summary>Produces two elements, awaiting a gate between them, and records enumerator disposal.</summary>
    /// <param name="gate">Completed to release the second element.</param>
    /// <param name="tracker">Records whether the enumerator was disposed.</param>
    /// <returns>The asynchronous sequence.</returns>
    private static async IAsyncEnumerable<SealedRecord> GatedTrackedAsync(TaskCompletionSource gate, DisposeTracker tracker)
    {
        try
        {
            yield return new("1", FirstElementName);
            await gate.Task;
            yield return new("2", SecondElementName);
        }
        finally
        {
            tracker.Disposed = true;
        }
    }
#endif

    /// <summary>A non-sealed reference-type element, not eligible for the typed JSON Lines path.</summary>
    public class NonSealedRecord
    {
        /// <summary>Gets or sets an identifier, present only so the type is not an empty placeholder.</summary>
        public string? Id { get; set; }
    }

    /// <summary>A non-sealed base type used to prove elements serialize as their declared type.</summary>
    public class BaseRecord
    {
        /// <summary>Gets or sets the identifier.</summary>
        public string? Id { get; set; }
    }

    /// <summary>A type derived from <see cref="BaseRecord"/> with an extra member that must not be written when passed as the base type.</summary>
    public sealed class DerivedRecord : BaseRecord
    {
        /// <summary>Gets or sets an extra member absent from <see cref="BaseRecord"/>.</summary>
        public string? Extra { get; set; }
    }

    /// <summary>Records whether an async enumerator was disposed.</summary>
    private sealed class DisposeTracker
    {
        /// <summary>Gets or sets a value indicating whether the enumerator's finally block ran.</summary>
        internal bool Disposed { get; set; }
    }

    /// <summary>A content serializer that is not <see cref="SystemTextJsonContentSerializer"/>.</summary>
    private sealed class PlainSerializer : IHttpContentSerializer
    {
        /// <inheritdoc/>
        public HttpContent ToHttpContent<T>(T item) => new StringContent(string.Empty);

        /// <inheritdoc/>
        public Task<T?> FromHttpContentAsync<T>(HttpContent content, CancellationToken cancellationToken = default) =>
            Task.FromResult<T?>(default);

        /// <inheritdoc/>
        public string? GetFieldNameForProperty(System.Reflection.PropertyInfo propertyInfo) => null;
    }

    /// <summary>A converter that reports it can convert <see cref="object"/>, forcing the untyped fallback.</summary>
    private sealed class ObjectConverter : JsonConverter<object>
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(object);

        /// <inheritdoc/>
        public override object? Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options) =>
            null;

        /// <inheritdoc/>
        public override void Write(System.Text.Json.Utf8JsonWriter writer, object value, System.Text.Json.JsonSerializerOptions options) =>
            writer.WriteNullValue();
    }

#if NET8_0_OR_GREATER
    /// <summary>A stream that signals once a flush is observed after the first write, before a second write arrives.</summary>
    /// <param name="flushedBeforeSecondItem">Completed the first time a flush is observed after at least one write.</param>
    private sealed class FlushRecordingStream(TaskCompletionSource<bool> flushedBeforeSecondItem) : MemoryStream
    {
        /// <summary>The number of writes observed so far.</summary>
        private int _writes;

        /// <inheritdoc/>
        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            await base.FlushAsync(cancellationToken);
            if (_writes >= 1)
            {
                _ = flushedBeforeSecondItem.TrySetResult(true);
            }
        }

        /// <inheritdoc/>
        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await base.WriteAsync(buffer, cancellationToken);
            _writes++;
        }
    }

    /// <summary>A stream whose flush always fails, recording that it was invoked before throwing.</summary>
    /// <param name="flushInvoked">Completed the moment <see cref="FlushAsync"/> is invoked, before it throws.</param>
    private sealed class FlushThrowingStream(TaskCompletionSource flushInvoked) : MemoryStream
    {
        /// <inheritdoc/>
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            _ = flushInvoked.TrySetResult();
            throw new IOException("Flush failed.");
        }
    }
#endif

    /// <summary>A sealed reference-type element, eligible for the typed JSON Lines path.</summary>
    /// <param name="Id">The identifier.</param>
    /// <param name="Name">The name.</param>
    public sealed record SealedRecord(string Id, string Name);
}
