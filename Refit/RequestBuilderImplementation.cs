using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Web;

namespace Refit
{
    class RequestBuilderImplementation<TApi>(RefitSettings? refitSettings = null)
        : RequestBuilderImplementation(typeof(TApi), refitSettings),
            IRequestBuilder<TApi> { }

    partial class RequestBuilderImplementation : IRequestBuilder
    {
        private const int StackallocThreshold = 512;
        static readonly QueryAttribute DefaultQueryAttribute = new ();
        static readonly Uri BaseUri = new ("http://api");
        readonly Dictionary<string, List<RestMethodInfoInternal>> interfaceHttpMethods;
        readonly ConcurrentDictionary<
            CloseGenericMethodKey,
            RestMethodInfoInternal
        > interfaceGenericHttpMethods;
        readonly IHttpContentSerializer serializer;
        readonly RefitSettings settings;
        public Type TargetType { get; }

        public RequestBuilderImplementation(
            Type refitInterfaceType,
            RefitSettings? refitSettings = null
        )
        {
            var targetInterfaceInheritedInterfaces = refitInterfaceType.GetInterfaces();

            settings = refitSettings ?? new RefitSettings();
            serializer = settings.ContentSerializer;
            interfaceGenericHttpMethods =
                new ConcurrentDictionary<CloseGenericMethodKey, RestMethodInfoInternal>();

            if (refitInterfaceType == null || !refitInterfaceType.GetTypeInfo().IsInterface)
            {
                throw new ArgumentException("targetInterface must be an Interface");
            }

            TargetType = refitInterfaceType;

            var dict = new Dictionary<string, List<RestMethodInfoInternal>>();

            AddInterfaceHttpMethods(refitInterfaceType, dict);
            foreach (var inheritedInterface in targetInterfaceInheritedInterfaces)
            {
                AddInterfaceHttpMethods(inheritedInterface, dict);
            }

            interfaceHttpMethods = dict;
        }

        void AddInterfaceHttpMethods(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]Type interfaceType,
            Dictionary<string, List<RestMethodInfoInternal>> methods
        )
        {
            // Consider public (the implicit visibility) and non-public abstract members of the interfaceType
            var methodInfos = interfaceType
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Where(i => i.IsAbstract);

            foreach (var methodInfo in methodInfos)
            {
                var attrs = methodInfo.GetCustomAttributes(true);
                var hasHttpMethod = attrs.OfType<HttpMethodAttribute>().Any();
                if (hasHttpMethod)
                {
                    if (!methods.TryGetValue(methodInfo.Name, out var value))
                    {
                        value = [];
                        methods.Add(methodInfo.Name, value);
                    }

                    var restinfo = new RestMethodInfoInternal(interfaceType, methodInfo, settings);
                    value.Add(restinfo);
                }
            }
        }

        RestMethodInfoInternal FindMatchingRestMethodInfo(
            string key,
            Type[]? parameterTypes,
            Type[]? genericArgumentTypes
        )
        {
            if (!interfaceHttpMethods.TryGetValue(key, out var httpMethods))
            {
                throw new ArgumentException(
                    "Method must be defined and have an HTTP Method attribute"
                );
            }

            if (parameterTypes == null)
            {
                if (httpMethods.Count > 1)
                {
                    throw new ArgumentException(
                        $"MethodName exists more than once, '{nameof(parameterTypes)}' mut be defined"
                    );
                }

                return CloseGenericMethodIfNeeded(httpMethods[0], genericArgumentTypes);
            }

            var isGeneric = genericArgumentTypes?.Length > 0;

            var possibleMethodsCollection = httpMethods.Where(
                method => method.MethodInfo.GetParameters().Length == parameterTypes.Length
            );

            // If it's a generic method, add that filter
            if (isGeneric)
                possibleMethodsCollection = possibleMethodsCollection.Where(
                    method =>
                        method.MethodInfo.IsGenericMethod
                        && method.MethodInfo.GetGenericArguments().Length
                            == genericArgumentTypes!.Length
                );
            else // exclude generic methods
                possibleMethodsCollection = possibleMethodsCollection.Where(
                    method => !method.MethodInfo.IsGenericMethod
                );

            var possibleMethods = possibleMethodsCollection.ToArray();

            if (possibleMethods.Length == 1)
                return CloseGenericMethodIfNeeded(possibleMethods[0], genericArgumentTypes);

            foreach (var method in possibleMethods)
            {
                var match = method
                    .MethodInfo.GetParameters()
                    .Select(p => p.ParameterType)
                    .SequenceEqual(parameterTypes);
                if (match)
                {
                    return CloseGenericMethodIfNeeded(method, genericArgumentTypes);
                }
            }

            throw new Exception("No suitable Method found...");
        }

