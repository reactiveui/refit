﻿using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;

// Enable support for C# 9 record types
#if !NET6_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
#endif

namespace Refit
{
    /// <summary>
    /// RestMethodInfo
    /// </summary>
    public record RestMethodInfo(
        string Name,
        Type HostingType,
        MethodInfo MethodInfo,
        string RelativePath,
        Type ReturnType
    );

    [DebuggerDisplay("{MethodInfo}")]
    internal class RestMethodInfoInternal
    {
        public string Name { get; set; }
        public Type Type { get; set; }
        public MethodInfo MethodInfo { get; set; }
        public HttpMethod HttpMethod { get; set; }
        public string RelativePath { get; set; }
        public bool IsMultipart { get; private set; }
        public string MultipartBoundary { get; private set; }
        public ParameterInfo? CancellationToken { get; set; }
        public Dictionary<string, string?> Headers { get; set; }
        public Dictionary<int, string> HeaderParameterMap { get; set; }
        public ISet<int> HeaderCollectionParameterMap { get; set; }
        public Dictionary<int, string> PropertyParameterMap { get; set; }
        public Tuple<BodySerializationMethod, bool, int>? BodyParameterInfo { get; set; }
        public Tuple<string, int>? AuthorizeParameterInfo { get; set; }
        public Dictionary<int, string> QueryParameterMap { get; set; }
        public Dictionary<int, Tuple<string, string>> AttachmentNameMap { get; set; }
        public Dictionary<int, ParameterInfo> ParameterInfoMap { get; set; }
        public Dictionary<int, RestMethodParameterInfo> ParameterMap { get; set; }
        public Type ReturnType { get; set; }
        public Type ReturnResultType { get; set; }
        public Type DeserializedResultType { get; set; }
        public RefitSettings RefitSettings { get; set; }
        public bool IsApiResponse { get; }
        public bool ShouldDisposeResponse { get; private set; }

        static readonly Regex ParameterRegex = new(@"{(.*?)}");
        static readonly HttpMethod PatchMethod = new("PATCH");

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public RestMethodInfoInternal(
            Type targetInterface,
            MethodInfo methodInfo,
            RefitSettings? refitSettings = null
        )
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            RefitSettings = refitSettings ?? new RefitSettings();
            Type = targetInterface ?? throw new ArgumentNullException(nameof(targetInterface));
            Name = methodInfo.Name;
            MethodInfo = methodInfo ?? throw new ArgumentNullException(nameof(methodInfo));

            var hma = methodInfo.GetCustomAttributes(true).OfType<HttpMethodAttribute>().First();

            HttpMethod = hma.Method;
            RelativePath = hma.Path;

            IsMultipart = methodInfo.GetCustomAttributes(true).OfType<MultipartAttribute>().Any();

            MultipartBoundary = IsMultipart
                ? methodInfo.GetCustomAttribute<MultipartAttribute>(true)?.BoundaryText
                    ?? new MultipartAttribute().BoundaryText
                : string.Empty;

            VerifyUrlPathIsSane(RelativePath);
            DetermineReturnTypeInfo(methodInfo);
            DetermineIfResponseMustBeDisposed();

            // Exclude cancellation token parameters from this list
            var parameterList = methodInfo
                .GetParameters()
                .Where(p => p.ParameterType != typeof(CancellationToken))
                .ToList();
            ParameterInfoMap = parameterList
                .Select((parameter, index) => new { index, parameter })
                .ToDictionary(x => x.index, x => x.parameter);
            ParameterMap = BuildParameterMap(RelativePath, parameterList);
            BodyParameterInfo = FindBodyParameter(parameterList, IsMultipart, hma.Method);
            AuthorizeParameterInfo = FindAuthorizationParameter(parameterList);

            Headers = ParseHeaders(methodInfo);
            HeaderParameterMap = BuildHeaderParameterMap(parameterList);
            HeaderCollectionParameterMap = RestMethodInfoInternal.BuildHeaderCollectionParameterMap(
                parameterList
            );
            PropertyParameterMap = BuildRequestPropertyMap(parameterList);

