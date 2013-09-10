using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Text;
using Newtonsoft.Json;
using System.IO;
using System.Web;
using System.Threading;

namespace Refit
{
    public class RequestBuilderFactory : IRequestBuilderFactory
    {
        public IRequestBuilder Create(Type interfaceType)
        {
            return new RequestBuilderImplementation(interfaceType);
        }
    }

    public class RequestBuilderImplementation : IRequestBuilder
    {
        readonly Type targetType;
        readonly Dictionary<string, RestMethodInfo> interfaceHttpMethods;

        public RequestBuilderImplementation(Type targetInterface)
        {
            if (targetInterface == null || !targetInterface.IsInterface) {
                throw new ArgumentException("targetInterface must be an Interface");
            }

            targetType = targetInterface;
            interfaceHttpMethods = RestService.RestMethodResolver.GetInterfaceRestMethodInfo(targetInterface);
        }

        public IEnumerable<string> InterfaceHttpMethods {
            get { return interfaceHttpMethods.Keys; }
        }

        public Func<object[], HttpRequestMessage> BuildRequestFactoryForMethod(string methodName)
        {
            if (!interfaceHttpMethods.ContainsKey(methodName)) {
                throw new ArgumentException("Method must be defined and have an HTTP Method attribute");
            }
            var restMethod = interfaceHttpMethods[methodName];

            return paramList => {
                var ret = new HttpRequestMessage() {
                    Method = restMethod.HttpMethod,
                };

                var urlTarget = new StringBuilder(restMethod.RelativePath);
                var queryParamsToAdd = new Dictionary<string, string>();

                for(int i=0; i < paramList.Length; i++) {
                    if (restMethod.ParameterMap.ContainsKey(i)) {
                        urlTarget.Replace("{" + restMethod.ParameterMap[i] + "}", paramList[i].ToString());
                        continue;
                    }

                    if (restMethod.BodyParameterInfo != null && restMethod.BodyParameterInfo.Item2 == i) {
                        var streamParam = paramList[i] as Stream;
                        var stringParam = paramList[i] as string;

                        if (streamParam != null) {
                            ret.Content = new StreamContent(streamParam);
                        } else if (stringParam != null) {
                            ret.Content = new StringContent(stringParam);
                        } else {
                            ret.Content = new StringContent(JsonConvert.SerializeObject(paramList[i]), Encoding.UTF8, "application/json");
                        }

                        continue;
                    }

                    if (paramList[i] != null) {
                        queryParamsToAdd[restMethod.QueryParameterMap[i]] = paramList[i].ToString();
                    }
                }

                // NB: The URI methods in .NET are dumb. Also, we do this 
                // UriBuilder business so that we preserve any hardcoded query 
                // parameters as well as add the parameterized ones.
                var uri = new UriBuilder(new Uri(new Uri("http://api"), urlTarget.ToString()));
                var query = HttpUtility.ParseQueryString(uri.Query ?? "");
                foreach(var kvp in queryParamsToAdd) {
                    query.Add(kvp.Key, kvp.Value);
                }

                if (query.HasKeys()) {
                    var pairs = query.Keys.Cast<string>().Select(x => HttpUtility.UrlEncode(x) + "=" + HttpUtility.UrlEncode(query[x]));
                    uri.Query = String.Join("&", pairs);
                } else {
                    uri.Query = null;
                }

                ret.RequestUri = new Uri(uri.Uri.GetComponents(UriComponents.PathAndQuery, UriFormat.UriEscaped), UriKind.Relative);
                return ret;
            };
        }

        public Func<HttpClient, object[], object> BuildRestResultFuncForMethod(string methodName)
        {
            if (!interfaceHttpMethods.ContainsKey(methodName)) {
                throw new ArgumentException("Method must be defined and have an HTTP Method attribute");
            }

            var restMethod = interfaceHttpMethods[methodName];

            if (restMethod.ReturnType == typeof(Task)) {
                return buildVoidTaskFuncForMethod(restMethod);
            } else if (restMethod.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)) {
                // NB: This jacked up reflection code is here because it's
                // difficult to upcast Task<object> to an arbitrary T, especially
                // if you need to AOT everything, so we need to reflectively 
                // invoke buildTaskFuncForMethod.
                var taskFuncMi = GetType().GetMethod("buildTaskFuncForMethod", BindingFlags.NonPublic | BindingFlags.Instance);
                var taskFunc = (MulticastDelegate)taskFuncMi.MakeGenericMethod(restMethod.SerializedReturnType)
                    .Invoke(this, new[] { restMethod });

                return (client, args) => {
                    return taskFunc.DynamicInvoke(new object[] { client, args } );
                };
            } else {
                // Same deal
                var rxFuncMi = GetType().GetMethod("buildRxFuncForMethod", BindingFlags.NonPublic | BindingFlags.Instance);
                var rxFunc = (MulticastDelegate)rxFuncMi.MakeGenericMethod(restMethod.SerializedReturnType)
                    .Invoke(this, new[] { restMethod });

                return (client, args) => {
                    return rxFunc.DynamicInvoke(new object[] { client, args });
                };
            }
        }

