// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ReactiveUI.Primitives.Advanced;
using ReactiveUI.Primitives.Concurrency;
using ReactiveUI.Primitives.Signals;

namespace Refit.Tests;

/// <summary>Test helpers for awaiting observable results through concrete Primitives types.</summary>
internal static class ObservableTestHelpers
{
    /// <summary>The timeout used by observable integration tests.</summary>
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    /// <summary>Wraps the source in a timeout signal.</summary>
    /// <typeparam name="T">The observable value type.</typeparam>
    /// <param name="source">The source observable.</param>
    /// <returns>A timeout-wrapped observable.</returns>
    internal static IObservable<T> WithTimeout<T>(IObservable<T> source) =>
        new ExpireSignal<T>(source, DefaultTimeout, ThreadPoolSequencer.Instance);

    /// <summary>Awaits the timeout-wrapped source.</summary>
    /// <typeparam name="T">The observable value type.</typeparam>
    /// <param name="source">The source observable.</param>
    /// <returns>The final observable value.</returns>
    internal static Task<T> AwaitWithTimeout<T>(IObservable<T> source) =>
        Await(WithTimeout(source));

    /// <summary>Awaits the source through a concrete Primitives await signal.</summary>
    /// <typeparam name="T">The observable value type.</typeparam>
    /// <param name="source">The source observable.</param>
    /// <returns>The final observable value.</returns>
    internal static async Task<T> Await<T>(IObservable<T> source)
    {
        AsyncSignal<T> signal = new();
        using var subscription = source.Subscribe(signal);
        return await signal;
    }
}
