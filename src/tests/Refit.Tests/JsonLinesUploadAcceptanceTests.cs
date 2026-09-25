// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net;
using System.Runtime.CompilerServices;
using Refit.Testing;

namespace Refit.Tests;

/// <summary>
/// End-to-end acceptance tests for a generated client uploading an <see cref="IAsyncEnumerable{T}"/> body as JSON
/// Lines, created with <see cref="RestService.ForGenerated{T}(HttpClient, System.Text.Json.Serialization.JsonSerializerContext)"/>
/// over a source-generated JSON context (so the body is written from typed metadata, never reflection). A
/// <see cref="StubHttp"/> handler with <see cref="RequestCapture.None"/> and a
/// <see cref="Reply.From(Func{HttpRequestMessage, CancellationToken, Task{HttpResponseMessage}})"/> responder reads the
/// request body by copying it directly, so a stubbed server observes bytes exactly as a real network handler would:
/// incrementally, as the producer yields them, not after the whole body has been buffered.
/// </summary>
public sealed class JsonLinesUploadAcceptanceTests
{
    /// <summary>The base address of the client under test.</summary>
    private const string BaseAddress = "http://uploads.test/";

    /// <summary>The route the upload is posted to.</summary>
    private const string UploadPath = "/upload";

    /// <summary>The number of records the producer yields.</summary>
    private const int RecordCount = 5;

    /// <summary>The timeout used by every wait, so a regression fails the test instead of hanging.</summary>
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Verifies the stubbed server receives the first record's bytes while the producer is still suspended
    /// producing the rest: the producer blocks, after yielding its first record, on a gate that only opens once the
    /// server-side responder has observed that record's bytes. If the body were serialized eagerly before the send
    /// (buffering the whole sequence), the gate would never open and the wait would time out.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UploadReachesTheConsumerBeforeProductionCompletes()
    {
        var produced = new List<int>();
        var firstRecordObserved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var observer = new SignalingStream(firstRecordObserved);
        var handler = new StubHttp { RequestCapture = RequestCapture.None };
        handler.Add(
            Route.Post(UploadPath),
            Reply.From(async (request, cancellationToken) =>
            {
                await request.Content!.CopyToAsync(observer, cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }));
        using var client = HttpClientTestFactory.Create(handler, new(BaseAddress));
        var api = RestService.ForGenerated<IJsonLinesUploadApi>(client, UploadJsonContext.Default);

        await api.Upload(ProduceGatedAsync(RecordCount, produced, firstRecordObserved), CancellationToken.None);

        await Assert.That(produced.Count).IsEqualTo(RecordCount);
        await Assert.That(firstRecordObserved.Task.IsCompletedSuccessfully).IsTrue();
    }

    /// <summary>
    /// Verifies cancelling the call mid-upload (once the consumer has already observed the first record, so the
    /// producer is confirmed to be suspended waiting rather than already finished) stops the call with a
    /// cancellation exception, runs the producer's <see langword="finally"/> block (disposing its enumerator), and
    /// never produces every record.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CancellingMidUploadStopsTheProducerAndUnwindsCleanly()
    {
        var produced = new List<int>();
        var finallyRan = new bool[1];
        var cancellationObservedByProducer = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstRecordObserved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var observer = new SignalingStream(firstRecordObserved);
        var handler = new StubHttp { RequestCapture = RequestCapture.None };
        handler.Add(
            Route.Post(UploadPath),
            Reply.From(async (request, cancellationToken) =>
            {
                await request.Content!.CopyToAsync(observer, cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }));
        using var client = HttpClientTestFactory.Create(handler, new(BaseAddress));
        var api = RestService.ForGenerated<IJsonLinesUploadApi>(client, UploadJsonContext.Default);

        using var cts = new CancellationTokenSource();
        var call = api.Upload(
            ProduceUntilCancelledAsync(RecordCount, produced, finallyRan, cancellationObservedByProducer),
            cts.Token);

        // Confirmed the server already observed the first record's bytes: the producer is genuinely suspended, not
        // merely about to run.
        await firstRecordObserved.Task.WaitAsync(WaitTimeout);
        await cts.CancelAsync();

        await Assert.That(async () => await call).Throws<OperationCanceledException>();
        await cancellationObservedByProducer.Task.WaitAsync(WaitTimeout);
        await Assert.That(finallyRan[0]).IsTrue();
        await Assert.That(produced.Count).IsLessThan(RecordCount);
    }

    /// <summary>Produces records, suspending after the first one until <paramref name="gate"/> completes.</summary>
    /// <param name="count">The number of records to produce.</param>
    /// <param name="produced">Records the index of every record produced.</param>
    /// <param name="gate">Opened by the test once the consumer has observed the first record's bytes.</param>
    /// <returns>The asynchronous sequence of records.</returns>
    private static async IAsyncEnumerable<UploadRecord> ProduceGatedAsync(
        int count,
        List<int> produced,
        TaskCompletionSource gate)
    {
        for (var i = 0; i < count; i++)
        {
            produced.Add(i);
            yield return new(i, $"r{i}");
            if (i == 0)
            {
                await gate.Task.WaitAsync(WaitTimeout);
            }
        }
    }

    /// <summary>Produces records, suspending indefinitely after the first one until cancelled.</summary>
    /// <param name="count">The number of records to produce.</param>
    /// <param name="produced">Records the index of every record produced.</param>
    /// <param name="finallyRan">Set once the producer's <see langword="finally"/> block runs.</param>
    /// <param name="cancellationObserved">Completed once the producer's own token reports the cancellation.</param>
    /// <param name="cancellationToken">The token bound to the enumerator by the sending content.</param>
    /// <returns>The asynchronous sequence of records.</returns>
    private static async IAsyncEnumerable<UploadRecord> ProduceUntilCancelledAsync(
        int count,
        List<int> produced,
        bool[] finallyRan,
        TaskCompletionSource cancellationObserved,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        try
        {
            for (var i = 0; i < count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                produced.Add(i);
                yield return new(i, $"r{i}");
                if (i == 0)
                {
                    await Task.Delay(Timeout.Infinite, cancellationToken);
                }
            }
        }
        finally
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _ = cancellationObserved.TrySetResult();
            }

            finallyRan[0] = true;
        }
    }

    /// <summary>A write-only stream that signals once its first write is observed, discarding the bytes written.</summary>
    /// <param name="firstWriteObserved">Completed the first time a write is observed.</param>
    private sealed class SignalingStream(TaskCompletionSource firstWriteObserved) : Stream
    {
        /// <inheritdoc/>
        public override bool CanRead => false;

        /// <inheritdoc/>
        public override bool CanSeek => false;

        /// <inheritdoc/>
        public override bool CanWrite => true;

        /// <inheritdoc/>
        public override long Length => throw new NotSupportedException();

        /// <inheritdoc/>
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override void Flush()
        {
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        /// <inheritdoc/>
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count) => _ = firstWriteObserved.TrySetResult();

        /// <inheritdoc/>
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            _ = firstWriteObserved.TrySetResult();
            return ValueTask.CompletedTask;
        }
    }
}
