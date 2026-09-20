// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>An observer that records every notification it receives and signals when the sequence terminates.</summary>
/// <typeparam name="T">The element type.</typeparam>
internal sealed class RecordingObserver<T> : IObserver<T>
{
    /// <summary>The elements received, in order.</summary>
    private readonly List<T> _values = [];

    /// <summary>Serializes access to <see cref="_values"/>.</summary>
    private readonly Lock _gate = new();

    /// <summary>Gets a task that completes when the sequence completes or fails.</summary>
    internal TaskCompletionSource Terminated { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Gets or sets a step run for each element before the next is awaited.</summary>
    internal Action<T>? OnNextStep { get; set; }

    /// <summary>Gets a value indicating whether the sequence completed normally.</summary>
    internal bool Completed { get; private set; }

    /// <summary>Gets the failure the sequence ended with, or <see langword="null"/>.</summary>
    internal Exception? Error { get; private set; }

    /// <inheritdoc/>
    public void OnNext(T value)
    {
        lock (_gate)
        {
            _values.Add(value);
        }

        OnNextStep?.Invoke(value);
    }

    /// <inheritdoc/>
    public void OnError(Exception error)
    {
        Error = error;
        _ = Terminated.TrySetResult();
    }

    /// <inheritdoc/>
    public void OnCompleted()
    {
        Completed = true;
        _ = Terminated.TrySetResult();
    }

    /// <summary>Gets a snapshot of the elements received so far.</summary>
    /// <returns>The elements in order.</returns>
    internal List<T> Snapshot()
    {
        lock (_gate)
        {
            return [.. _values];
        }
    }
}