        RestMethodInfoInternal CloseGenericMethodIfNeeded(
            RestMethodInfoInternal restMethodInfo,
            Type[]? genericArgumentTypes
        )
        {
            if (genericArgumentTypes != null)
            {
                return interfaceGenericHttpMethods.GetOrAdd(
                    new CloseGenericMethodKey(restMethodInfo.MethodInfo, genericArgumentTypes),
                    _ =>
                        new RestMethodInfoInternal(
                            restMethodInfo.Type,
                            restMethodInfo.MethodInfo.MakeGenericMethod(genericArgumentTypes),
                            restMethodInfo.RefitSettings
                        )
                );
            }
            return restMethodInfo;
        }

        public Func<HttpClient, object[], object?> BuildRestResultFuncForMethod(
            string methodName,
            Type[]? parameterTypes = null,
            Type[]? genericArgumentTypes = null
        )
        {
            if (!interfaceHttpMethods.ContainsKey(methodName))
            {
                throw new ArgumentException(
                    "Method must be defined and have an HTTP Method attribute"
                );
            }

            var restMethod = FindMatchingRestMethodInfo(
                methodName,
                parameterTypes,
                genericArgumentTypes
            );
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
                var taskFuncMi = typeof(RequestBuilderImplementation).GetMethod(
                    nameof(BuildTaskFuncForMethod),
                    BindingFlags.NonPublic | BindingFlags.Instance
                );
                var taskFunc = (MulticastDelegate?)
                    (
                        taskFuncMi!.MakeGenericMethod(
                            restMethod.ReturnResultType,
                            restMethod.DeserializedResultType
                        )
                    ).Invoke(this, [restMethod]);

                return (client, args) => taskFunc!.DynamicInvoke(client, args);
            }

            // Same deal
            var rxFuncMi = typeof(RequestBuilderImplementation).GetMethod(
                nameof(BuildRxFuncForMethod),
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            var rxFunc = (MulticastDelegate?)
                (
                    rxFuncMi!.MakeGenericMethod(
                        restMethod.ReturnResultType,
                        restMethod.DeserializedResultType
                    )
                ).Invoke(this, [restMethod]);

            return (client, args) => rxFunc!.DynamicInvoke(client, args);
        }

