using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Threading;

namespace Refit
{
    [DebuggerDisplay("{MethodInfo}")]
    public class RestMethodInfo
    {
        public string Name { get; set; }
        public Type Type { get; set; }
        public MethodInfo MethodInfo { get; set; }
        public HttpMethod HttpMethod { get; set; }
        public string RelativePath { get; set; }
        public bool IsMultipart { get; private set; }
        public string MultipartBoundary { get; private set; }
        public ParameterInfo CancellationToken { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public Dictionary<int, string> HeaderParameterMap { get; set; }
        public Tuple<BodySerializationMethod, bool, int> BodyParameterInfo { get; set; }
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

        static readonly Regex ParameterRegex = new Regex(@"{(.*?)}");
        static readonly HttpMethod PatchMethod = new HttpMethod("PATCH");

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

            IsMultipart = methodInfo.GetCustomAttributes(true)
                .OfType<MultipartAttribute>()
                .Any();

            MultipartBoundary = IsMultipart ? methodInfo.GetCustomAttribute<MultipartAttribute>(true).BoundaryText : string.Empty;

            VerifyUrlPathIsSane(RelativePath);
            DetermineReturnTypeInfo(methodInfo);
            DetermineIfResponseMustBeDisposed();

            // Exclude cancellation token parameters from this list
            var parameterList = methodInfo.GetParameters().Where(p => p.ParameterType != typeof(CancellationToken)).ToList();
            ParameterInfoMap = parameterList
                .Select((parameter, index) => new { index, parameter })
                .ToDictionary(x => x.index, x => x.parameter);
            ParameterMap = BuildParameterMap(RelativePath, parameterList);
            BodyParameterInfo = FindBodyParameter(parameterList, IsMultipart, hma.Method);

            Headers = ParseHeaders(methodInfo);
            HeaderParameterMap = BuildHeaderParameterMap(parameterList);

            // get names for multipart attachments
            AttachmentNameMap = new Dictionary<int, Tuple<string, string>>();
            if (IsMultipart)
            {
                for (var i = 0; i < parameterList.Count; i++)
                {
                    if (ParameterMap.ContainsKey(i) || HeaderParameterMap.ContainsKey(i))
                    {
                        continue;
                    }

                    var attachmentName = GetAttachmentNameForParameter(parameterList[i]);
                    if (attachmentName == null)
                        continue;

                    AttachmentNameMap[i] = Tuple.Create(attachmentName, GetUrlNameForParameter(parameterList[i]));
                }
            }

            QueryParameterMap = new Dictionary<int, string>();
            for (var i = 0; i < parameterList.Count; i++)
            {
                if (ParameterMap.ContainsKey(i) ||
                    HeaderParameterMap.ContainsKey(i) ||
                    (BodyParameterInfo != null && BodyParameterInfo.Item3 == i))
                {
                    continue;
                }                

                QueryParameterMap.Add(i, GetUrlNameForParameter(parameterList[i]));
            }

            var ctParams = methodInfo.GetParameters().Where(p => p.ParameterType == typeof(CancellationToken)).ToList();
            if (ctParams.Count > 1)
            {
                throw new ArgumentException($"Argument list to method \"{methodInfo.Name}\" can only contain a single CancellationToken");
            }

            CancellationToken = ctParams.FirstOrDefault();

            IsApiResponse = ReturnResultType.GetTypeInfo().IsGenericType &&
                            (ReturnResultType.GetGenericTypeDefinition() == typeof(ApiResponse<>)
                             || ReturnResultType.GetGenericTypeDefinition()  == typeof(IApiResponse<>)
                             || ReturnResultType == typeof(IApiResponse));
        }

        private PropertyInfo[] GetParameterProperties(ParameterInfo parameter)
        {
            return parameter.ParameterType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetMethod.IsPublic).ToArray();
        }

        void VerifyUrlPathIsSane(string relativePath)
        {
            if (relativePath == "")
                return;

            if (!relativePath.StartsWith("/"))
            {
                goto bogusPath;
            }

            var parts = relativePath.Split('/');
            if (parts.Length == 0)
            {
                goto bogusPath;
            }

            return;

bogusPath:
            throw new ArgumentException($"URL path {relativePath} must be of the form '/foo/bar/baz'");
        }