            // get names for multipart attachments
            AttachmentNameMap = [];
            if (IsMultipart)
            {
                for (var i = 0; i < parameterList.Count; i++)
                {
                    if (
                        ParameterMap.ContainsKey(i)
                        || HeaderParameterMap.ContainsKey(i)
                        || PropertyParameterMap.ContainsKey(i)
                        || HeaderCollectionParameterMap.Contains(i)
                    )
                    {
                        continue;
                    }

                    var attachmentName = GetAttachmentNameForParameter(parameterList[i]);
                    if (attachmentName == null)
                        continue;

                    AttachmentNameMap[i] = Tuple.Create(
                        attachmentName,
                        GetUrlNameForParameter(parameterList[i])
                    );
                }
            }

            QueryParameterMap = [];
            for (var i = 0; i < parameterList.Count; i++)
            {
                if (
                    ParameterMap.ContainsKey(i)
                    || HeaderParameterMap.ContainsKey(i)
                    || PropertyParameterMap.ContainsKey(i)
                    || HeaderCollectionParameterMap.Contains(i)
                    || (BodyParameterInfo != null && BodyParameterInfo.Item3 == i)
                    || (AuthorizeParameterInfo != null && AuthorizeParameterInfo.Item2 == i)
                )
                {
                    continue;
                }

                QueryParameterMap.Add(i, GetUrlNameForParameter(parameterList[i]));
            }

            var ctParams = methodInfo
                .GetParameters()
                .Where(p => p.ParameterType == typeof(CancellationToken))
                .ToList();
            if (ctParams.Count > 1)
            {
                throw new ArgumentException(
                    $"Argument list to method \"{methodInfo.Name}\" can only contain a single CancellationToken"
                );
            }

            CancellationToken = ctParams.FirstOrDefault();

            IsApiResponse =
                ReturnResultType!.GetTypeInfo().IsGenericType
                    && (
                        ReturnResultType!.GetGenericTypeDefinition() == typeof(ApiResponse<>)
                        || ReturnResultType.GetGenericTypeDefinition() == typeof(IApiResponse<>)
                    )
                || ReturnResultType == typeof(IApiResponse);
        }

        static HashSet<int> BuildHeaderCollectionParameterMap(List<ParameterInfo> parameterList)
        {
            var headerCollectionMap = new HashSet<int>();

            for (var i = 0; i < parameterList.Count; i++)
            {
                var param = parameterList[i];
                var headerCollection = param
                    .GetCustomAttributes(true)
                    .OfType<HeaderCollectionAttribute>()
                    .FirstOrDefault();

                if (headerCollection != null)
                {
                    //opted for IDictionary<string, string> semantics here as opposed to the looser IEnumerable<KeyValuePair<string, string>> because IDictionary will enforce uniqueness of keys
                    if (param.ParameterType.IsAssignableFrom(typeof(IDictionary<string, string>)))
                    {
                        headerCollectionMap.Add(i);
                    }
                    else
                    {
                        throw new ArgumentException(
                            $"HeaderCollection parameter of type {param.ParameterType.Name} is not assignable from IDictionary<string, string>"
                        );
                    }
                }
            }

            if (headerCollectionMap.Count > 1)
                throw new ArgumentException(
                    "Only one parameter can be a HeaderCollection parameter"
                );

            return headerCollectionMap;
        }

        public RestMethodInfo ToRestMethodInfo() =>
            new(Name, Type, MethodInfo, RelativePath, ReturnType);

        static Dictionary<int, string> BuildRequestPropertyMap(List<ParameterInfo> parameterList)
        {
            var propertyMap = new Dictionary<int, string>();

            for (var i = 0; i < parameterList.Count; i++)
            {
                var param = parameterList[i];
                var propertyAttribute = param
                    .GetCustomAttributes(true)
                    .OfType<PropertyAttribute>()
                    .FirstOrDefault();

                if (propertyAttribute != null)
                {
                    var propertyKey = !string.IsNullOrEmpty(propertyAttribute.Key)
                        ? propertyAttribute.Key
                        : param.Name!;
                    propertyMap[i] = propertyKey!;
                }
            }

            return propertyMap;
        }