        Func<HttpClient, object[], Task> buildVoidTaskFuncForMethod(RestMethodInfo restMethod)
        {
            var factory = BuildRequestFactoryForMethod(restMethod.Name);
                        
            return async (client, paramList) => {
                var rq = factory(paramList);
                var resp = await client.SendAsync(rq);

                resp.EnsureSuccessStatusCode();
            };
        }

        Func<HttpClient, object[], Task<T>> buildTaskFuncForMethod<T>(RestMethodInfo restMethod)
            where T : class
        {
            var factory = BuildRequestFactoryForMethod(restMethod.Name);

            return async (client, paramList) => {
                var rq = factory(paramList);
                var resp = await client.SendAsync(rq);
                if (restMethod.SerializedReturnType == null) {
                    return resp as T;
                }

                resp.EnsureSuccessStatusCode();

                var content = await resp.Content.ReadAsStringAsync();
                if (restMethod.SerializedReturnType == typeof(string)) {
                    return content as T;
                }

                return JsonConvert.DeserializeObject<T>(content);
            };
        }

        Func<HttpClient, object[], IObservable<T>> buildRxFuncForMethod<T>(RestMethodInfo restMethod)
            where T : class
        {
            var taskFunc = buildTaskFuncForMethod<T>(restMethod);

            return (client, paramList) => {
                var ret = new FakeAsyncSubject<T>();

                taskFunc(client, paramList).ContinueWith(t => {
                    if (t.Exception != null) {
                        ret.OnError(t.Exception);
                    } else {
                        ret.OnNext(t.Result);
                        ret.OnCompleted();
                    }
                });

                return ret;
            };
        }

        class CompletionResult 
        {
            public bool IsCompleted { get; set; }
            public Exception Error { get; set; }
        }

        class FakeAsyncSubject<T> : IObservable<T>, IObserver<T>
        {
            bool resultSet;
            T result;
            CompletionResult completion;
            List<IObserver<T>> subscriberList = new List<IObserver<T>>();

            public void OnNext(T value)
            {
                if (completion == null) return;

                result = value;
                resultSet = true;

                var currentList = default(IObserver<T>[]);
                lock (subscriberList) { currentList = subscriberList.ToArray(); }
                foreach (var v in currentList) v.OnNext(value);
            }

            public void OnError(Exception error)
            {
                var final = Interlocked.CompareExchange(ref completion, new CompletionResult() { IsCompleted = false, Error = error }, null);
                if (final.IsCompleted) return;
                                
                var currentList = default(IObserver<T>[]);
                lock (subscriberList) { currentList = subscriberList.ToArray(); }
                foreach (var v in currentList) v.OnError(error);

                final.IsCompleted = true;
            }

            public void OnCompleted()
            {
                var final = Interlocked.CompareExchange(ref completion, new CompletionResult() { IsCompleted = false, Error = null }, null);
                if (final.IsCompleted) return;
                                
                var currentList = default(IObserver<T>[]);
                lock (subscriberList) { currentList = subscriberList.ToArray(); }
                foreach (var v in currentList) v.OnCompleted();

                final.IsCompleted = true;
            }

            public IDisposable Subscribe(IObserver<T> observer)
            {
                if (completion != null) {
                    if (completion.Error != null) {
                        observer.OnError(completion.Error);
                        return new AnonymousDisposable(() => {});
                    }

                    if (resultSet) observer.OnNext(result);
                    observer.OnCompleted();
                        
                    return new AnonymousDisposable(() => {});
                }

                lock (subscriberList) { 
                    subscriberList.Add(observer);
                }

                return new AnonymousDisposable(() => {
                    lock (subscriberList) { subscriberList.Remove(observer); }
                });
            }
        }
    }

    sealed class AnonymousDisposable : IDisposable
    {
        readonly Action block;

        public AnonymousDisposable(Action block)
        {
            this.block = block;
        }

        public void Dispose()
        {
            block();
        }
    }
}
