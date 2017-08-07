using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Refit
{
    partial class RequestBuilderImplementation
    {
        sealed class TaskToObservable<T> : IObservable<T>
        {
            Func<CancellationToken, Task<T>> taskFactory;

            public TaskToObservable(Func<CancellationToken, Task<T>> taskFactory) 
            {
                this.taskFactory = taskFactory;
            }

            public IDisposable Subscribe(IObserver<T> observer)
            {
                var cts = new CancellationTokenSource();
                taskFactory(cts.Token).ContinueWith(t => 
                {
                    if (cts.IsCancellationRequested) return;

                    ToObservableDone(t, observer);
                }, TaskScheduler.Default);

                return new AnonymousDisposable(cts.Cancel);
            }

            static void ToObservableDone<TResult>(Task<TResult> task, IObserver<TResult> subject)
            {
                switch (task.Status)
                {
                    case TaskStatus.RanToCompletion:
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
#pragma warning disable VSTHRD102 // Implement internal logic asynchronously
                        subject.OnNext(task.Result);
#pragma warning restore VSTHRD102 // Implement internal logic asynchronously
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
                        subject.OnCompleted();
                        break;
                    case TaskStatus.Faulted:
                        subject.OnError(task.Exception.InnerException);
                        break;
                    case TaskStatus.Canceled:
                        subject.OnError(new TaskCanceledException(task));
                        break;
                }
            }
        }
    }
}