        Dictionary<int, RestMethodParameterInfo> BuildParameterMap(string relativePath, List<ParameterInfo> parameterInfo)
        {
            var ret = new Dictionary<int, RestMethodParameterInfo>();

            // This section handles pattern matching in the URL. We also need it to add parameter key/values for any attribute with a [Query]
            var parameterizedParts = relativePath.Split('/', '?')
                .SelectMany(x => ParameterRegex.Matches(x).Cast<Match>())
                .ToList();

            if (parameterizedParts.Count > 0)
            {
                var paramValidationDict = parameterInfo.ToDictionary(k => GetUrlNameForParameter(k).ToLowerInvariant(), v => v);
                //if the param is an lets make a dictionary for all it's potential parameters
                var objectParamValidationDict = parameterInfo.Where(x => x.ParameterType.GetTypeInfo().IsClass)
                                                                 .SelectMany(x => GetParameterProperties(x).Select(p => Tuple.Create(x, p)))
                                                                 .GroupBy(i => $"{i.Item1.Name}.{GetUrlNameForProperty(i.Item2)}".ToLowerInvariant())
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

                    if (paramValidationDict.ContainsKey(name)) //if it's a standard parameter
                    {
                        var paramType = paramValidationDict[name].ParameterType;
                        if (isRoundTripping && paramType != typeof(string))
                        {
                            throw new ArgumentException($"URL {relativePath} has round-tripping parameter {rawName}, but the type of matched method parameter is {paramType.FullName}. It must be a string.");
                        }
                        var parameterType = isRoundTripping ? ParameterType.RoundTripping : ParameterType.Normal;
                        var restMethodParameterInfo = new RestMethodParameterInfo(name, paramValidationDict[name]) { Type = parameterType };
                        ret.Add(parameterInfo.IndexOf(restMethodParameterInfo.ParameterInfo), restMethodParameterInfo);
                    }
                    //else if it's a property on a object parameter
                    else if (objectParamValidationDict.ContainsKey(name) && !isRoundTripping)
                    {
                        var property = objectParamValidationDict[name];
                        var parameterIndex = parameterInfo.IndexOf(property.Item1);
                        //If we already have this parameter, add additional ParameterProperty
                        if (ret.ContainsKey(parameterIndex))
                        {
                            if (!ret[parameterIndex].IsObjectPropertyParameter)
                            {
                                throw new ArgumentException($"Parameter {property.Item1.Name} matches both a parameter and nested parameter on a parameter object");
                            }
                            //we already have this parameter. add the additional property
                            ret[parameterIndex].ParameterProperties.Add(new RestMethodParameterProperty(name, property.Item2));
                        }
                        else
                        {
                            var restMethodParameterInfo = new RestMethodParameterInfo(true, property.Item1);
                            restMethodParameterInfo.ParameterProperties.Add(new RestMethodParameterProperty(name, property.Item2));
                            ret.Add(parameterInfo.IndexOf(restMethodParameterInfo.ParameterInfo), restMethodParameterInfo);
                        }
                    }
                    else
                    {
                        throw new ArgumentException($"URL {relativePath} has parameter {rawName}, but no method parameter matches");
                    }

                }
            }
            return ret;
        }

        string GetUrlNameForParameter(ParameterInfo paramInfo)
        {
            var aliasAttr = paramInfo.GetCustomAttributes(true)
                .OfType<AliasAsAttribute>()
                .FirstOrDefault();
            return aliasAttr != null ? aliasAttr.Name : paramInfo.Name;
        }
        string GetUrlNameForProperty(PropertyInfo propInfo)
        {
            var aliasAttr = propInfo.GetCustomAttributes(true)
                .OfType<AliasAsAttribute>()
                .FirstOrDefault();
            return aliasAttr != null ? aliasAttr.Name : propInfo.Name;
        }

        string GetAttachmentNameForParameter(ParameterInfo paramInfo)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var nameAttr = paramInfo.GetCustomAttributes<AttachmentNameAttribute>(true)
