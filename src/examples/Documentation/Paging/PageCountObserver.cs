// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Documentation.Paging;

/// <summary>Counts values delivered by one subscription without retaining response pages.</summary>
/// <typeparam name="T">The item or page reported by the listing.</typeparam>
internal sealed class PageCountObserver<T> : IObserver<T>
{
    /// <summary>Completes when the last page has arrived or paging has failed.</summary>
    private readonly TaskCompletionSource<int> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>The number of values delivered to this subscription.</summary>
    private int _count;

    /// <inheritdoc/>
    public void OnNext(T value) => _count++;

    /// <inheritdoc/>
    public void OnCompleted() => _completion.SetResult(_count);

    /// <inheritdoc/>
    public void OnError(Exception error) => _completion.SetException(error);

    /// <summary>Counts the values for one subscription, disposing it when the caller cancels.</summary>
    /// <param name="values">The items or pages to observe.</param>
    /// <param name="token">Cancels the subscription wait.</param>
    /// <returns>The final count.</returns>
    internal static async Task<int> CountAsync(IObservable<T> values, CancellationToken token)
    {
        PageCountObserver<T> counter = new();
        using IDisposable subscription = values.Subscribe(counter);
        return await counter._completion.Task.WaitAsync(token);
    }
}
