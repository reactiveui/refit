// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Refit.Tests;

/// <summary>Verifies the observable adapter that turns an asynchronous sequence into a cold <see cref="IObservable{T}"/>.</summary>
public class AsyncEnumerableObservableTests
{
    /// <summary>The number of values the finite source produces.</summary>
    private const int ValueCount = 3;

    /// <summary>The longest a test waits for a notification.</summary>
    private static readonly TimeSpan AwaitTimeout = TimeSpan.FromSeconds(5);

    /// <summary>Verifies every value is delivered in order and followed by completion.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Subscribe_DeliversEveryValueThenCompletes()
    {
        var observer = new RecordingObserver<int>();

        using var subscription = new AsyncEnumerableObservable<int>(Count(ValueCount, null, default)).Subscribe(observer);
        await Assert.That(observer.Terminated.Task).CompletesWithin(AwaitTimeout);

        await Assert.That(string.Join(",", observer.Snapshot())).IsEqualTo("0,1,2");
        await Assert.That(observer.Completed).IsTrue();
        await Assert.That(observer.Error).IsNull();
    }

    /// <summary>Verifies a failure of the source is delivered as an error after the values that preceded it.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Subscribe_SourceFailure_ReachesOnError()
    {
        var observer = new RecordingObserver<int>();

        using var subscription = new AsyncEnumerableObservable<int>(FailAfterOne()).Subscribe(observer);
        await Assert.That(observer.Terminated.Task).CompletesWithin(AwaitTimeout);

        await Assert.That(string.Join(",", observer.Snapshot())).IsEqualTo("0");
        await Assert.That(observer.Error).IsTypeOf<InvalidOperationException>();
        await Assert.That(observer.Completed).IsFalse();
    }

    /// <summary>Verifies disposing a subscription cancels the sequence, disposes its enumerator, and delivers no terminal notification.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Dispose_CancelsTheSequenceAndDisposesTheEnumerator()
    {
        var firstDelivered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var sourceDisposed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var observer = new RecordingObserver<int> { OnNextStep = _ => firstDelivered.TrySetResult() };

        var subscription = new AsyncEnumerableObservable<int>(WaitAfterOne(sourceDisposed, false, default)).Subscribe(observer);
        await Assert.That(firstDelivered.Task).CompletesWithin(AwaitTimeout);
        subscription.Dispose();

        await Assert.That(sourceDisposed.Task).CompletesWithin(AwaitTimeout);
        await Assert.That(observer.Completed).IsFalse();
        await Assert.That(observer.Error).IsNull();
    }

    /// <summary>Verifies a failure raised by the source in response to cancellation is not delivered to the observer.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Dispose_FailureCausedByCancellation_IsNotDelivered()
    {
        var firstDelivered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var sourceDisposed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var observer = new RecordingObserver<int> { OnNextStep = _ => firstDelivered.TrySetResult() };

        var subscription = new AsyncEnumerableObservable<int>(WaitAfterOne(sourceDisposed, true, default)).Subscribe(observer);
        await Assert.That(firstDelivered.Task).CompletesWithin(AwaitTimeout);
        subscription.Dispose();

        await Assert.That(sourceDisposed.Task).CompletesWithin(AwaitTimeout);
        await Assert.That(observer.Error).IsNull();
        await Assert.That(observer.Completed).IsFalse();
    }

    /// <summary>Verifies a value the source produces after disposal is discarded and the enumerator is still disposed.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Dispose_ValueProducedAfterDisposal_IsNotDelivered()
    {
        var firstDelivered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var sourceDisposed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var observer = new RecordingObserver<int> { OnNextStep = _ => firstDelivered.TrySetResult() };

        var subscription = new AsyncEnumerableObservable<int>(IgnoreCancellation(release, sourceDisposed)).Subscribe(observer);
        await Assert.That(firstDelivered.Task).CompletesWithin(AwaitTimeout);
        subscription.Dispose();
        release.SetResult();

        await Assert.That(sourceDisposed.Task).CompletesWithin(AwaitTimeout);
        await Assert.That(string.Join(",", observer.Snapshot())).IsEqualTo("0");
        await Assert.That(observer.Completed).IsFalse();
    }

    /// <summary>Verifies a source that finishes asynchronously when disposed still completes the subscription and is disposed.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Subscribe_SourceWithAsynchronousDisposal_CompletesAndIsDisposed()
    {
        var sourceDisposed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var observer = new RecordingObserver<int>();

        using var subscription = new AsyncEnumerableObservable<int>(DisposeSlowly(sourceDisposed)).Subscribe(observer);

        await Assert.That(observer.Terminated.Task).CompletesWithin(AwaitTimeout);
        await Assert.That(sourceDisposed.Task).CompletesWithin(AwaitTimeout);
        await Assert.That(observer.Completed).IsTrue();
    }

    /// <summary>Verifies an observer that throws does not leave the source undisposed.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Subscribe_ObserverThrows_StillDisposesTheSource()
    {
        var sourceDisposed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var observer = new RecordingObserver<int> { OnNextStep = static _ => throw new InvalidOperationException("observer failed") };

        using var subscription = new AsyncEnumerableObservable<int>(Count(ValueCount, sourceDisposed, default)).Subscribe(observer);

        await Assert.That(sourceDisposed.Task).CompletesWithin(AwaitTimeout);
        await Assert.That(observer.Completed).IsFalse();
    }

