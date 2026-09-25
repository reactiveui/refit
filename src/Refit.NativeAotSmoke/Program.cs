// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Runtime.CompilerServices;
using System.Text.Json;
using Refit;
using Refit.NativeAotSmoke;

const int ExpectedTodoId = 42;

const string ExpectedTodoTitle = "prove native aot";

const int FormCount = 2;

const int SearchPage = 3;

const int SecondSearchId = 2;

var handler = new NativeAotSmokeHandler();

using var client = new HttpClient(handler) { BaseAddress = new("https://aot.refit.test") };

var api = SmokeApiFactory.Create(client, AotJsonContext.Default);

var created = await api.CreateTodoAsync(new(ExpectedTodoTitle)).ConfigureAwait(false);

if (created.Id != ExpectedTodoId || created.Title != ExpectedTodoTitle)
{
    throw new InvalidOperationException("The AOT POST response was not deserialized correctly.");
}

var described = await api.CreateDescribedTodoAsync(new(ExpectedTodoTitle), AotJsonContext.Default.Todo).ConfigureAwait(false);

if (described.Id != ExpectedTodoId || described.Title != ExpectedTodoTitle)
{
    throw new InvalidOperationException("The AOT method that takes JsonTypeInfo metadata did not round-trip the item.");
}

var formResponse = await api.SubmitFormAsync(new("Ada", FormCount)).ConfigureAwait(false);

if (formResponse != "accepted")
{
    throw new InvalidOperationException("The AOT form response was not returned correctly.");
}

var status = await api.GetStatusAsync().ConfigureAwait(false);

if (!status.IsSuccessStatusCode || status.Content?.Name != "native-aot")
{
    throw new InvalidOperationException("The AOT ApiResponse<T> result was not deserialized correctly.");
}

if (!handler.SawPostBody)
{
    throw new InvalidOperationException("The AOT request body was not serialized through Refit.");
}

// A generic method closed over a concrete type: generated inline (generic JSON body + SendAsync<T, TBody>), no reflection.
var echoed = await api.EchoAsync<Todo>(new("generic inline")).ConfigureAwait(false);

if (echoed.Id != ExpectedTodoId || echoed.Title != "generic inline")
{
    throw new InvalidOperationException("The AOT generic method result was not deserialized correctly.");
}

var searched = await api
    .SearchAsync("a b", SearchPage, [1, SecondSearchId], SmokeSort.DateDescending, "ready", "x%2Fy")
    .ConfigureAwait(false);

if (searched.Name != "native-aot" || !handler.SawExpectedQuery)
{
    throw new InvalidOperationException("The AOT generated query string was not constructed correctly.");
}

if (!handler.SawFormBody)
{
    throw new InvalidOperationException("The AOT URL-encoded request body was not serialized through generated Refit code.");
}

// The context does not describe SmokeUnregistered, so reading it fails instead of falling back to reflection.
try
{
    _ = await api.GetUnregisteredAsync().ConfigureAwait(false);
    throw new InvalidOperationException("The AOT client read a type its JSON context does not describe.");
}
catch (Exception ex) when (ex.GetBaseException() is NotSupportedException)
{
    // Expected: the generated JSON context is the only metadata source.
}

// A missed generated-client lookup forces the interface assembly's module initializer and looks again, which is what
// rescues runtimes that have not run it yet. Prove that forcing path is reachable under Native AOT.
string? missingClientMessage = null;

try
{
    _ = RestService.ForGenerated<INoGeneratedClientApi>(client);
}
catch (InvalidOperationException ex)
{
    missingClientMessage = ex.Message;
}

if (missingClientMessage?.Contains("doesn't look like a Refit interface", StringComparison.Ordinal) != true)
{
    throw new InvalidOperationException("The AOT lookup for a missing generated client did not report it.");
}

