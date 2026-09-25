// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>A cold observable that enumerates an asynchronous sequence once per subscription and stops when the subscription is disposed.</summary>
/// <typeparam name="T">The element type.</typeparam>
/// <param name="source">The sequence enumerated for each subscription.</param>
[System.Diagnostics.DebuggerDisplay("AsyncEnumerableObservable<{typeof(T).Name,nq}>")]
internal sealed class AsyncEnumerableObservable<T>(IAsyncEnumerable<T> source) : IObservable<T>
{
    /// <inheritdoc/>
    public IDisposable Subscribe(IObserver<T> observer)
    {
        ArgumentExceptionHelper.ThrowIfNull(observer);

        var subscription = new Subscription(observer);
        subscription.Start(source);
        return subscription;
    }

    /// <summary>Delivers a sequence to one observer and cancels it, mid-request if need be, when disposed.</summary>
    [System.Diagnostics.DebuggerDisplay("Finished = {_finished}")]
    private sealed class Subscription : IDisposable
    {
        /// <summary>The downstream observer.</summary>
        private readonly IObserver<T> _observer;

        /// <summary>The source of the cancellation observed by the sequence.</summary>
        private readonly CancellationTokenSource _cancellation = new();

        /// <summary>Serializes cancelling and disposing <see cref="_cancellation"/>.</summary>
        private readonly Lock _gate = new();

        /// <summary>Whether the subscription has ended, by disposal or by the sequence finishing.</summary>
        private bool _finished;

        /// <summary>Initializes a new instance of the <see cref="Subscription"/> class.</summary>
        /// <param name="observer">The downstream observer.</param>
        internal Subscription(IObserver<T> observer) => _observer = observer;

        /// <summary>Cancels the sequence and releases the subscription; no further notification is delivered.</summary>
        public void Dispose()
        {
            lock (_gate)
            {
                if (_finished)
                {
                    return;
                }

                _finished = true;
                _cancellation.Cancel();
                _cancellation.Dispose();
            }
        }

        /// <summary>Starts delivering the sequence to the observer.</summary>
        /// <param name="source">The sequence to enumerate.</param>
        internal void Start(IAsyncEnumerable<T> source) => _ = PumpAsync(source, _cancellation.Token);

        /// <summary>Delivers the sequence, then disposes its enumerator and releases the subscription.</summary>
        /// <param name="source">The sequence to enumerate.</param>
        /// <param name="cancellationToken">A token that is cancelled when the subscription is disposed.</param>
        /// <returns>A task that completes when the sequence has been enumerated and disposed.</returns>
        private async Task PumpAsync(IAsyncEnumerable<T> source, CancellationToken cancellationToken)
        {
            var enumerator = source.GetAsyncEnumerator(cancellationToken);
            try
            {
                await DeliverAsync(enumerator, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                await enumerator.DisposeAsync().ConfigureAwait(false);
                Dispose();
            }
        }

        /// <summary>Delivers each element, then completion or the failure of the sequence, unless the subscription was disposed.</summary>
        /// <param name="enumerator">The enumerator to drain.</param>
        /// <param name="cancellationToken">A token that is cancelled when the subscription is disposed.</param>
        /// <returns>A task that completes when the sequence has ended or the subscription was disposed.</returns>
        private async Task DeliverAsync(IAsyncEnumerator<T> enumerator, CancellationToken cancellationToken)
        {
            var delivering = true;
            while (delivering)
            {
                delivering = await DeliverNextAsync(enumerator, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>Delivers the next element, or completion or the failure of the sequence, unless the subscription was disposed.</summary>
        /// <param name="enumerator">The enumerator to advance.</param>
        /// <param name="cancellationToken">A token that is cancelled when the subscription is disposed.</param>
        /// <returns><see langword="true"/> when an element was delivered and more may follow; otherwise <see langword="false"/>.</returns>
        private async Task<bool> DeliverNextAsync(IAsyncEnumerator<T> enumerator, CancellationToken cancellationToken)
        {
            bool hasNext;
            try
            {
                hasNext = await enumerator.MoveNextAsync().ConfigureAwait(false);
            }
            catch (Exception error)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    _observer.OnError(error);
                }

                return false;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            if (!hasNext)
            {
                _observer.OnCompleted();
                return false;
            }

            _observer.OnNext(enumerator.Current);
            return true;
        }
    }
}