        void AddMultipartItem(
            MultipartFormDataContent multiPartContent,
            string fileName,
            string parameterName,
            object itemValue
        )
        {
            if (itemValue is HttpContent content)
            {
                multiPartContent.Add(content);
                return;
            }
            if (itemValue is MultipartItem multipartItem)
            {
                var httpContent = multipartItem.ToContent();
                multiPartContent.Add(
                    httpContent,
                    multipartItem.Name ?? parameterName,
                    string.IsNullOrEmpty(multipartItem.FileName) ? fileName : multipartItem.FileName
                );
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

            if (itemValue is byte[] byteArrayValue)
            {
                var fileContent = new ByteArrayContent(byteArrayValue);
                multiPartContent.Add(fileContent, parameterName, fileName);
                return;
            }

            // Fallback to serializer
            Exception e;
            try
            {
                multiPartContent.Add(
                    settings.ContentSerializer.ToHttpContent(itemValue),
                    parameterName
                );
                return;
            }
            catch (Exception ex)
            {
                // Eat this since we're about to throw as a fallback anyway
                e = ex;
            }

            throw new ArgumentException(
                $"Unexpected parameter type in a Multipart request. Parameter {fileName} is of type {itemValue.GetType().Name}, whereas allowed types are String, Stream, FileInfo, Byte array and anything that's JSON serializable",
                nameof(itemValue),
                e
            );
        }

        Func<HttpClient, CancellationToken, object[], Task<T?>> BuildCancellableTaskFuncForMethod<
            T,
            TBody
        >(RestMethodInfoInternal restMethod)
        {
            return async (client, ct, paramList) =>
            {
                if (client.BaseAddress == null)
                    throw new InvalidOperationException(
                        "BaseAddress must be set on the HttpClient instance"
                    );

                var factory = BuildRequestFactoryForMethod(
                    restMethod,
                    client.BaseAddress.AbsolutePath,
                    restMethod.CancellationToken != null
                );
                var rq = factory(paramList);
                HttpResponseMessage? resp = null;
                HttpContent? content = null;
                var disposeResponse = true;
                try
                {
                    // Load the data into buffer when body should be buffered.
                    if (IsBodyBuffered(restMethod, rq))
                    {
                        await rq.Content!.LoadIntoBufferAsync().ConfigureAwait(false);
                    }
                    resp = await client
                        .SendAsync(rq, HttpCompletionOption.ResponseHeadersRead, ct)
                        .ConfigureAwait(false);
                    content = resp.Content ?? new StringContent(string.Empty);
                    Exception? e = null;
                    disposeResponse = restMethod.ShouldDisposeResponse;

                    if (typeof(T) != typeof(HttpResponseMessage))
                    {
                        e = await settings.ExceptionFactory(resp).ConfigureAwait(false);
                    }

                    if (restMethod.IsApiResponse)
                    {
                        var body = default(TBody);

                        try
                        {
                            // Only attempt to deserialize content if no error present for backward-compatibility
                            body =
                                e == null
                                    ? await DeserializeContentAsync<TBody>(resp, content, ct)
                                        .ConfigureAwait(false)
                                    : default;
                        }
                        catch (Exception ex)
                        {
                            //if an error occured while attempting to deserialize return the wrapped ApiException
                            if (settings.DeserializationExceptionFactory != null)
                                e = await settings.DeserializationExceptionFactory(resp, ex).ConfigureAwait(false);
                            else
                            {
                                e = await ApiException.Create(
                                    "An error occured deserializing the response.",
                                    resp.RequestMessage!,
                                    resp.RequestMessage!.Method,
                                    resp,
                                    settings,
                                    ex
                                ).ConfigureAwait(false);
                            }
                        }

                        return ApiResponse.Create<T, TBody>(
                            resp,
                            body,
                            settings,
                            e as ApiException
                        );
                    }
                    else if (e != null)
                    {
                        disposeResponse = false; // caller has to dispose
                        throw e;
                    }
                    else
                    {
                        try
                        {
                            return await DeserializeContentAsync<T>(resp, content, ct)
                                .ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            if (settings.DeserializationExceptionFactory != null)
                            {
                                var customEx = await settings.DeserializationExceptionFactory(resp, ex).ConfigureAwait(false);
                                if (customEx != null)
                                    throw customEx;
                                return default;
                            }
                            else
                            {
                                throw await ApiException.Create(
                                    "An error occured deserializing the response.",
                                    resp.RequestMessage!,
                                    resp.RequestMessage!.Method,
                                    resp,
                                    settings,
                                    ex
                                ).ConfigureAwait(false);
                            }
                        }
                    }
                }
                finally
                {
                    // Ensure we clean up the request
                    // Especially important if it has open files/streams
                    rq.Dispose();
                    if (disposeResponse)
                    {
                        resp?.Dispose();
                        content?.Dispose();
                    }
                }
            };
        }

        async Task<T?> DeserializeContentAsync<T>(
            HttpResponseMessage resp,
            HttpContent content,
            CancellationToken cancellationToken
        )
        {
            T? result;
            if (typeof(T) == typeof(HttpResponseMessage))
            {
                // NB: This double-casting manual-boxing hate crime is the only way to make
                // this work without a 'class' generic constraint. It could blow up at runtime
                // and would be A Bad Idea if we hadn't already vetted the return type.
                result = (T)(object)resp;
            }
            else if (typeof(T) == typeof(HttpContent))
            {
                result = (T)(object)content;
            }
            else if (typeof(T) == typeof(Stream))
            {
                var stream = (object)
                    await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                result = (T)stream;
            }
            else if (typeof(T) == typeof(string))
            {
                using var stream = await content
                    .ReadAsStreamAsync(cancellationToken)
                    .ConfigureAwait(false);
                using var reader = new StreamReader(stream);
                var str = (object)await reader.ReadToEndAsync().ConfigureAwait(false);
                result = (T)str;
            }
            else
            {
                result = await serializer
                    .FromHttpContentAsync<T>(content, cancellationToken)
                    .ConfigureAwait(false);
            }
            return result;
        }

        List<KeyValuePair<string, object?>> BuildQueryMap(
            object? @object,
            string? delimiter = null,
            RestMethodParameterInfo? parameterInfo = null
        )
        {
            if (@object is IDictionary idictionary)
            {
                return BuildQueryMap(idictionary, delimiter);
            }

            var kvps = new List<KeyValuePair<string, object?>>();

            if (@object is null)
                return kvps;

            var props = @object
                .GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.CanRead && p.GetMethod?.IsPublic == true);

            foreach (var propertyInfo in props)
            {
                var obj = propertyInfo.GetValue(@object);
                if (obj == null)
                    continue;

                //if we have a parameter info lets check it to make sure it isn't bound to the path
                if (parameterInfo is { IsObjectPropertyParameter: true })
                {
                    if (parameterInfo.ParameterProperties.Any(x => x.PropertyInfo == propertyInfo))
                    {
                        continue;
                    }
                }

                var key = propertyInfo.Name;

                var aliasAttribute = propertyInfo.GetCustomAttribute<AliasAsAttribute>();
                key = aliasAttribute?.Name ?? settings.UrlParameterKeyFormatter.Format(key);

                // Look to see if the property has a Query attribute, and if so, format it accordingly
                var queryAttribute = propertyInfo.GetCustomAttribute<QueryAttribute>();
                if (queryAttribute is { Format: not null })
                {
                    obj = settings.FormUrlEncodedParameterFormatter.Format(
                        obj,
                        queryAttribute.Format
                    );
                }

                // If obj is IEnumerable - format it accounting for Query attribute and CollectionFormat
                if (obj is not string && obj is IEnumerable ienu && obj is not IDictionary)
                {
                    foreach (
                        var value in ParseEnumerableQueryParameterValue(
                            ienu,
                            propertyInfo,
                            propertyInfo.PropertyType,
                            queryAttribute
                        )
                    )
                    {
                        kvps.Add(new KeyValuePair<string, object?>(key, value));
                    }

                    continue;
                }

                if (DoNotConvertToQueryMap(obj))
                {
                    kvps.Add(new KeyValuePair<string, object?>(key, obj));
                    continue;
                }

                switch (obj)
                {
                    case IDictionary idict:
                        foreach (var keyValuePair in BuildQueryMap(idict, delimiter))
                        {
                            kvps.Add(
                                new KeyValuePair<string, object?>(
                                    $"{key}{delimiter}{keyValuePair.Key}",
                                    keyValuePair.Value
                                )
                            );
                        }

                        break;

                    default:
                        foreach (var keyValuePair in BuildQueryMap(obj, delimiter))
                        {
                            kvps.Add(
                                new KeyValuePair<string, object?>(
                                    $"{key}{delimiter}{keyValuePair.Key}",
                                    keyValuePair.Value
                                )
                            );
                        }

                        break;
                }
            }

            return kvps;
        }