// Typed async JSON Lines upload: [Body(BodySerializationMethod.JsonLines)] IAsyncEnumerable<SmokeRecord>.
// Proves, under Native AOT with only the registered JSON context's metadata: (a) each element serializes as
// SmokeRecord (camelCase property names) and round-trips through the same context, (b) data reaches the handler
// before the producer finishes producing (backpressure - the producer blocks after its first element on a gate
// that only the handler opens, once it has observed the first bytes), and (c) cancelling the call's token
// mid-upload cancels the producer through its EnumeratorCancellation token, runs its finally block, stops it
// short of every record, and surfaces as OperationCanceledException.
const int UploadRecordCount = 5;

const int UploadTimeoutSeconds = 10;

var uploadRecordsYielded = 0;

var uploadRecordsYieldedWhenFirstLineReceived = new StrongBox<int>(-1);

using var uploadContinueGate = new SemaphoreSlim(0, 1);

handler.OnUploadFirstBytes = () =>
{
    uploadRecordsYieldedWhenFirstLineReceived.Value = Volatile.Read(ref uploadRecordsYielded);
    _ = uploadContinueGate.Release();
};

using var uploadTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(UploadTimeoutSeconds));

try
{
    await api.UploadAsync(
            ProduceUploadRecordsAsync(UploadRecordCount, uploadContinueGate, () => Interlocked.Increment(ref uploadRecordsYielded)),
            uploadTimeout.Token)
        .ConfigureAwait(false);
}
catch (OperationCanceledException) when (uploadTimeout.IsCancellationRequested)
{
    throw new InvalidOperationException(
        $"The AOT typed JSON Lines upload timed out after {UploadTimeoutSeconds}s waiting for the handler to observe the first line; the handler likely never received data incrementally.");
}

if (!handler.SawUploadBody || handler.UploadedBody is not { } uploadedBody)
{
    throw new InvalidOperationException("The AOT typed JSON Lines upload did not reach the handler.");
}

if (uploadRecordsYieldedWhenFirstLineReceived.Value != 1)
{
    throw new InvalidOperationException(
        "The AOT typed JSON Lines upload did not reach the handler while the producer was still producing; "
        + $"expected exactly 1 record yielded when the first bytes arrived, observed {uploadRecordsYieldedWhenFirstLineReceived.Value}.");
}

var uploadLines = uploadedBody.Split('\n', StringSplitOptions.RemoveEmptyEntries);

if (uploadLines.Length != UploadRecordCount)
{
    throw new InvalidOperationException($"The AOT typed JSON Lines upload wrote {uploadLines.Length} lines; expected {UploadRecordCount}.");
}

for (var i = 0; i < uploadLines.Length; i++)
{
    if (!uploadLines[i].Contains("\"sequence\":", StringComparison.Ordinal)
        || !uploadLines[i].Contains("\"label\":", StringComparison.Ordinal))
    {
        throw new InvalidOperationException(
            $"The AOT typed JSON Lines upload did not use the registered context's camelCase property names: {uploadLines[i]}");
    }

    var parsedRecord = JsonSerializer.Deserialize(uploadLines[i], AotJsonContext.Default.SmokeRecord);

    if (parsedRecord is null || parsedRecord.Sequence != i || parsedRecord.Label != $"line-{i}")
    {
        throw new InvalidOperationException(
            $"The AOT typed JSON Lines upload line did not round-trip through the registered context: {uploadLines[i]}");
    }
}

Console.WriteLine($"Native AOT Refit smoke test: typed async JSON Lines upload wrote {uploadLines.Length} lines, first line observed by the handler after exactly 1 record was produced.");

// Cancellation: never release the gate; only cancellation can end the producer's wait.
var cancelProducerFinallyRan = false;

var cancelProducerCompletedAll = false;

var cancelRecordsYielded = 0;

using var uploadCancel = new CancellationTokenSource();

using var cancelTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(UploadTimeoutSeconds));

using var cancelLink = CancellationTokenSource.CreateLinkedTokenSource(uploadCancel.Token, cancelTimeout.Token);

using var cancelGate = new SemaphoreSlim(0, 1);

handler.OnUploadFirstBytes = () => uploadCancel.Cancel();

