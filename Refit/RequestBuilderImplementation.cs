using System;
using System.Net;
using System.Collections;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Text;
using Newtonsoft.Json;
using System.IO;
using HttpUtility = System.Web.HttpUtility;
using System.Threading;

namespace Refit
{
    public class RequestBuilderFactory : IRequestBuilderFactory
    {
        public IRequestBuilder Create(Type interfaceType, RefitSettings settings = null)
        {
            return new RequestBuilderImplementation(interfaceType, settings);
        }
    }

    public class RequestBuilderImplementation : IRequestBuilder
    {
        readonly Type targetType;
        readonly Dictionary<string, RestMethodInfo> interfaceHttpMethods;
        readonly RefitSettings settings;

        public RequestBuilderImplementation(Type targetInterface, RefitSettings refitSettings = null)
        {
            settings = refitSettings ?? new RefitSettings();
            if (targetInterface == null || !targetInterface.IsInterface()) {
                throw new ArgumentException("targetInterface must be an Interface");
            }

            targetType = targetInterface;
            interfaceHttpMethods = targetInterface.GetMethods()
                .SelectMany(x => {
                    var attrs = x.GetCustomAttributes(true);
                    var hasHttpMethod = attrs.OfType<HttpMethodAttribute>().Any();
                    if (!hasHttpMethod) return Enumerable.Empty<RestMethodInfo>();

                    return EnumerableEx.Return(new RestMethodInfo(targetInterface, x, settings));
                })
                .ToDictionary(k => k.Name, v => v);
        }

        public IEnumerable<string> InterfaceHttpMethods {
            get { return interfaceHttpMethods.Keys; }
        }

        public Func<object[], HttpRequestMessage> BuildRequestFactoryForMethod(string methodName, string basePath = "")
        {
            if (!interfaceHttpMethods.ContainsKey(methodName)) {
                throw new ArgumentException("Method must be defined and have an HTTP Method attribute");
            }
            var restMethod = interfaceHttpMethods[methodName];

            return paramList => {
                var ret = new HttpRequestMessage() {
                    Method = restMethod.HttpMethod,
                };

                foreach (var header in restMethod.Headers) {
                    setHeader(ret, header.Key, header.Value);
                }   

                for(int i=0; i < paramList.Length; i++) {

                    if (restMethod.ParameterMap.ContainsKey(i)) {
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
                            switch (restMethod.BodyParameterInfo.Item1) {
                            case BodySerializationMethod.UrlEncoded:
                                ret.Content = new FormUrlEncodedContent(new FormValueDictionary(paramList[i]));
                                break;
                            case BodySerializationMethod.Json:
                                ret.Content = new StringContent(JsonConvert.SerializeObject(paramList[i], settings.JsonSerializerSettings), Encoding.UTF8, "application/json");
                                break;
                            }
                        }
                        continue;
                    }

                    if (restMethod.HeaderParameterMap.ContainsKey(i)) {
                        setHeader(ret, restMethod.HeaderParameterMap[i], paramList[i]);
                    } 
                }

                ret.RequestUri = settings.UrlTemplateHandler.GetRequestUri(settings.UrlParameterFormatter, restMethod,paramList,basePath);
                return ret;
            };
        }

        void setHeader(HttpRequestMessage request, string name, object value) 
        {
            // Clear any existing version of this header we may have set, because
            // we want to allow removal/redefinition of headers. 

            // NB: We have to enumerate the header names to check existence because 
            // Contains throws if it's the wrong header type for the collection.
            if (request.Headers.Any(x => x.Key == name)) {
                request.Headers.Remove(name);
            }
            if (request.Content != null && request.Content.Headers.Any(x => x.Key == name)) {
                request.Content.Headers.Remove(name);
            }

            if (value == null) return;

            var s = value.ToString();
            request.Headers.TryAddWithoutValidation(name, s);

            if (request.Content != null) {
                request.Content.Headers.TryAddWithoutValidation(name, s);
            }
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
            return async (client, paramList) => {
                var factory = BuildRequestFactoryForMethod(restMethod.Name, client.BaseAddress.AbsolutePath);
                var rq = factory(paramList);
                var resp = await client.SendAsync(rq);

                if (!resp.IsSuccessStatusCode) {
                    throw await ApiException.Create(resp, settings);
                }
            };
        }

        Func<HttpClient, object[], Task<T>> buildTaskFuncForMethod<T>(RestMethodInfo restMethod)
        {
            var ret = buildCancellableTaskFuncForMethod<T>(restMethod);
            return (client, paramList) => ret(client, CancellationToken.None, paramList);
        }

        Func<HttpClient, CancellationToken, object[], Task<T>> buildCancellableTaskFuncForMethod<T>(RestMethodInfo restMethod)
        {
            return async (client, ct, paramList) => {
                var factory = BuildRequestFactoryForMethod(restMethod.Name, client.BaseAddress.AbsolutePath);
                var rq = factory(paramList);

                var resp = await client.SendAsync(rq, HttpCompletionOption.ResponseHeadersRead, ct);

                if (restMethod.SerializedReturnType == typeof(HttpResponseMessage)) {
                    // NB: This double-casting manual-boxing hate crime is the only way to make 
                    // this work without a 'class' generic constraint. It could blow up at runtime 
                    // and would be A Bad Idea if we hadn't already vetted the return type.
                    return (T)(object)resp; 
                }

                if (!resp.IsSuccessStatusCode) {
                    throw await ApiException.Create(resp, restMethod.RefitSettings);
                }

                var ms = new MemoryStream();
                var fromStream = await resp.Content.ReadAsStreamAsync();
                await fromStream.CopyToAsync(ms, 4096, ct);

                var bytes = ms.ToArray();
                var content = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
                if (restMethod.SerializedReturnType == typeof(string)) {
                    return (T)(object)content; 
                }

                return JsonConvert.DeserializeObject<T>(content, settings.JsonSerializerSettings);
            };
        }

        Func<HttpClient, object[], IObservable<T>> buildRxFuncForMethod<T>(RestMethodInfo restMethod)
        {
            var taskFunc = buildCancellableTaskFuncForMethod<T>(restMethod);

            return (client, paramList) => 
                new TaskToObservable<T>(ct => taskFunc(client, ct, paramList));
        }

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
                });

                return new AnonymousDisposable(cts.Cancel);
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

    class FormValueDictionary : Dictionary<string, string>
    {
        static readonly Dictionary<Type, PropertyInfo[]> propertyCache
            = new Dictionary<Type, PropertyInfo[]>();

        public FormValueDictionary(object source) 
        {
            if (source == null) return;
            var dictionary = source as IDictionary;

            if (dictionary != null) {
                foreach (var key in dictionary.Keys) {
                    Add(key.ToString(), string.Format("{0}", dictionary[key]));
                }
                
                return;
            }

            var type = source.GetType();

            lock (propertyCache) {
                if (!propertyCache.ContainsKey(type)) {
                    propertyCache[type] = getProperties(type);
                }

                foreach (var property in propertyCache[type]) {
                    Add(getFieldNameForProperty(property), string.Format("{0}", property.GetValue(source, null)));
                }
            }
        }

        PropertyInfo[] getProperties(Type type) 
        {
            return type.GetProperties()
                .Where(p => p.CanRead)
                .ToArray();
        }

        string getFieldNameForProperty(PropertyInfo propertyInfo)
        {
            var aliasAttr = propertyInfo.GetCustomAttributes(true)
                .OfType<AliasAsAttribute>()
                .FirstOrDefault();
            return aliasAttr != null ? aliasAttr.Name : propertyInfo.Name;
        }
    }
}
