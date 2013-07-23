using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;
using Castle.DynamicProxy;
using System.Threading.Tasks;
using System.Threading;

namespace Refit
{
    class CastleRestService : IRestService
    {
        readonly ProxyGenerator proxyGen = new ProxyGenerator();
        public T For<T>(HttpClient client)
        {
            var rb = RequestBuilder.ForType<T>();
            return (T)proxyGen.CreateInterfaceProxyWithoutTarget(typeof(T), new RestServiceMethodMissing(rb, client));
        }
    }

    class RestServiceMethodMissing : IInterceptor
    {
        readonly HttpClient client;
        readonly Dictionary<string, Func<HttpClient, object[], object>> methodImpls;

        public RestServiceMethodMissing(IRequestBuilder requestBuilder, HttpClient client)
        {
            methodImpls = requestBuilder.InterfaceHttpMethods.ToDictionary(k => k, v => requestBuilder.BuildRestResultFuncForMethod(v));
            this.client = client;
        }

        public void Intercept(IInvocation invocation)
        {
            if (!methodImpls.ContainsKey(invocation.Method.Name)) {
                throw new NotImplementedException();
            }

            invocation.ReturnValue = methodImpls[invocation.Method.Name](client, invocation.Arguments);
            Console.WriteLine(invocation.ReturnValue);
        }
    }
}