        List<KeyValuePair<string, object?>> BuildQueryMap(
            IDictionary dictionary,
            string? delimiter = null
        )
        {
            var kvps = new List<KeyValuePair<string, object?>>();

            foreach (var key in dictionary.Keys)
            {
                var obj = dictionary[key];
                if (obj == null)
                    continue;

                var keyType = key.GetType();
                var formattedKey = settings.UrlParameterFormatter.Format(key, keyType, keyType);

                if (string.IsNullOrWhiteSpace(formattedKey)) // blank keys can't be put in the query string
                {
                    continue;
                }

                if (DoNotConvertToQueryMap(obj))
                {
                    kvps.Add(new KeyValuePair<string, object?>(formattedKey!, obj));
                }
                else
                {
                    foreach (var keyValuePair in BuildQueryMap(obj, delimiter))
                    {
                        kvps.Add(
                            new KeyValuePair<string, object?>(
                                $"{formattedKey}{delimiter}{keyValuePair.Key}",
                                keyValuePair.Value
                            )
                        );
                    }
                }
            }

            return kvps;
        }

        Func<object[], HttpRequestMessage> BuildRequestFactoryForMethod(
            RestMethodInfoInternal restMethod,
            string basePath,
            bool paramsContainsCancellationToken
        )
        {
            return paramList =>
            {
                // make sure we strip out any cancellation tokens
                if (paramsContainsCancellationToken)
                {
                    paramList = paramList
                        .Where(o => o == null || o.GetType() != typeof(CancellationToken))
                        .ToArray();
                }

                var ret = new HttpRequestMessage { Method = restMethod.HttpMethod };

                // set up multipart content
                MultipartFormDataContent? multiPartContent = null;
                if (restMethod.IsMultipart)
                {
                    multiPartContent = new MultipartFormDataContent(restMethod.MultipartBoundary);
                    ret.Content = multiPartContent;
                }

                List<KeyValuePair<string, string?>>? queryParamsToAdd = null;
                var headersToAdd = restMethod.Headers.Count > 0 ?
                    new Dictionary<string, string?>(restMethod.Headers)
                    : null;

                RestMethodParameterInfo? parameterInfo = null;

                for (var i = 0; i < paramList.Length; i++)
                {
                    var isParameterMappedToRequest = false;
                    var param = paramList[i];
                    // if part of REST resource URL, substitute it in
                    if (restMethod.ParameterMap.TryGetValue(i, out var parameterMapValue))
                    {
                        parameterInfo = parameterMapValue;
                        if (!parameterInfo.IsObjectPropertyParameter)
                        {
                            // mark parameter mapped if not an object
                            // we want objects to fall through so any parameters on this object not bound here get passed as query parameters
                            isParameterMappedToRequest = true;
                        }
                    }

                    // if marked as body, add to content
                    if (
                        restMethod.BodyParameterInfo != null
                        && restMethod.BodyParameterInfo.Item3 == i
                    )
                    {
                        AddBodyToRequest(restMethod, param, ret);
                        isParameterMappedToRequest = true;
                    }

                    // if header, add to request headers
                    if (restMethod.HeaderParameterMap.TryGetValue(i, out var headerParameterValue))
                    {
                        headersToAdd ??= [];
                        headersToAdd[headerParameterValue] = param?.ToString();
                        isParameterMappedToRequest = true;
                    }

                    //if header collection, add to request headers
                    if (restMethod.HeaderCollectionAt(i))
                    {
                        if (param is IDictionary<string, string> headerCollection)
                        {
                            foreach (var header in headerCollection)
                            {
                                headersToAdd ??= [];
                                headersToAdd[header.Key] = header.Value;
                            }
                        }

                        isParameterMappedToRequest = true;
                    }

                    //if authorize, add to request headers with scheme
                    if (
                        restMethod.AuthorizeParameterInfo != null
                        && restMethod.AuthorizeParameterInfo.Item2 == i
                    )
                    {
                        headersToAdd ??= [];
                        headersToAdd["Authorization"] =
                            $"{restMethod.AuthorizeParameterInfo.Item1} {param}";
                        isParameterMappedToRequest = true;
                    }

                    //if property, add to populate into HttpRequestMessage.Properties
                    if (restMethod.PropertyParameterMap.ContainsKey(i))
                    {
                        isParameterMappedToRequest = true;
                    }

                    // ignore nulls and already processed parameters
                    if (isParameterMappedToRequest || param == null)
                        continue;

                    // for anything that fell through to here, if this is not a multipart method add the parameter to the query string
                    // or if is an object bound to the path add any non-path bound properties to query string
                    // or if it's an object with a query attribute
                    var queryAttribute = restMethod
                        .ParameterInfoArray[i]
                        .GetCustomAttribute<QueryAttribute>();
                    if (
                        !restMethod.IsMultipart
                        || restMethod.ParameterMap.ContainsKey(i)
                            && restMethod.ParameterMap[i].IsObjectPropertyParameter
                        || queryAttribute != null
                    )
                    {
                        queryParamsToAdd ??= [];
                        AddQueryParameters(restMethod, queryAttribute, param, queryParamsToAdd, i, parameterInfo);
                        continue;
                    }

                    AddMultiPart(restMethod, i, param, multiPartContent);
                }

                AddHeadersToRequest(headersToAdd, ret);

                AddPropertiesToRequest(restMethod, ret, paramList);
#if NET6_0_OR_GREATER
                AddVersionToRequest(ret);
#endif
                // NB: The URI methods in .NET are dumb. Also, we do this
                // UriBuilder business so that we preserve any hardcoded query
                // parameters as well as add the parameterized ones.
                var urlTarget = BuildRelativePath(basePath, restMethod, paramList);

                var uri = new UriBuilder(new Uri(BaseUri, urlTarget));
                ParseExistingQueryString(uri, ref queryParamsToAdd);

                if (queryParamsToAdd is not null && queryParamsToAdd.Count != 0)
                {
                    uri.Query = CreateQueryString(queryParamsToAdd);;
                }
                else
                {
                    uri.Query = null;
                }

                ret.RequestUri = new Uri(
                    uri.Uri.GetComponents(UriComponents.PathAndQuery, restMethod.QueryUriFormat),
                    UriKind.Relative
                );
                return ret;
            };
        }