    /// <summary>Verifies a source whose disposal fails after a subscription was disposed delivers nothing further.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Dispose_SourceDisposalFails_DeliversNothingFurther()
    {
        var firstDelivered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var sourceDisposed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var observer = new RecordingObserver<int> { OnNextStep = _ => firstDelivered.TrySetResult() };

        var subscription = new AsyncEnumerableObservable<int>(FailToDispose(release, sourceDisposed)).Subscribe(observer);
        await Assert.That(firstDelivered.Task).CompletesWithin(AwaitTimeout);
        subscription.Dispose();
        release.SetResult();

        await Assert.That(sourceDisposed.Task).CompletesWithin(AwaitTimeout);
        await Assert.That(string.Join(",", observer.Snapshot())).IsEqualTo("0");
        await Assert.That(observer.Error).IsNull();
    }

    /// <summary>Verifies every subscription enumerates the source afresh.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Subscribe_EverySubscriptionEnumeratesTheSourceAgain()
    {
        var observable = new AsyncEnumerableObservable<int>(Count(ValueCount, null, default));
        var first = new RecordingObserver<int>();
        var second = new RecordingObserver<int>();

        using var firstSubscription = observable.Subscribe(first);
        using var secondSubscription = observable.Subscribe(second);
        await Assert.That(first.Terminated.Task).CompletesWithin(AwaitTimeout);
        await Assert.That(second.Terminated.Task).CompletesWithin(AwaitTimeout);

        await Assert.That(string.Join(",", second.Snapshot())).IsEqualTo(string.Join(",", first.Snapshot()));
        await Assert.That(first.Snapshot().Count).IsEqualTo(ValueCount);
    }

    /// <summary>Verifies subscribing a null observer is rejected.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Subscribe_NullObserver_Throws()
    {
        var observable = new AsyncEnumerableObservable<int>(Count(ValueCount, null, default));

        await Assert.That(() => observable.Subscribe(null!)).Throws<ArgumentNullException>();
    }

    /// <summary>Produces the numbers from zero.</summary>
    /// <param name="count">The number of values.</param>
    /// <param name="disposed">Completed when the enumerator is disposed, or <see langword="null"/>.</param>
    /// <param name="cancellationToken">A token that cancels the sequence.</param>
    /// <returns>The sequence.</returns>
    private static async IAsyncEnumerable<int> Count(int count, TaskCompletionSource? disposed, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        try
        {
            for (var value = 0; value < count; value++)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                yield return value;
            }
        }
        finally
        {
            disposed?.TrySetResult();
        }
    }

    /// <summary>Produces one value and finishes asynchronously when disposed.</summary>
    /// <param name="disposed">Completed when the enumerator has finished disposing.</param>
    /// <returns>The sequence.</returns>
    private static async IAsyncEnumerable<int> DisposeSlowly(TaskCompletionSource disposed)
    {
        try
        {
            await Task.Yield();
            yield return 0;
        }
        finally
        {
            await Task.Yield();
            _ = disposed.TrySetResult();
        }
    }

    /// <summary>Produces one value, waits for a release, produces another, and fails when disposed.</summary>
    /// <param name="release">Completed by the test to let the sequence continue.</param>
    /// <param name="disposed">Completed when disposal begins.</param>
    /// <returns>The sequence.</returns>
    /// <exception cref="InvalidOperationException">Always, when disposed.</exception>
    private static async IAsyncEnumerable<int> FailToDispose(TaskCompletionSource release, TaskCompletionSource disposed)
    {
        try
        {
            await Task.Yield();
            yield return 0;
            await release.Task;
            yield return 1;
        }
        finally
        {
            _ = disposed.TrySetResult();
            await Task.FromException(new InvalidOperationException("disposal failed"));
        }
    }

    /// <summary>Produces one value, then fails.</summary>
    /// <returns>The sequence.</returns>
    /// <exception cref="InvalidOperationException">Always, after the first value.</exception>
    private static async IAsyncEnumerable<int> FailAfterOne()
    {
        await Task.Yield();
        yield return 0;
        throw new InvalidOperationException("source failed");
    }

    /// <summary>Produces one value, waits for a release that ignores cancellation, then produces another.</summary>
    /// <param name="release">Completed by the test to let the sequence continue.</param>
    /// <param name="disposed">Completed when the enumerator is disposed.</param>
    /// <returns>The sequence.</returns>
    private static async IAsyncEnumerable<int> IgnoreCancellation(TaskCompletionSource release, TaskCompletionSource disposed)
    {
        try
        {
            await Task.Yield();
            yield return 0;
            await release.Task;
            yield return 1;
        }
        finally
        {
            _ = disposed.TrySetResult();
        }
    }

    /// <summary>Produces one value, then waits for cancellation.</summary>
    /// <param name="disposed">Completed when the enumerator is disposed.</param>
    /// <param name="failOnCancellation">Whether the sequence fails with an unrelated exception when cancelled instead of propagating the cancellation.</param>
    /// <param name="cancellationToken">A token that cancels the sequence.</param>
    /// <returns>The sequence.</returns>
    /// <exception cref="InvalidOperationException">The sequence was cancelled and <paramref name="failOnCancellation"/> is set.</exception>
    private static async IAsyncEnumerable<int> WaitAfterOne(TaskCompletionSource disposed, bool failOnCancellation, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        try
        {
            await Task.Yield();
            yield return 0;
            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (OperationCanceledException) when (failOnCancellation)
            {
                throw new InvalidOperationException("failed while cancelling");
            }
        }
        finally
        {
            _ = disposed.TrySetResult();
        }
    }
}