#pragma warning restore CS0618 // Type or member is obsolete
                .FirstOrDefault();

            // also check for AliasAs
            return nameAttr?.Name ?? paramInfo.GetCustomAttributes<AliasAsAttribute>(true).FirstOrDefault()?.Name;
        }

        Tuple<BodySerializationMethod, bool, int> FindBodyParameter(IList<ParameterInfo> parameterList, bool isMultipart, HttpMethod method)
        {

            // The body parameter is found using the following logic / order of precedence:
            // 1) [Body] attribute
            // 2) POST/PUT/PATCH: Reference type other than string
            // 3) If there are two reference types other than string, without the body attribute, throw

            var bodyParams = parameterList
                .Select(x => new { Parameter = x, BodyAttribute = x.GetCustomAttributes(true).OfType<BodyAttribute>().FirstOrDefault() })
                .Where(x => x.BodyAttribute != null)
                .ToList();

            // multipart requests may not contain a body, implicit or explicit
            if (isMultipart)
            {
                if (bodyParams.Count > 0)
                {
                    throw new ArgumentException("Multipart requests may not contain a Body parameter");
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
                return Tuple.Create(ret.BodyAttribute.SerializationMethod, ret.BodyAttribute.Buffered ?? RefitSettings.Buffered,
                    parameterList.IndexOf(ret.Parameter));
            }

            // Not in post/put/patch? bail
            if (!method.Equals(HttpMethod.Post) && !method.Equals(HttpMethod.Put) && !method.Equals(PatchMethod))
            {
                return null;
            }

            // see if we're a post/put/patch
            // BH: explicitly skip [Query]-denoted params
            var refParams = parameterList.Where(pi => !pi.ParameterType.GetTypeInfo().IsValueType && pi.ParameterType != typeof(string) && pi.GetCustomAttribute<QueryAttribute>() == null).ToList();

            // Check for rule #3
            if (refParams.Count > 1)
            {
                throw new ArgumentException("Multiple complex types found. Specify one parameter as the body using BodyAttribute");
            }

            if (refParams.Count == 1)
            {
                return Tuple.Create(BodySerializationMethod.Serialized, false, parameterList.IndexOf(refParams[0]));
            }

            return null;
        }

        Dictionary<string, string> ParseHeaders(MethodInfo methodInfo)
        {
            var ret = new Dictionary<string, string>();

            var inheritedAttributes = methodInfo.DeclaringType != null
                ? methodInfo.DeclaringType.GetInterfaces().SelectMany(i => i.GetTypeInfo().GetCustomAttributes(true)).Reverse()
                : new Attribute[0];

            var declaringTypeAttributes = methodInfo.DeclaringType != null
                ? methodInfo.DeclaringType.GetTypeInfo().GetCustomAttributes(true)
                : new Attribute[0];

            // Headers set on the declaring type have to come first,
            // so headers set on the method can replace them. Switching
            // the order here will break stuff.
            var headers = inheritedAttributes.Concat(declaringTypeAttributes).Concat(methodInfo.GetCustomAttributes(true))
                .OfType<HeadersAttribute>()
                .SelectMany(ha => ha.Headers);

            foreach (var header in headers)
            {
                if (string.IsNullOrWhiteSpace(header)) continue;

                // NB: Silverlight doesn't have an overload for String.Split()
                // with a count parameter, but header values can contain
                // ':' so we have to re-join all but the first part to get the
                // value.
                var parts = header.Split(':');
                ret[parts[0].Trim()] = parts.Length > 1 ?
                    string.Join(":", parts.Skip(1)).Trim() : null;
            }

            return ret;
        }

        Dictionary<int, string> BuildHeaderParameterMap(List<ParameterInfo> parameterList)
        {
            var ret = new Dictionary<int, string>();

            for (var i = 0; i < parameterList.Count; i++)
            {
                var header = parameterList[i].GetCustomAttributes(true)
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
            if (returnType.IsGenericType && (methodInfo.ReturnType.GetGenericTypeDefinition() != typeof(Task<>)
                                             || methodInfo.ReturnType.GetGenericTypeDefinition() != typeof(IObservable<>)))
            {
                ReturnType = returnType;
                ReturnResultType = returnType.GetGenericArguments()[0];

                if (ReturnResultType.IsGenericType &&
                    (ReturnResultType.GetGenericTypeDefinition() == typeof(ApiResponse<>)
                     || ReturnResultType.GetGenericTypeDefinition() == typeof(IApiResponse<>)))
                {
                        DeserializedResultType = ReturnResultType.GetGenericArguments()[0];
                }
                else if (ReturnResultType == typeof(IApiResponse))
                {
                    DeserializedResultType = typeof(HttpContent);
                }else
                    DeserializedResultType = ReturnResultType;
            }
            else if (returnType == typeof(Task))
            {
                ReturnType = methodInfo.ReturnType;
                ReturnResultType = typeof(void);
                DeserializedResultType = typeof(void);
            }
            else
                throw new ArgumentException($"Method \"{methodInfo.Name}\" is invalid. All REST Methods must return either Task<T> or IObservable<T>");
        }

        private void DetermineIfResponseMustBeDisposed()
        {
            // Rest method caller will have to dispose if it's one of those 3
            ShouldDisposeResponse = DeserializedResultType != typeof(HttpResponseMessage) &&
                                    DeserializedResultType != typeof(HttpContent) &&
                                    DeserializedResultType != typeof(Stream);
        }

    }
}