        string BuildRelativePath(string basePath, RestMethodInfoInternal restMethod, object[] paramList)
        {
            basePath = basePath == "/" ? string.Empty : basePath;
            var pathFragments = restMethod.FragmentPath;
            if (pathFragments.Count == 0)
            {
                return basePath;
            }
            if (string.IsNullOrEmpty(basePath) && pathFragments.Count == 1)
            {
                Debug.Assert(pathFragments[0].IsConstant);
                return pathFragments[0].Value!;
            }

#pragma warning disable CA2000
            var vsb = new ValueStringBuilder(stackalloc char[StackallocThreshold]);
#pragma warning restore CA2000
            vsb.Append(basePath);

            foreach (var fragment in pathFragments)
            {
                AppendPathFragmentValue(ref vsb, restMethod, paramList, fragment);
            }

            return vsb.ToString();
        }

        void AppendPathFragmentValue(ref ValueStringBuilder vsb, RestMethodInfoInternal restMethod, object[] paramList,
            ParameterFragment fragment)
        {
            if (fragment.IsConstant)
            {
                vsb.Append(fragment.Value!);
                return;
            }

            var contains = restMethod.ParameterMap.TryGetValue(fragment.ArgumentIndex, out var parameterMapValue);
            if (!contains || parameterMapValue is null)
                throw new InvalidOperationException($"{restMethod.ParameterMap} should contain parameter.");

            if (fragment.IsObjectProperty)
            {
                var param = paramList[fragment.ArgumentIndex];
                var property = parameterMapValue.ParameterProperties[fragment.PropertyIndex];
                var propertyObject = property.PropertyInfo.GetValue(param);

                vsb.Append(Uri.EscapeDataString(settings.UrlParameterFormatter.Format(
                    propertyObject,
                    property.PropertyInfo,
                    property.PropertyInfo.PropertyType
                ) ?? string.Empty));
                return;
            }

            if (fragment.IsDynamicRoute)
            {
                var param = paramList[fragment.ArgumentIndex];

                if (parameterMapValue.Type == ParameterType.Normal)
                {
                    vsb.Append(Uri.EscapeDataString(
                        settings.UrlParameterFormatter.Format(
                            param,
                            restMethod.ParameterInfoArray[fragment.ArgumentIndex],
                            restMethod.ParameterInfoArray[fragment.ArgumentIndex].ParameterType
                        ) ?? string.Empty
                    ));
                    return;
                }

                // If round tripping, split string up, format each segment and append to vsb.
                Debug.Assert(parameterMapValue.Type == ParameterType.RoundTripping);
                var paramValue = (string)param;
                var split = paramValue.Split('/');

                var firstSection = true;
                foreach (var section in split)
                {
                    if(!firstSection)
                        vsb.Append('/');

                    vsb.Append(
                        Uri.EscapeDataString(
                            settings.UrlParameterFormatter.Format(
                                section,
                                restMethod.ParameterInfoArray[fragment.ArgumentIndex],
                                restMethod.ParameterInfoArray[fragment.ArgumentIndex].ParameterType
                            ) ?? string.Empty
                        ));
                    firstSection = false;
                }

                return;
            }

            throw new ArgumentException($"{nameof(ParameterFragment)} is in an invalid form.");
        }