var wasCancelled = false;

try
{
    await api.UploadAsync(
            ProduceUploadRecordsAsync(
                UploadRecordCount,
                cancelGate,
                () => Interlocked.Increment(ref cancelRecordsYielded),
                () => cancelProducerFinallyRan = true,
                () => cancelProducerCompletedAll = true),
            cancelLink.Token)
        .ConfigureAwait(false);
}
catch (OperationCanceledException)
{
    wasCancelled = true;
}

if (!wasCancelled)
{
    throw new InvalidOperationException("The AOT typed JSON Lines upload was not cancelled by the caller's token.");
}

if (cancelTimeout.IsCancellationRequested)
{
    throw new InvalidOperationException(
        "The AOT typed JSON Lines upload cancellation fell back to the smoke test's own timeout; the caller's cancellation token did not reach the producer.");
}

if (!cancelProducerFinallyRan)
{
    throw new InvalidOperationException("The AOT typed JSON Lines upload producer's finally block did not run after cancellation.");
}

if (cancelProducerCompletedAll)
{
    throw new InvalidOperationException("The AOT typed JSON Lines upload producer completed all records despite cancellation.");
}

if (cancelRecordsYielded >= UploadRecordCount)
{
    throw new InvalidOperationException("The AOT typed JSON Lines upload producer yielded every record despite cancellation.");
}

Console.WriteLine(
    $"Native AOT Refit smoke test: typed async JSON Lines upload cancellation stopped the producer after {cancelRecordsYielded} of {UploadRecordCount} records and threw OperationCanceledException.");

Console.WriteLine("Native AOT Refit smoke test passed.");

/// <summary>Produces <see cref="SmokeRecord"/> values lazily, one at a time, pausing after the first element until
/// <paramref name="continueAfterFirst"/> is released, so a consumer can observe backpressure. The token flows in
/// through <see cref="EnumeratorCancellationAttribute"/> because it is supplied by the collection's caller via
/// <c>GetAsyncEnumerator(CancellationToken)</c>, not by this method's own caller.</summary>
/// <param name="count">The number of records to produce.</param>
/// <param name="continueAfterFirst">Released to let the producer continue past its first element.</param>
/// <param name="onYielded">Invoked immediately before each record is handed to the consumer via <c>yield return</c>.</param>
/// <param name="onFinally">Invoked when the producer's <c>finally</c> block runs, on any exit path.</param>
/// <param name="onCompletedAll">Invoked once every record has been produced, before the method returns.</param>
/// <param name="enumeratorCancellationToken">The token supplied by the enumerable's caller.</param>
/// <returns>The lazily produced sequence.</returns>
static async IAsyncEnumerable<SmokeRecord> ProduceUploadRecordsAsync(
    int count,
    SemaphoreSlim continueAfterFirst,
    Action onYielded,
    Action? onFinally = null,
    Action? onCompletedAll = null,
    [EnumeratorCancellation] CancellationToken enumeratorCancellationToken = default)
{
    try
    {
        for (var i = 0; i < count; i++)
        {
            enumeratorCancellationToken.ThrowIfCancellationRequested();

            // Counted before the yield: the consumer's MoveNextAsync call that returns this element completes
            // (and the element starts serializing) before this iterator resumes past the yield point, so the
            // count must already reflect this element by the time the consumer observes it.
            onYielded();
            yield return new SmokeRecord(i, $"line-{i}");

            if (i == 0)
            {
                await continueAfterFirst.WaitAsync(enumeratorCancellationToken).ConfigureAwait(false);
            }
        }

        onCompletedAll?.Invoke();
    }
    finally
    {
        onFinally?.Invoke();
    }
}

/// <summary>The generated top-level program's declaring type, sealed so the JIT can devirtualize its members.</summary>
internal sealed partial class Program
{
    /// <summary>Initializes a new instance of the <see cref="Program"/> class. Unused; the entry point is the generated top-level <c>Main</c>.</summary>
    private Program()
    {
    }
}
