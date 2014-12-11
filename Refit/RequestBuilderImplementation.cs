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

                string urlTarget = (basePath == "/" ? String.Empty : basePath) + restMethod.RelativePath;
                var queryParamsToAdd = new Dictionary<string, string>();

                for(int i=0; i < paramList.Length; i++) {
                    if (restMethod.ParameterMap.ContainsKey(i)) {
                        urlTarget = Regex.Replace(
                            urlTarget, 
                            "{" + restMethod.ParameterMap[i] + "}", 
                            settings.UrlParameterFormatter.Format(paramList[i], restMethod.ParameterInfoMap[i]), 
                            RegexOptions.IgnoreCase);
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
                    } else {
                        if (paramList[i] != null) {
                            queryParamsToAdd[restMethod.QueryParameterMap[i]] = settings.UrlParameterFormatter.Format(paramList[i], restMethod.ParameterInfoMap[i]);
                        }
                    }
                }

                // NB: The URI methods in .NET are dumb. Also, we do this 
                // UriBuilder business so that we preserve any hardcoded query 
                // parameters as well as add the parameterized ones.
                var uri = new UriBuilder(new Uri(new Uri("http://api"), urlTarget));
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

    public class RestMethodInfo
    {
        public string Name { get; set; }
        public Type Type { get; set; }
        public MethodInfo MethodInfo { get; set; }
        public HttpMethod HttpMethod { get; set; }
        public string RelativePath { get; set; }
        public Dictionary<int, string> ParameterMap { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public Dictionary<int, string> HeaderParameterMap { get; set; }
        public Tuple<BodySerializationMethod, int> BodyParameterInfo { get; set; }
        public Dictionary<int, string> QueryParameterMap { get; set; }
        public Dictionary<int, ParameterInfo> ParameterInfoMap { get; set; }
        public Type ReturnType { get; set; }
        public Type SerializedReturnType { get; set; }
        public RefitSettings RefitSettings { get; set; }

        static readonly Regex parameterRegex = new Regex(@"{(.*?)}");

        public RestMethodInfo(Type targetInterface, MethodInfo methodInfo, RefitSettings refitSettings = null)
        {
            RefitSettings = refitSettings ?? new RefitSettings();
            Type = targetInterface;
            Name = methodInfo.Name;
            MethodInfo = methodInfo;

            var hma = methodInfo.GetCustomAttributes(true)
                .OfType<HttpMethodAttribute>()
                .First();

            HttpMethod = hma.Method;
            RelativePath = hma.Path;

            verifyUrlPathIsSane(RelativePath);
            determineReturnTypeInfo(methodInfo);

            var parameterList = methodInfo.GetParameters().ToList();
            ParameterInfoMap = parameterList
                .Select((parameter, index) => new { index, parameter })
                .ToDictionary(x => x.index, x => x.parameter);
            ParameterMap = buildParameterMap(RelativePath, parameterList);
            BodyParameterInfo = findBodyParameter(parameterList);

            Headers = parseHeaders(methodInfo);
            HeaderParameterMap = buildHeaderParameterMap(parameterList);

            QueryParameterMap = new Dictionary<int, string>();
            for (int i=0; i < parameterList.Count; i++) {
                if (ParameterMap.ContainsKey(i) || HeaderParameterMap.ContainsKey(i) || (BodyParameterInfo != null && BodyParameterInfo.Item2 == i)) {
                    continue;
                }

                QueryParameterMap[i] = getUrlNameForParameter(parameterList[i]);
            }
        }

        void verifyUrlPathIsSane(string relativePath) 
        {
            if (relativePath == "")
                return;

            if (!relativePath.StartsWith("/")) {
                goto bogusPath;
            }

            var parts = relativePath.Split('/');
            if (parts.Length == 0) {
                goto bogusPath;
            }

            return;

        bogusPath:
            throw new ArgumentException("URL path must be of the form '/foo/bar/baz'");
        }

        Dictionary<int, string> buildParameterMap(string relativePath, List<ParameterInfo> parameterInfo)
        {
            var ret = new Dictionary<int, string>();

            var parameterizedParts = relativePath.Split('/', '?')
                .SelectMany(x => parameterRegex.Matches(x).Cast<Match>())
                .ToList();

            if (parameterizedParts.Count == 0) {
                return ret;
            }

            var paramValidationDict = parameterInfo.ToDictionary(k => getUrlNameForParameter(k).ToLowerInvariant(), v => v);

            foreach (var match in parameterizedParts) {
                var name = match.Groups[1].Value.ToLowerInvariant();
                if (!paramValidationDict.ContainsKey(name)) {
                    throw new ArgumentException(String.Format("URL has parameter {0}, but no method parameter matches", name));
                }

                ret.Add(parameterInfo.IndexOf(paramValidationDict[name]), name);
            }

            return ret;
        }

        string getUrlNameForParameter(ParameterInfo paramInfo)
        {
            var aliasAttr = paramInfo.GetCustomAttributes(true)
                .OfType<AliasAsAttribute>()
                .FirstOrDefault();
            return aliasAttr != null ? aliasAttr.Name : paramInfo.Name;
        }

        Tuple<BodySerializationMethod, int> findBodyParameter(List<ParameterInfo> parameterList)
        {
            var bodyParams = parameterList
                .Select(x => new { Parameter = x, BodyAttribute = x.GetCustomAttributes(true).OfType<BodyAttribute>().FirstOrDefault() })
                .Where(x => x.BodyAttribute != null)
                .ToList();

            if (bodyParams.Count > 1) {
                throw new ArgumentException("Only one parameter can be a Body parameter");
            }

            if (bodyParams.Count == 0) {
                return null;
            }

            var ret = bodyParams[0];
            return Tuple.Create(ret.BodyAttribute.SerializationMethod, parameterList.IndexOf(ret.Parameter));
        }

        Dictionary<string, string> parseHeaders(MethodInfo methodInfo) 
        {
            var ret = new Dictionary<string, string>();

            var declaringTypeAttributes = methodInfo.DeclaringType != null
                ? methodInfo.DeclaringType.GetCustomAttributes(true)
                : new Attribute[0];

            // Headers set on the declaring type have to come first, 
            // so headers set on the method can replace them. Switching
            // the order here will break stuff.
            var headers = declaringTypeAttributes.Concat(methodInfo.GetCustomAttributes(true))
                .OfType<HeadersAttribute>()
                .SelectMany(ha => ha.Headers);

            foreach (var header in headers) {
                if (string.IsNullOrWhiteSpace(header)) continue;

            // NB: Silverlight doesn't have an overload for String.Split()
            // with a count parameter, but header values can contain
            // ':' so we have to re-join all but the first part to get the
            // value.
                var parts = header.Split(':');
                ret[parts[0].Trim()] = parts.Length > 1 ? 
                    String.Join(":", parts.Skip(1)).Trim() : null;
            }

            return ret;
        }

        Dictionary<int, string> buildHeaderParameterMap(List<ParameterInfo> parameterList) 
        {
            var ret = new Dictionary<int, string>();

            for (int i = 0; i < parameterList.Count; i++) {
                var header = parameterList[i].GetCustomAttributes(true)
                    .OfType<HeaderAttribute>()
                    .Select(ha => ha.Header)
                    .FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(header)) {
                    ret[i] = header.Trim();
                }
            }

            return ret;
        }

        void determineReturnTypeInfo(MethodInfo methodInfo)
        {
            if (methodInfo.ReturnType.IsGenericType() == false) {
                if (methodInfo.ReturnType != typeof (Task)) {
                    goto bogusMethod;
                }

                ReturnType = methodInfo.ReturnType;
                SerializedReturnType = typeof(void);
                return;
            }

            var genericType = methodInfo.ReturnType.GetGenericTypeDefinition();
            if (genericType != typeof(Task<>) && genericType != typeof(IObservable<>)) {
                goto bogusMethod;
            }

            ReturnType = methodInfo.ReturnType;
            SerializedReturnType = methodInfo.ReturnType.GetGenericArguments()[0];
            return;

        bogusMethod:
            throw new ArgumentException("All REST Methods must return either Task<T> or IObservable<T>");
        }
    }

    public class ApiException : Exception
    {
        public HttpStatusCode StatusCode { get; private set; }
        public string ReasonPhrase { get; private set; }
        public HttpResponseHeaders Headers { get; private set; }

        public HttpContentHeaders ContentHeaders { get; private set; }

        public string Content { get; private set; }

        public bool HasContent {
            get { return !String.IsNullOrWhiteSpace(Content); }
        }
        public RefitSettings RefitSettings{get;set;}

        ApiException(HttpStatusCode statusCode, string reasonPhrase, HttpResponseHeaders headers, RefitSettings refitSettings = null) : 
            base(createMessage(statusCode, reasonPhrase)) 
        {
            StatusCode = statusCode;
            ReasonPhrase = reasonPhrase;
            Headers = headers;
            RefitSettings = refitSettings;
        }

        public T GetContentAs<T>()
        {
            return HasContent ? 
                JsonConvert.DeserializeObject<T>(Content, RefitSettings.JsonSerializerSettings) : 
                default(T);
        }

        public static async Task<ApiException> Create(HttpResponseMessage response, RefitSettings refitSettings = null) 
        {
            var exception = new ApiException(response.StatusCode, response.ReasonPhrase, response.Headers, refitSettings);

            if (response.Content == null) return exception;
            
            try {
                exception.ContentHeaders = response.Content.Headers;
                exception.Content = await response.Content.ReadAsStringAsync();
                response.Content.Dispose();
            } catch {
                // NB: We're already handling an exception at this point, 
                // so we want to make sure we don't throw another one 
                // that hides the real error.
            }
            
            return exception;
        }

        static string createMessage(HttpStatusCode statusCode, string reasonPhrase)
        {
            return String.Format("Response status code does not indicate success: {0} ({1}).", (int)statusCode, reasonPhrase);
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