        void AddBodyToRequest(RestMethodInfoInternal restMethod, object param, HttpRequestMessage ret)
        {
            if (param is HttpContent httpContentParam)
            {
                ret.Content = httpContentParam;
            }
            else if (param is Stream streamParam)
            {
                ret.Content = new StreamContent(streamParam);
            }
            // Default sends raw strings
            else if (
                restMethod.BodyParameterInfo!.Item1 == BodySerializationMethod.Default
                && param is string stringParam
            )
            {
                ret.Content = new StringContent(stringParam);
            }
            else
            {
                switch (restMethod.BodyParameterInfo.Item1)
                {
                    case BodySerializationMethod.UrlEncoded:
                        ret.Content = param is string str
                            ? (HttpContent)
                            new StringContent(
                                Uri.EscapeDataString(str),
                                Encoding.UTF8,
                                "application/x-www-form-urlencoded"
                            )
                            : new FormUrlEncodedContent(
                                new FormValueMultimap(param, settings)
                            );
                        break;
                    case BodySerializationMethod.Default:
#pragma warning disable CS0618 // Type or member is obsolete
                    case BodySerializationMethod.Json:
#pragma warning restore CS0618 // Type or member is obsolete
                    case BodySerializationMethod.Serialized:
                        var content = serializer.ToHttpContent(param);
                        switch (restMethod.BodyParameterInfo.Item2)
                        {
                            case false:
                                ret.Content = new PushStreamContent(
#pragma warning disable IDE1006 // Naming Styles
                                    async (stream, _, __) =>
#pragma warning restore IDE1006 // Naming Styles
                                    {
                                        using (stream)
                                        {
                                            await content
                                                .CopyToAsync(stream)
                                                .ConfigureAwait(false);
                                        }
                                    },
                                    content.Headers.ContentType
                                );
                                break;
                            case true:
                                ret.Content = content;
                                break;
                        }

                        break;
                }
            }
        }

        void AddQueryParameters(RestMethodInfoInternal restMethod, QueryAttribute? queryAttribute, object param,
            List<KeyValuePair<string, string?>> queryParamsToAdd, int i, RestMethodParameterInfo? parameterInfo)
        {
            var attr = queryAttribute ?? DefaultQueryAttribute;
            if (DoNotConvertToQueryMap(param))
            {
                queryParamsToAdd.AddRange(
                    ParseQueryParameter(
                        param,
                        restMethod.ParameterInfoArray[i],
                        restMethod.QueryParameterMap[i],
                        attr
                    )
                );
            }
            else
            {
                foreach (var kvp in BuildQueryMap(param, attr.Delimiter, parameterInfo))
                {
                    var path = !string.IsNullOrWhiteSpace(attr.Prefix)
                        ? $"{attr.Prefix}{attr.Delimiter}{kvp.Key}"
                        : kvp.Key;
                    queryParamsToAdd.AddRange(
                        ParseQueryParameter(
                            kvp.Value,
                            restMethod.ParameterInfoArray[i],
                            path,
                            attr
                        )
                    );
                }
            }
        }

        void AddMultiPart(RestMethodInfoInternal restMethod, int i, object param,
            MultipartFormDataContent? multiPartContent)
        {
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
            if (param is IEnumerable<object> enumerable)
            {
                foreach (var item in enumerable!)
                {
                    AddMultipartItem(multiPartContent!, itemName, parameterName, item);
                }
            }
            else
            {
                AddMultipartItem(multiPartContent!, itemName, parameterName, param);
            }
        }

        static void AddHeadersToRequest(Dictionary<string, string?>? headersToAdd, HttpRequestMessage ret)
        {
            // NB: We defer setting headers until the body has been
            // added so any custom content headers don't get left out.
            if (headersToAdd is null || headersToAdd.Count <= 0)
                return;

            // We could have content headers, so we need to make
            // sure we have an HttpContent object to add them to,
            // provided the HttpClient will allow it for the method
            if (ret.Content == null && !IsBodyless(ret.Method))
                ret.Content = new ByteArrayContent([]);

            foreach (var header in headersToAdd)
            {
                SetHeader(ret, header.Key, header.Value);
            }
        }

