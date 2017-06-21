using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Refit
{
    partial class RequestBuilderImplementation
    {
        class TaskToObservable<T> : IObservable<T>
        {
            Func<CancellationToken, Task<T>> taskFactory;

            public TaskToObservable(Func<CancellationToken, Task<T>> taskFactory) 
            {
                this.taskFactory = taskFactory;
            }

            public IDisposable Subscribe(IObserver<T> observer)
            {
                var cts = new CancellationTokenSource();
                taskFactory(cts.Token).ContinueWith(t => {
                    if (cts.IsCancellationRequested) return;

                    if (t.Exception != null) {
                        observer.OnError(t.Exception.InnerExceptions.First());
                        return;
                    }

                    try {
                        observer.OnNext(t.Result);
                    } catch (Exception ex) {
                        observer.OnError(ex);
                    }
                        
                    observer.OnCompleted();
                }, TaskScheduler.Default);

                return new AnonymousDisposable(cts.Cancel);
            }
        }
    }
}