        static PropertyInfo[] GetParameterProperties(ParameterInfo parameter)
        {
            return parameter
                .ParameterType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetMethod?.IsPublic == true)
                .ToArray();
        }

        static void VerifyUrlPathIsSane(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return;

            if (!relativePath.StartsWith("/"))
                throw new ArgumentException(
                    $"URL path {relativePath} must start with '/' and be of the form '/foo/bar/baz'"
                );
        }

        static Dictionary<int, RestMethodParameterInfo> BuildParameterMap(
            string relativePath,
            List<ParameterInfo> parameterInfo
        )
        {
            var ret = new Dictionary<int, RestMethodParameterInfo>();

            // This section handles pattern matching in the URL. We also need it to add parameter key/values for any attribute with a [Query]
            var parameterizedParts = relativePath
                .Split('/', '?')
                .SelectMany(x => ParameterRegex.Matches(x).Cast<Match>())
                .ToList();

            if (parameterizedParts.Count > 0)
            {
                var paramValidationDict = parameterInfo.ToDictionary(
                    k => GetUrlNameForParameter(k).ToLowerInvariant(),
                    v => v
                );
                //if the param is an lets make a dictionary for all it's potential parameters
                var objectParamValidationDict = parameterInfo
                    .Where(x => x.ParameterType.GetTypeInfo().IsClass)
                    .SelectMany(x => GetParameterProperties(x).Select(p => Tuple.Create(x, p)))
                    .GroupBy(
                        i => $"{i.Item1.Name}.{GetUrlNameForProperty(i.Item2)}".ToLowerInvariant()
                    )
                    .ToDictionary(k => k.Key, v => v.First());
                foreach (var match in parameterizedParts)
                {
                    var rawName = match.Groups[1].Value.ToLowerInvariant();
                    var isRoundTripping = rawName.StartsWith("**");
                    string name;
                    if (isRoundTripping)
                    {
                        name = rawName.Substring(2);
                    }
                    else
                    {
                        name = rawName;
                    }

                    if (paramValidationDict.TryGetValue(name, out var value)) //if it's a standard parameter
                    {
                        var paramType = value.ParameterType;
                        if (isRoundTripping && paramType != typeof(string))
                        {
                            throw new ArgumentException(
                                $"URL {relativePath} has round-tripping parameter {rawName}, but the type of matched method parameter is {paramType.FullName}. It must be a string."
                            );
                        }
                        var parameterType = isRoundTripping
                            ? ParameterType.RoundTripping
                            : ParameterType.Normal;
                        var restMethodParameterInfo = new RestMethodParameterInfo(name, value)
                        {
                            Type = parameterType
                        };
#if NET6_0_OR_GREATER
                        ret.TryAdd(
                            parameterInfo.IndexOf(restMethodParameterInfo.ParameterInfo),
                            restMethodParameterInfo
                        );
#else
                        var idx = parameterInfo.IndexOf(restMethodParameterInfo.ParameterInfo);
                        if (!ret.ContainsKey(idx))
                        {
                            ret.Add(idx, restMethodParameterInfo);
                        }
#endif
                    }
                    //else if it's a property on a object parameter
                    else if (
                        objectParamValidationDict.TryGetValue(name, out var value1)
                        && !isRoundTripping
                    )
                    {
                        var property = value1;
                        var parameterIndex = parameterInfo.IndexOf(property.Item1);
                        //If we already have this parameter, add additional ParameterProperty
                        if (ret.TryGetValue(parameterIndex, out var value2))
                        {
                            if (!value2.IsObjectPropertyParameter)
                            {
                                throw new ArgumentException(
                                    $"Parameter {property.Item1.Name} matches both a parameter and nested parameter on a parameter object"
                                );
                            }

                            value2.ParameterProperties.Add(
                                new RestMethodParameterProperty(name, property.Item2)
                            );
                        }
                        else
                        {
                            var restMethodParameterInfo = new RestMethodParameterInfo(
                                true,
                                property.Item1
                            );
                            restMethodParameterInfo.ParameterProperties.Add(
                                new RestMethodParameterProperty(name, property.Item2)
                            );
#if NET6_0_OR_GREATER
                            ret.TryAdd(
                                parameterInfo.IndexOf(restMethodParameterInfo.ParameterInfo),
                                restMethodParameterInfo
                            );
#else
                            // Do the contains check
                            var idx = parameterInfo.IndexOf(restMethodParameterInfo.ParameterInfo);
                            if (!ret.ContainsKey(idx))
                            {
                                ret.Add(idx, restMethodParameterInfo);
                            }
#endif
                        }
                    }
                    else
                    {
                        throw new ArgumentException(
                            $"URL {relativePath} has parameter {rawName}, but no method parameter matches"
                        );
                    }
                }
            }
            return ret;
        }

        static string GetUrlNameForParameter(ParameterInfo paramInfo)
        {
            var aliasAttr = paramInfo
                .GetCustomAttributes(true)
                .OfType<AliasAsAttribute>()
                .FirstOrDefault();
            return aliasAttr != null ? aliasAttr.Name : paramInfo.Name!;
        }

        static string GetUrlNameForProperty(PropertyInfo propInfo)
        {
            var aliasAttr = propInfo
                .GetCustomAttributes(true)
                .OfType<AliasAsAttribute>()
                .FirstOrDefault();
            return aliasAttr != null ? aliasAttr.Name : propInfo.Name;
        }

        static string GetAttachmentNameForParameter(ParameterInfo paramInfo)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var nameAttr = paramInfo
                .GetCustomAttributes<AttachmentNameAttribute>(true)