        void AddPropertiesToRequest(RestMethodInfoInternal restMethod, HttpRequestMessage ret, object[] paramList)
        {
            // Add RefitSetting.HttpRequestMessageOptions to the HttpRequestMessage
            if (settings.HttpRequestMessageOptions != null)
            {
                foreach (var p in settings.HttpRequestMessageOptions)
                {
#if NET6_0_OR_GREATER
                    ret.Options.Set(new HttpRequestOptionsKey<object>(p.Key), p.Value);
#else
                    ret.Properties.Add(p);
#endif
                }
            }

            for (var i = 0; i < paramList.Length; i++)
            {
                if (restMethod.PropertyParameterMap.TryGetValue(i, out var propertyKey))
                {
#if NET6_0_OR_GREATER
                    ret.Options.Set(
                        new HttpRequestOptionsKey<object?>(propertyKey),
                        paramList[i]
                    );
#else
                    ret.Properties[propertyKey] = paramList[i];
#endif
                }
            }

            // Always add the top-level type of the interface to the properties
#if NET6_0_OR_GREATER
                ret.Options.Set(
                    new HttpRequestOptionsKey<Type>(HttpRequestMessageOptions.InterfaceType),
                    TargetType
                );
                ret.Options.Set(
                    new HttpRequestOptionsKey<RestMethodInfo>(
                        HttpRequestMessageOptions.RestMethodInfo
                    ),
                    restMethod.RestMethodInfo
                );
#else
            ret.Properties[HttpRequestMessageOptions.InterfaceType] = TargetType;
            ret.Properties[HttpRequestMessageOptions.RestMethodInfo] =
                restMethod.RestMethodInfo;
#endif
        }

#if NET6_0_OR_GREATER
        void AddVersionToRequest(HttpRequestMessage ret)
        {
            ret.Version = settings.Version;
            ret.VersionPolicy = settings.VersionPolicy;
        }
#endif

        IEnumerable<KeyValuePair<string, string?>> ParseQueryParameter(
            object? param,
            ParameterInfo parameterInfo,
            string queryPath,
            QueryAttribute queryAttribute
        )
        {
            if (param is not string && param is IEnumerable paramValues)
            {
                foreach (
                    var value in ParseEnumerableQueryParameterValue(
                        paramValues,
                        parameterInfo,
                        parameterInfo.ParameterType,
                        queryAttribute
                    )
                )
                {
                    yield return new KeyValuePair<string, string?>(queryPath, value);
                }
            }
            else
            {
                yield return new KeyValuePair<string, string?>(
                    queryPath,
                    settings.UrlParameterFormatter.Format(
                        param,
                        parameterInfo,
                        parameterInfo.ParameterType
                    )
                );
            }
        }

        IEnumerable<string?> ParseEnumerableQueryParameterValue(
            IEnumerable paramValues,
            ICustomAttributeProvider customAttributeProvider,
            Type type,
            QueryAttribute? queryAttribute
        )
        {
            var collectionFormat =
                queryAttribute != null && queryAttribute.IsCollectionFormatSpecified
                    ? queryAttribute.CollectionFormat
                    : settings.CollectionFormat;

            switch (collectionFormat)
            {
                case CollectionFormat.Multi:
                    foreach (var paramValue in paramValues)
                    {
                        yield return settings.UrlParameterFormatter.Format(
                            paramValue,
                            customAttributeProvider,
                            type
                        );
                    }

                    break;

                default:
                    var delimiter =
                        collectionFormat switch
                        {
                            CollectionFormat.Ssv => " ",
                            CollectionFormat.Tsv => "\t",
                            CollectionFormat.Pipes => "|",
                            _ => ","
                        };

                    // Missing a "default" clause was preventing the collection from serializing at all, as it was hitting "continue" thus causing an off-by-one error
                    var formattedValues = paramValues
                        .Cast<object>()
                        .Select(
                            v =>
                                settings.UrlParameterFormatter.Format(
                                    v,
                                    customAttributeProvider,
                                    type
                                )
                        );

                    yield return string.Join(delimiter, formattedValues);

                    break;
            }
        }

        static void ParseExistingQueryString(UriBuilder uri, ref List<KeyValuePair<string, string?>>? queryParamsToAdd)
        {
            if (string.IsNullOrEmpty(uri.Query))
                return;

            queryParamsToAdd ??= [];
            var query = HttpUtility.ParseQueryString(uri.Query);
            var index = 0;
            foreach (var key in query.AllKeys)
            {
                if (!string.IsNullOrWhiteSpace(key))
                {
                    queryParamsToAdd.Insert(
                        index++,
                        new KeyValuePair<string, string?>(key, query[key])
                    );
                }
            }
        }

