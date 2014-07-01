namespace Refit.Tests.support
{
    using System;
    using System.Collections.Generic;
    using System.Reactive;

    using Microsoft.Reactive.Testing;

    public class TestableObserver<T> : ITestableObserver<T>
    {
        private readonly Action _afterAction;

        public bool OnNextWasCalled { get; set; }

        public bool OnErrorWasCalled { get; set; }

        public bool OnCompleteWasCalled { get; set; }

        public TestableObserver(Action afterAction)
        {
            _afterAction = afterAction;
            OnNextWasCalled = false;
            OnCompleteWasCalled = false;
            OnErrorWasCalled = false;
        }

        public void OnNext(T value)
        {
            OnNextWasCalled = true;
            _afterAction();
        }

        public void OnError(Exception error)
        {
            OnErrorWasCalled = true;
            _afterAction();
        }

        public void OnCompleted()
        {
            OnCompleteWasCalled = true;
            _afterAction();
        }

        public IList<Recorded<Notification<T>>> Messages { get; private set; }
    }
}