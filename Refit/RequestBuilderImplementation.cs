using System;
using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace Refit
{
    partial class RequestBuilderImplementation : IRequestBuilder
    {
        static readonly ISet<HttpMethod> bodylessMethods = new HashSet<HttpMethod>
        {
            HttpMethod.Get,
            HttpMethod.Head
        };
        readonly Dictionary<string, List<RestMethodInfo>> interfaceHttpMethods;
        readonly ConcurrentDictionary<CloseGenericMethodKey, RestMethodInfo> interfaceGenericHttpMethods;
        readonly JsonSerializer serializer;
        readonly RefitSettings settings;
        readonly Type targetType;

        public RequestBuilderImplementation(Type targetInterface, RefitSettings refitSettings = null)
        {
            settings = refitSettings ?? new RefitSettings();
            serializer = JsonSerializer.Create(settings.JsonSerializerSettings);
            interfaceGenericHttpMethods = new ConcurrentDictionary<CloseGenericMethodKey, RestMethodInfo>();

            if (targetInterface == null || !targetInterface.GetTypeInfo().IsInterface)
            {
                throw new ArgumentException("targetInterface must be an Interface");
            }

            targetType = targetInterface;
            var dict = new Dictionary<string, List<RestMethodInfo>>();

            foreach (var methodInfo in targetInterface.GetMethods()) 
            {
                var attrs = methodInfo.GetCustomAttributes(true);
                var hasHttpMethod = attrs.OfType<HttpMethodAttribute>().Any();
                if (hasHttpMethod) 
                {
                    if (!dict.ContainsKey(methodInfo.Name)) 
                    {
                        dict.Add(methodInfo.Name, new List<RestMethodInfo>());
                    }
                    var restinfo = new RestMethodInfo(targetInterface, methodInfo, settings);
                    dict[methodInfo.Name].Add(restinfo);
                }
            }

            interfaceHttpMethods = dict;
        }

        RestMethodInfo FindMatchingRestMethodInfo(string key, Type[] parameterTypes, Type[] genericArgumentTypes)
        {
            if (interfaceHttpMethods.TryGetValue(key, out var httpMethods)) 
            {
                if (parameterTypes == null) 
                {
                    if (httpMethods.Count > 1) 
                    {
                        throw new ArgumentException($"MethodName exists more than once, '{nameof(parameterTypes)}' mut be defined");
                    }
                    return CloseGenericMethodIfNeeded(httpMethods[0], genericArgumentTypes);
                }

                var isGeneric = genericArgumentTypes?.Length > 0;

                var possibleMethodsList = httpMethods.Where(method => method.MethodInfo.GetParameters().Length == parameterTypes.Count());

                // If it's a generic method, add that filter
                if (isGeneric)
                    possibleMethodsList = possibleMethodsList.Where(method => method.MethodInfo.IsGenericMethod && method.MethodInfo.GetGenericArguments().Length == genericArgumentTypes.Length);
                else // exclude generic methods
                    possibleMethodsList = possibleMethodsList.Where(method => !method.MethodInfo.IsGenericMethod);

                var possibleMethods = possibleMethodsList.ToList();

                if (possibleMethods.Count == 1)
                    return CloseGenericMethodIfNeeded(possibleMethods[0], genericArgumentTypes);

                var parameterTypesArray = parameterTypes.ToArray();
                foreach (var method in possibleMethods) 
                {
                    var match = method.MethodInfo.GetParameters()
                                      .Select(p => p.ParameterType)
                                      .SequenceEqual(parameterTypesArray);
                    if (match)
                    {
                        return CloseGenericMethodIfNeeded(method, genericArgumentTypes);
                    }
                }

                throw new Exception("No suitable Method found...");
            }
            else 
            {
                throw new ArgumentException("Method must be defined and have an HTTP Method attribute");
            }

        }

        private RestMethodInfo CloseGenericMethodIfNeeded(RestMethodInfo restMethodInfo, Type[] genericArgumentTypes)
        {
            if (genericArgumentTypes != null)
            {
                return interfaceGenericHttpMethods.GetOrAdd(new CloseGenericMethodKey(restMethodInfo.MethodInfo, genericArgumentTypes),
                    _ => new RestMethodInfo(restMethodInfo.Type, restMethodInfo.MethodInfo.MakeGenericMethod(genericArgumentTypes), restMethodInfo.RefitSettings));
            }
            return restMethodInfo;
        }

        public Func<HttpClient, object[], object> BuildRestResultFuncForMethod(string methodName, Type[] parameterTypes = null, Type[] genericArgumentTypes = null)
        {
            if (!interfaceHttpMethods.ContainsKey(methodName))
            {
                throw new ArgumentException("Method must be defined and have an HTTP Method attribute");
            }

            var restMethod = FindMatchingRestMethodInfo(methodName, parameterTypes, genericArgumentTypes);
            if (restMethod.ReturnType == typeof(Task))
            {
                return BuildVoidTaskFuncForMethod(restMethod);
            }

            if (restMethod.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                // NB: This jacked up reflection code is here because it's
                // difficult to upcast Task<object> to an arbitrary T, especially
                // if you need to AOT everything, so we need to reflectively 
                // invoke buildTaskFuncForMethod.
                var taskFuncMi = GetType().GetMethod(nameof(BuildTaskFuncForMethod), BindingFlags.NonPublic | BindingFlags.Instance);
                var taskFunc = (MulticastDelegate)taskFuncMi.MakeGenericMethod(restMethod.SerializedReturnType)
                                                            .Invoke(this, new[] { restMethod });

                return (client, args) => taskFunc.DynamicInvoke(client, args);
            }

            // Same deal
            var rxFuncMi = GetType().GetMethod(nameof(BuildRxFuncForMethod), BindingFlags.NonPublic | BindingFlags.Instance);
            var rxFunc = (MulticastDelegate)rxFuncMi.MakeGenericMethod(restMethod.SerializedReturnType)
                                                    .Invoke(this, new[] { restMethod });

            return (client, args) => rxFunc.DynamicInvoke(client, args); 
        }

        void AddMultipartItem(MultipartFormDataContent multiPartContent, string fileName, string parameterName, object itemValue)
        {

            if (itemValue is MultipartItem multipartItem) 
            {
                var httpContent = multipartItem.ToContent();
                multiPartContent.Add(httpContent, parameterName, string.IsNullOrEmpty(multipartItem.FileName) ? fileName : multipartItem.FileName);
                return;
            }

            if (itemValue is Stream streamValue)
            {
                var streamContent = new StreamContent(streamValue);
                multiPartContent.Add(streamContent, parameterName, fileName);
                return;
            }

            if (itemValue is string stringValue) 
            {
                multiPartContent.Add(new StringContent(stringValue), parameterName);
                return;
            }

            if (itemValue is FileInfo fileInfoValue)
            {
                var fileContent = new StreamContent(fileInfoValue.OpenRead());
                multiPartContent.Add(fileContent, parameterName, fileInfoValue.Name);
                return;
            }

            if (itemValue is byte[] byteArrayValue) {
                var fileContent = new ByteArrayContent(byteArrayValue);
                multiPartContent.Add(fileContent, parameterName, fileName);
                return;
            }

            // Fallback to Json
            Exception e = null;
            try
            {
                var stringContent = new StringContent(JsonConvert.SerializeObject(itemValue, settings.JsonSerializerSettings), Encoding.UTF8, "application/json");
                multiPartContent.Add(stringContent, parameterName);
                return;
            }
            catch(Exception ex)
            {
                // Eat this since we're about to throw as a fallback anyway
                e = ex;
            }

            throw new ArgumentException($"Unexpected parameter type in a Multipart request. Parameter {fileName} is of type {itemValue.GetType().Name}, whereas allowed types are String, Stream, FileInfo, Byte array and anything that's JSON serializable", nameof(itemValue), e);
        }

        Func<HttpClient, CancellationToken, object[], Task<T>> BuildCancellableTaskFuncForMethod<T>(RestMethodInfo restMethod)
        {
            return async (client, ct, paramList) =>
            {
                if (client.BaseAddress == null)
                    throw new InvalidOperationException("BaseAddress must be set on the HttpClient instance");

                var factory = BuildRequestFactoryForMethod(restMethod, client.BaseAddress.AbsolutePath, restMethod.CancellationToken != null);
                var rq = factory(paramList);
                HttpResponseMessage resp = null;
                var disposeResponse = true;
                try
                {
                    resp = await client.SendAsync(rq, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
                    if (restMethod.SerializedReturnType == typeof(HttpResponseMessage))
                    {
                        disposeResponse = false; // caller has to dispose

                        // NB: This double-casting manual-boxing hate crime is the only way to make 
                        // this work without a 'class' generic constraint. It could blow up at runtime 
                        // and would be A Bad Idea if we hadn't already vetted the return type.
                        return (T)(object)resp;
                    }

                    var isApiResponse = restMethod.SerializedReturnType.GetTypeInfo().IsGenericType && 
                                        restMethod.SerializedReturnType.GetGenericTypeDefinition() == typeof(ApiResponse<>);
               
                    if (!resp.IsSuccessStatusCode && !isApiResponse)
                    {
                        disposeResponse = false;
                        throw await ApiException.Create(rq, restMethod.HttpMethod, resp, restMethod.RefitSettings).ConfigureAwait(false);
                    }

                    var serializedReturnType = (isApiResponse) ? restMethod.SerializedGenericArgument : restMethod.SerializedReturnType;                    

                    if (serializedReturnType == typeof(HttpContent))
                    {
                        disposeResponse = false; // caller has to clean up the content
                        if (isApiResponse)
                            return ApiResponse.Create<T>(resp, resp.Content);
                        return (T)(object)resp.Content;
                    }

                    if (serializedReturnType == typeof(Stream))
                    {
                        disposeResponse = false; // caller has to dispose
                        var stream = (object)await resp.Content.ReadAsStreamAsync().ConfigureAwait(false);
                        if (isApiResponse)
                            return ApiResponse.Create<T>(resp, stream);
                        return (T)stream;
                    }

                    using (var stream = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    using (var reader = new StreamReader(stream))
                    {
                        if (serializedReturnType == typeof(string))
                        {
                            var str = (object)await reader.ReadToEndAsync().ConfigureAwait(false);
                            if (isApiResponse)
                                return ApiResponse.Create<T>(resp, str);
                            return (T)str;
                        }

                        using (var jsonReader = new JsonTextReader(reader))
                        {
                            var json = serializer.Deserialize(jsonReader, serializedReturnType);
                            if (isApiResponse)
                                return ApiResponse.Create<T>(resp, json);
                            return (T)json;
                        }
                    }
                }
                finally
                {
                    // Ensure we clean up the request
                    // Especially important if it has open files/streams
                    rq.Dispose();
                    if (disposeResponse)
                        resp?.Dispose();
                }
            };
        }

        List<KeyValuePair<string, object>> BuildQueryMap(object @object, string delimiter = null)
        {
            if (@object is IDictionary idictionary)
            {
                return BuildQueryMap(idictionary, delimiter);
            }

            var kvps = new List<KeyValuePair<string, object>>();

            var bindingFlags = BindingFlags.Instance | BindingFlags.Public;
            var props = @object.GetType().GetProperties(bindingFlags);

            foreach (var propertyInfo in props)
            {
                var obj = propertyInfo.GetValue(@object);
                if (obj == null)
                    continue;

                var key = propertyInfo.Name;

                var aliasAttribute = propertyInfo.GetCustomAttribute<AliasAsAttribute>();
                if (aliasAttribute != null)
                    key = aliasAttribute.Name;

                if (DoNotConvertToQueryMap(obj))
                {
                    kvps.Add(new KeyValuePair<string, object>(key, obj));
                    continue;
                }

                switch (obj)
                {
                    case IDictionary idict:
                        foreach (var keyValuePair in BuildQueryMap(idict, delimiter))
                        {
                            kvps.Add(new KeyValuePair<string, object>($"{key}{delimiter}{keyValuePair.Key}", keyValuePair.Value));
                        }

                        break;

                    case IEnumerable ienu:
                        foreach (var o in ienu)
                        {
                            kvps.Add(new KeyValuePair<string, object>(key, o));
                        }

                        break;

                    default:
                        foreach (var keyValuePair in BuildQueryMap(obj, delimiter))
                        {
                            kvps.Add(new KeyValuePair<string, object>($"{key}{delimiter}{keyValuePair.Key}", keyValuePair.Value));
                        }

                        break;
                }
            }

            return kvps;
        }

        List<KeyValuePair<string, object>> BuildQueryMap(IDictionary dictionary, string delimiter = null)
        {
            var kvps = new List<KeyValuePair<string, object>>();

            var props = dictionary.Keys;
            foreach (string key in props)
            {
                var obj = dictionary[key];
                if (obj == null)
                    continue;

                if (DoNotConvertToQueryMap(obj))
                {
                    kvps.Add(new KeyValuePair<string, object>(key, obj));
                }
                else
                {
                    foreach (var keyValuePair in BuildQueryMap(obj, delimiter))
                    {
                        kvps.Add(new KeyValuePair<string, object>($"{key}{delimiter}{keyValuePair.Key}", keyValuePair.Value));
                    }
                }
            }

            return kvps;
        }

        Func<object[], HttpRequestMessage> BuildRequestFactoryForMethod(RestMethodInfo restMethod, string basePath, bool paramsContainsCancellationToken)
        {
           
            return paramList =>
            {
                // make sure we strip out any cancelation tokens
                if (paramsContainsCancellationToken)
                {
                    paramList = paramList.Where(o => o == null || o.GetType() != typeof(CancellationToken)).ToArray();
                }

                var ret = new HttpRequestMessage
                {
                    Method = restMethod.HttpMethod
                };

                // set up multipart content
                MultipartFormDataContent multiPartContent = null;
                if (restMethod.IsMultipart)
                {
                    multiPartContent = new MultipartFormDataContent("----MyGreatBoundary");
                    ret.Content = multiPartContent;
                }

                var urlTarget = (basePath == "/" ? string.Empty : basePath) + restMethod.RelativePath;
                var queryParamsToAdd = new List<KeyValuePair<string, string>>();
                var headersToAdd = new Dictionary<string, string>(restMethod.Headers);

                for (var i = 0; i < paramList.Length; i++)
                {
                    // if part of REST resource URL, substitute it in
                    if (restMethod.ParameterMap.ContainsKey(i))
                    {
                        urlTarget = Regex.Replace(
                            urlTarget,
                            "{" + restMethod.ParameterMap[i] + "}",
                            Uri.EscapeDataString(settings.UrlParameterFormatter
                                    .Format(paramList[i], restMethod.ParameterInfoMap[i])),
                            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                        continue;
                    }

                    // if marked as body, add to content
                    if (restMethod.BodyParameterInfo != null && restMethod.BodyParameterInfo.Item3 == i)
                    {
                        if (paramList[i] is HttpContent httpContentParam)
                        {
                            ret.Content = httpContentParam;
                        }
                        else if (paramList[i] is Stream streamParam)
                        {
                            ret.Content = new StreamContent(streamParam);
                        }
                        // Default sends raw strings
                        else if (restMethod.BodyParameterInfo.Item1 == BodySerializationMethod.Default &&
                                 paramList[i] is string stringParam)
                        {
                            ret.Content = new StringContent(stringParam);
                        }
                        else
                        {
                            switch (restMethod.BodyParameterInfo.Item1)
                            {
                                case BodySerializationMethod.UrlEncoded:
                                    ret.Content = paramList[i] is string str ? (HttpContent)new StringContent(Uri.EscapeDataString(str), Encoding.UTF8, "application/x-www-form-urlencoded") :  new FormUrlEncodedContent(new FormValueDictionary(paramList[i], settings));
                                    break;
                                case BodySerializationMethod.Default:
                                case BodySerializationMethod.Json:
                                    var param = paramList[i];
                                    switch (restMethod.BodyParameterInfo.Item2)
                                    {
                                        case false:
                                            ret.Content = new PushStreamContent((stream, _, __) =>
                                                                                {
                                                                                    using (var writer = new JsonTextWriter(new StreamWriter(stream)))
                                                                                    {
                                                                                        serializer.Serialize(writer, param);
                                                                                    }
                                                                                },
                                                                                "application/json");
                                            break;
                                        case true:
                                            ret.Content = new StringContent(
                                                JsonConvert.SerializeObject(paramList[i], settings.JsonSerializerSettings),
                                                Encoding.UTF8,
                                                "application/json");
                                            break;
                                    }

                                    break;
                            }
                        }

                        continue;
                    }

                    // if header, add to request headers
                    if (restMethod.HeaderParameterMap.ContainsKey(i))
                    {
                        headersToAdd[restMethod.HeaderParameterMap[i]] = paramList[i]?.ToString();
                        continue;
                    }

                    // ignore nulls
                    if (paramList[i] == null) continue;

                    // for anything that fell through to here, if this is not
                    // a multipart method, add the parameter to the query string
                    if (!restMethod.IsMultipart)
                    {
                        var attr = restMethod.ParameterInfoMap[i].GetCustomAttribute<QueryAttribute>() ?? new QueryAttribute();
                        if (DoNotConvertToQueryMap(paramList[i]))
                        {
                            queryParamsToAdd.Add(new KeyValuePair<string, string>(restMethod.QueryParameterMap[i], settings.UrlParameterFormatter.Format(paramList[i], restMethod.ParameterInfoMap[i])));
                        }
                        else
                        {
                            foreach (var kvp in BuildQueryMap(paramList[i], attr.Delimiter))
                            {
                                var path = !string.IsNullOrWhiteSpace(attr.Prefix) ? $"{attr.Prefix}{attr.Delimiter}{kvp.Key}" : kvp.Key;
                                queryParamsToAdd.Add(new KeyValuePair<string, string>(path, settings.UrlParameterFormatter.Format(kvp.Value, restMethod.ParameterInfoMap[i])));
                            }
                        }

                        continue;
                    }

                    // we are in a multipart method, add the part to the content
                    // the parameter name should be either the attachment name or the parameter name (as fallback)
                    string itemName;
                    string parameterName;

                    if (!restMethod.AttachmentNameMap.TryGetValue(i, out var attachment))
                    {
                        itemName = restMethod.QueryParameterMap[i];
                        parameterName = itemName;
                    }
                    else
                    {
                        itemName = attachment.Item1;
                        parameterName = attachment.Item2;
                    }

                    // Check to see if it's an IEnumerable
                    var itemValue = paramList[i];
                    var enumerable = itemValue as IEnumerable<object>;
                    var typeIsCollection = false;

                    if (enumerable != null)
                    {
                        typeIsCollection = true;
                    }

                    if (typeIsCollection)
                    {
                        foreach (var item in enumerable)
                        {
                            AddMultipartItem(multiPartContent, itemName, parameterName, item);
                        }
                    }
                    else
                    {
                        AddMultipartItem(multiPartContent, itemName, parameterName, itemValue);
                    }
                }

                // NB: We defer setting headers until the body has been
                // added so any custom content headers don't get left out.
                if (headersToAdd.Count > 0)
                {
                    // We could have content headers, so we need to make
                    // sure we have an HttpContent object to add them to,
                    // provided the HttpClient will allow it for the method
                    if (ret.Content == null && !bodylessMethods.Contains(ret.Method))
                        ret.Content = new ByteArrayContent(new byte[0]);

                    foreach (var header in headersToAdd)
                    {
                        SetHeader(ret, header.Key, header.Value);
                    }
                }

                // NB: The URI methods in .NET are dumb. Also, we do this 
                // UriBuilder business so that we preserve any hardcoded query 
                // parameters as well as add the parameterized ones.
                var uri = new UriBuilder(new Uri(new Uri("http://api"), urlTarget));
                var query = HttpUtility.ParseQueryString(uri.Query ?? "");
                foreach (var key in query.AllKeys)
                {
                    queryParamsToAdd.Insert(0, new KeyValuePair<string, string>(key, query[key]));
                }

                if (queryParamsToAdd.Any())
                {
                    var pairs = queryParamsToAdd.Select(x => Uri.EscapeDataString(x.Key) + "=" + Uri.EscapeDataString(x.Value));
                    uri.Query = string.Join("&", pairs);
                }
                else
                {
                    uri.Query = null;
                }

                ret.RequestUri = new Uri(uri.Uri.GetComponents(UriComponents.PathAndQuery, UriFormat.UriEscaped), UriKind.Relative);
                return ret;
            };
        }

        Func<HttpClient, object[], IObservable<T>> BuildRxFuncForMethod<T>(RestMethodInfo restMethod)
        {
            var taskFunc = BuildCancellableTaskFuncForMethod<T>(restMethod);

            return (client, paramList) =>
            {
                return new TaskToObservable<T>(ct =>
                {
                    var methodCt = CancellationToken.None;
                    if (restMethod.CancellationToken != null)
                    {
                        methodCt = paramList.OfType<CancellationToken>().FirstOrDefault();
                    }

                    // link the two
                    var cts = CancellationTokenSource.CreateLinkedTokenSource(methodCt, ct);

                    return taskFunc(client, cts.Token, paramList);
                });
            };
        }

        Func<HttpClient, object[], Task<T>> BuildTaskFuncForMethod<T>(RestMethodInfo restMethod)
        {
            var ret = BuildCancellableTaskFuncForMethod<T>(restMethod);

            return (client, paramList) =>
            {
                if (restMethod.CancellationToken != null)
                {
                    return ret(client, paramList.OfType<CancellationToken>().FirstOrDefault(), paramList);
                }

                return ret(client, CancellationToken.None, paramList);
            };
        }

        Func<HttpClient, object[], Task> BuildVoidTaskFuncForMethod(RestMethodInfo restMethod)
        {
            return async (client, paramList) =>
            {
                if (client.BaseAddress == null)
                    throw new InvalidOperationException("BaseAddress must be set on the HttpClient instance");

                var factory = BuildRequestFactoryForMethod(restMethod, client.BaseAddress.AbsolutePath, restMethod.CancellationToken != null);
                var rq = factory(paramList);

                var ct = CancellationToken.None;

                if (restMethod.CancellationToken != null)
                {
                    ct = paramList.OfType<CancellationToken>().FirstOrDefault();
                }

                using (var resp = await client.SendAsync(rq, ct).ConfigureAwait(false))
                {
                    if (!resp.IsSuccessStatusCode)
                    {
                        throw await ApiException.Create(rq, restMethod.HttpMethod, resp, settings).ConfigureAwait(false);
                    }
                }
            };
        }

        static bool DoNotConvertToQueryMap(object value)
        {
            if (value == null)
                return false;

            var type = value.GetType();

            bool ShouldReturn() => type == typeof(string) ||
                                  typeof(IFormattable).IsAssignableFrom(type) ||
                                  type == typeof(Uri);

            // Bail out early & match string
            if (ShouldReturn())
                return true;

            // Get the element type for enumerables
            if (value is IEnumerable enu)
            {
                var ienu = typeof(IEnumerable<>);
                // We don't want to enumerate to get the type, so we'll just look for IEnumerable<T>
                var intType = type.GetInterfaces()
                                     .FirstOrDefault(i => i.GetTypeInfo().IsGenericType &&
                                                          i.GetGenericTypeDefinition() == ienu);

                if (intType != null)
                {
                    type = intType.GetGenericArguments()[0];
                }

            }

            return ShouldReturn();
        }

        static void SetHeader(HttpRequestMessage request, string name, string value)
        {
            // Clear any existing version of this header that might be set, because
            // we want to allow removal/redefinition of headers. 
            // We also don't want to double up content headers which may have been
            // set for us automatically.

            // NB: We have to enumerate the header names to check existence because 
            // Contains throws if it's the wrong header type for the collection.
            if (request.Headers.Any(x => x.Key == name))
            {
                request.Headers.Remove(name);
            }

            if (request.Content != null && request.Content.Headers.Any(x => x.Key == name))
            {
                request.Content.Headers.Remove(name);
            }

            if (value == null) return;

            var added = request.Headers.TryAddWithoutValidation(name, value);

            // Don't even bother trying to add the header as a content header
            // if we just added it to the other collection.
            if (!added && request.Content != null)
            {
                request.Content.Headers.TryAddWithoutValidation(name, value);
            }
        }
    }
}