        static string CreateQueryString(List<KeyValuePair<string, string?>> queryParamsToAdd)
        {
            // Suppress warning as ValueStringBuilder.ToString calls Dispose()
#pragma warning disable CA2000
            var vsb = new ValueStringBuilder(stackalloc char[StackallocThreshold]);
#pragma warning restore CA2000
            var firstQuery = true;
            foreach (var queryParam in queryParamsToAdd)
            {
                if(queryParam is not { Key: not null, Value: not null })
                    continue;
                if(!firstQuery)
                {
                    // for all items after the first we add a & symbol
                    vsb.Append('&');
                }
                var key = Uri.EscapeDataString(queryParam.Key);
#if NET6_0_OR_GREATER
                // if first query does not start with ? then prepend it
                if (vsb.Length == 0 && key.Length > 0 && key[0] != '?')
                {
                    // query starts with ?
                    vsb.Append('?');
                }
#endif
                vsb.Append(key);
                vsb.Append('=');
                vsb.Append(Uri.EscapeDataString(queryParam.Value ?? string.Empty));
                if (firstQuery)
                    firstQuery = false;
            }

            return vsb.ToString();
        }

        Func<HttpClient, object[], IObservable<T?>> BuildRxFuncForMethod<T, TBody>(
            RestMethodInfoInternal restMethod
        )
        {
            var taskFunc = BuildCancellableTaskFuncForMethod<T, TBody>(restMethod);

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

        Func<HttpClient, object[], Task<T?>> BuildTaskFuncForMethod<T, TBody>(
            RestMethodInfoInternal restMethod
        )
        {
            var ret = BuildCancellableTaskFuncForMethod<T, TBody>(restMethod);

            return (client, paramList) =>
            {
                if (restMethod.CancellationToken != null)
                {
                    return ret(
                        client,
                        paramList.OfType<CancellationToken>().FirstOrDefault(),
                        paramList
                    );
                }

                return ret(client, CancellationToken.None, paramList);
            };
        }

        Func<HttpClient, object[], Task> BuildVoidTaskFuncForMethod(
            RestMethodInfoInternal restMethod
        )
        {
            return async (client, paramList) =>
            {
                if (client.BaseAddress == null)
                    throw new InvalidOperationException(
                        "BaseAddress must be set on the HttpClient instance"
                    );

                var factory = BuildRequestFactoryForMethod(
                    restMethod,
                    client.BaseAddress.AbsolutePath,
                    restMethod.CancellationToken != null
                );
                var rq = factory(paramList);

                var ct = CancellationToken.None;

                if (restMethod.CancellationToken != null)
                {
                    ct = paramList.OfType<CancellationToken>().FirstOrDefault();
                }

                // Load the data into buffer when body should be buffered.
                if (IsBodyBuffered(restMethod, rq))
                {
                    await rq.Content!.LoadIntoBufferAsync().ConfigureAwait(false);
                }
                using var resp = await client.SendAsync(rq, ct).ConfigureAwait(false);

                var exception = await settings.ExceptionFactory(resp).ConfigureAwait(false);
                if (exception != null)
                {
                    throw exception;
                }
            };
        }

        private static bool IsBodyBuffered(
            RestMethodInfoInternal restMethod,
            HttpRequestMessage? request
        )
        {
            return (restMethod.BodyParameterInfo?.Item2 ?? false) && (request?.Content != null);
        }

        static bool DoNotConvertToQueryMap(object? value)
        {
            if (value == null)
                return false;

            var type = value.GetType();

            // Bail out early & match string
            if (ShouldReturn(type))
                return true;

            if (value is not IEnumerable)
                return false;

            // Get the element type for enumerables
            var ienu = typeof(IEnumerable<>);
            // We don't want to enumerate to get the type, so we'll just look for IEnumerable<T>
            var intType = type.GetInterfaces()
                .FirstOrDefault(
                    i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == ienu
                );

            if (intType == null)
                return false;

            type = intType.GetGenericArguments()[0];
            return ShouldReturn(type);

            // Check if type is a simple string or IFormattable type, check underlying type if Nullable<T>
            static bool ShouldReturn(Type type) =>
                Nullable.GetUnderlyingType(type) is { } underlyingType
                    ? ShouldReturn(underlyingType)
                    : type == typeof(string)
                      || type == typeof(bool)
                      || type == typeof(char)
                      || typeof(IFormattable).IsAssignableFrom(type)
                      || type == typeof(Uri);
        }

        static void SetHeader(HttpRequestMessage request, string name, string? value)
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

            if (value == null)
                return;

            // CRLF injection protection
            name = EnsureSafe(name);
            value = EnsureSafe(value);

            var added = request.Headers.TryAddWithoutValidation(name, value);

            // Don't even bother trying to add the header as a content header
            // if we just added it to the other collection.
            if (!added && request.Content != null)
            {
                request.Content.Headers.TryAddWithoutValidation(name, value);
            }
        }

        static string EnsureSafe(string value)
        {
            // Remove CR and LF characters
#pragma warning disable CA1307 // Specify StringComparison for clarity
            return value.Replace("\r", string.Empty).Replace("\n", string.Empty);
#pragma warning restore CA1307 // Specify StringComparison for clarity
        }

        static bool IsBodyless(HttpMethod method) => method == HttpMethod.Get || method == HttpMethod.Head;
    }
}
