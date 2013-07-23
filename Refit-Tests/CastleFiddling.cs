using System;
using Castle.DynamicProxy;
using NUnit.Framework;

namespace Refit
{
    public interface IFoo
    {
        void Bar(int baz);
        void Bamf(string burf);
    }

    public class TestIntercepter : IInterceptor
    {
        public void Intercept(IInvocation invocation)
        {
            Console.WriteLine(invocation.Method.Name);
        }
    }

    [TestFixture]
    public class CastleSomeShit
    {
        [Test]
        public void AttemptToCastleAnInterface()
        {
            var pg = new ProxyGenerator();
            var interceptor = new TestIntercepter();
            var target = pg.CreateInterfaceProxyWithoutTarget<IFoo>(interceptor);

            target.Bamf("foo");
        }
    }
}