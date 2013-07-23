using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;
using Castle.DynamicProxy;
using System.Threading.Tasks;
using System.Threading;

namespace Refit
{
    public static class RestService
    {
        static readonly ProxyGenerator proxyGen = new ProxyGenerator();
        public static T For<T>(HttpClient client)
        {
            var rb = new RequestBuilder(typeof(T));
            return (T)proxyGen.CreateInterfaceProxyWithoutTarget(typeof(T), new RestServiceMethodMissing(rb, client));
        }

        public static T For<T>(string hostUrl)
        {
            var client = new HttpClient() { BaseAddress = new Uri(hostUrl) };
            return RestService.For<T>(client);
        }
    }

    class RestServiceMethodMissing : IInterceptor
    {
        readonly HttpClient client;
        readonly Dictionary<string, Func<HttpClient, object[], object>> methodImpls;

        public RestServiceMethodMissing(RequestBuilder requestBuilder, HttpClient client)
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