#pragma warning restore CS0618 // Type or member is obsolete
                .FirstOrDefault();

            // also check for AliasAs
            return nameAttr?.Name
                ?? paramInfo.GetCustomAttributes<AliasAsAttribute>(true).FirstOrDefault()?.Name!;
        }

        Tuple<BodySerializationMethod, bool, int>? FindBodyParameter(
            IList<ParameterInfo> parameterList,
            bool isMultipart,
            HttpMethod method
        )
        {
            // The body parameter is found using the following logic / order of precedence:
            // 1) [Body] attribute
            // 2) POST/PUT/PATCH: Reference type other than string
            // 3) If there are two reference types other than string, without the body attribute, throw

            var bodyParams = parameterList
                .Select(
                    x =>
                        new
                        {
                            Parameter = x,
                            BodyAttribute = x.GetCustomAttributes(true)
                                .OfType<BodyAttribute>()
                                .FirstOrDefault()
                        }
                )
                .Where(x => x.BodyAttribute != null)
                .ToList();

            // multipart requests may not contain a body, implicit or explicit
            if (isMultipart)
            {
                if (bodyParams.Count > 0)
                {
                    throw new ArgumentException(
                        "Multipart requests may not contain a Body parameter"
                    );
                }
                return null;
            }

            if (bodyParams.Count > 1)
            {
                throw new ArgumentException("Only one parameter can be a Body parameter");
            }

            // #1, body attribute wins
            if (bodyParams.Count == 1)
            {
                var ret = bodyParams[0];
                return Tuple.Create(
                    ret.BodyAttribute!.SerializationMethod,
                    ret.BodyAttribute.Buffered ?? RefitSettings.Buffered,
                    parameterList.IndexOf(ret.Parameter)
                );
            }

            // Not in post/put/patch? bail
            if (
                !method.Equals(HttpMethod.Post)
                && !method.Equals(HttpMethod.Put)
                && !method.Equals(PatchMethod)
            )
            {
                return null;
            }

            // see if we're a post/put/patch
            // explicitly skip [Query], [HeaderCollection], and [Property]-denoted params
            var refParams = parameterList
                .Where(
                    pi =>
                        !pi.ParameterType.GetTypeInfo().IsValueType
                        && pi.ParameterType != typeof(string)
                        && pi.GetCustomAttribute<QueryAttribute>() == null
                        && pi.GetCustomAttribute<HeaderCollectionAttribute>() == null
                        && pi.GetCustomAttribute<PropertyAttribute>() == null
                )
                .ToList();

            // Check for rule #3
            if (refParams.Count > 1)
            {
                throw new ArgumentException(
                    "Multiple complex types found. Specify one parameter as the body using BodyAttribute"
                );
            }

            if (refParams.Count == 1)
            {
                return Tuple.Create(
                    BodySerializationMethod.Serialized,
                    RefitSettings.Buffered,
                    parameterList.IndexOf(refParams[0])
                );
            }

            return null;
        }

        static Tuple<string, int>? FindAuthorizationParameter(IList<ParameterInfo> parameterList)
        {
            var authorizeParams = parameterList
                .Select(
                    x =>
                        new
                        {
                            Parameter = x,
                            AuthorizeAttribute = x.GetCustomAttributes(true)
                                .OfType<AuthorizeAttribute>()
                                .FirstOrDefault()
                        }
                )
                .Where(x => x.AuthorizeAttribute != null)
                .ToList();

            if (authorizeParams.Count > 1)
            {
                throw new ArgumentException("Only one parameter can be an Authorize parameter");
            }

            if (authorizeParams.Count == 1)
            {
                var ret = authorizeParams[0];
                return Tuple.Create(
                    ret.AuthorizeAttribute!.Scheme,
                    parameterList.IndexOf(ret.Parameter)
                );
            }

            return null;
        }

        static Dictionary<string, string?> ParseHeaders(MethodInfo methodInfo)
        {
            var ret = new Dictionary<string, string?>();

            var inheritedAttributes =
                methodInfo.DeclaringType != null
                    ? methodInfo
                        .DeclaringType.GetInterfaces()
                        .SelectMany(i => i.GetTypeInfo().GetCustomAttributes(true))
                        .Reverse()
                    : Array.Empty<Attribute>();

            var declaringTypeAttributes =
                methodInfo.DeclaringType != null
                    ? methodInfo.DeclaringType.GetTypeInfo().GetCustomAttributes(true)
                    : Array.Empty<Attribute>();

            // Headers set on the declaring type have to come first,
            // so headers set on the method can replace them. Switching
            // the order here will break stuff.
            var headers = inheritedAttributes
                .Concat(declaringTypeAttributes)
                .Concat(methodInfo.GetCustomAttributes(true))
                .OfType<HeadersAttribute>()
                .SelectMany(ha => ha.Headers);

            foreach (var header in headers)
            {
                if (string.IsNullOrWhiteSpace(header))
                    continue;

                // NB: Silverlight doesn't have an overload for String.Split()
                // with a count parameter, but header values can contain
                // ':' so we have to re-join all but the first part to get the
                // value.
                var parts = header.Split(':');
                ret[parts[0].Trim()] =
                    parts.Length > 1 ? string.Join(":", parts.Skip(1)).Trim() : null;
            }

            return ret;
        }

        static Dictionary<int, string> BuildHeaderParameterMap(List<ParameterInfo> parameterList)
        {
            var ret = new Dictionary<int, string>();

            for (var i = 0; i < parameterList.Count; i++)
            {
                var header = parameterList[i]
                    .GetCustomAttributes(true)
                    .OfType<HeaderAttribute>()
                    .Select(ha => ha.Header)
                    .FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(header))
                {
                    ret[i] = header.Trim();
                }
            }

            return ret;
        }

        void DetermineReturnTypeInfo(MethodInfo methodInfo)
        {
            var returnType = methodInfo.ReturnType;
            if (
                returnType.IsGenericType
                && (
                    methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)
                    || methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(ValueTask<>)
                    || methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(IObservable<>)
                )
            )
            {
                ReturnType = returnType;
                ReturnResultType = returnType.GetGenericArguments()[0];

                if (
                    ReturnResultType.IsGenericType
                    && (
                        ReturnResultType.GetGenericTypeDefinition() == typeof(ApiResponse<>)
                        || ReturnResultType.GetGenericTypeDefinition() == typeof(IApiResponse<>)
                    )
                )
                {
                    DeserializedResultType = ReturnResultType.GetGenericArguments()[0];
                }
                else if (ReturnResultType == typeof(IApiResponse))
                {
                    DeserializedResultType = typeof(HttpContent);
                }
                else
                    DeserializedResultType = ReturnResultType;
            }
            else if (returnType == typeof(Task))
            {
                ReturnType = methodInfo.ReturnType;
                ReturnResultType = typeof(void);
                DeserializedResultType = typeof(void);
            }
            else
                throw new ArgumentException(
                    $"Method \"{methodInfo.Name}\" is invalid. All REST Methods must return either Task<T> or ValueTask<T> or IObservable<T>"
                );
        }

        void DetermineIfResponseMustBeDisposed()
        {
            // Rest method caller will have to dispose if it's one of those 3
            ShouldDisposeResponse =
                DeserializedResultType != typeof(HttpResponseMessage)
                && DeserializedResultType != typeof(HttpContent)
                && DeserializedResultType != typeof(Stream);
        }
    }
}
