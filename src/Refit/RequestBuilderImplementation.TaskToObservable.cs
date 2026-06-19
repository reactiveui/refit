// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit;

/// <summary>Adapts cancellable task factories into observable sequences for <see cref="RequestBuilderImplementation"/>.</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Minor Code Smell",
    "SST1432:Mark type as static",
    Justification = "False positive: this is one part of a partial class whose other parts declare instance members; the type cannot be static.")]
internal partial class RequestBuilderImplementation
{
    /// <summary>Adapts a cancellable task factory into an observable sequence.</summary>
    /// <typeparam name="T">The result type produced by the task.</typeparam>
    private sealed class TaskToObservable<T> : IObservable<T?>
    {
        /// <summary>The factory that produces the task to observe.</summary>
        private readonly Func<CancellationToken, Task<T?>> _taskFactory;

        /// <summary>Initializes a new instance of the <see cref="TaskToObservable{T}"/> class.</summary>
        /// <param name="taskFactory">The factory that produces the task to observe.</param>
        public TaskToObservable(Func<CancellationToken, Task<T?>> taskFactory) => this._taskFactory = taskFactory;

        /// <inheritdoc/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD110", Justification = "Subscription is fire-and-forget; the continuation task is intentionally not awaited.")]
        public IDisposable Subscribe(IObserver<T?> observer)
        {
            var cts = new CancellationTokenSource();
            _taskFactory(cts.Token)
                .ContinueWith(
                    t =>
                    {
                        try
                        {
                            if (cts.IsCancellationRequested)
                            {
                                return;
                            }

                            ToObservableDone(t, observer);
                        }
                        finally
                        {
                            cts.Dispose();
                        }
                    },
                    TaskScheduler.Default);

            return new AnonymousDisposable(() =>
            {
                try
                {
                    cts.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // The token source was already disposed by the completed continuation; nothing to cancel.
                }
            });
        }

        /// <summary>Forwards the completed task's outcome to the observer.</summary>
        /// <typeparam name="TResult">The result type of the task.</typeparam>
        /// <param name="task">The completed task.</param>
        /// <param name="subject">The observer to notify.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD002", Justification = "Task is already completed here, so the result read never blocks.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Sonar", "S4462", Justification = "Task is already completed here, so the result read never blocks.")]
        private static void ToObservableDone<TResult>(Task<TResult?> task, IObserver<TResult?> subject)
        {
            switch (task.Status)
            {
                case TaskStatus.RanToCompletion:
                {
                    subject.OnNext(task.Result);
                    subject.OnCompleted();
                    break;
                }

                case TaskStatus.Faulted:
                {
                    subject.OnError(task.Exception!.InnerException!);
                    break;
                }

                case TaskStatus.Canceled:
                {
                    subject.OnError(new TaskCanceledException(task));
                    break;
                }
            }
        }
    }
}
