using System;
using System.Collections;
using System.Net.Http;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Threading;

namespace Refit
{
    public enum ParameterType
    {
        Normal,
        RoundTripping
    }

    [DebuggerDisplay("{MethodInfo}")]
    public class RestMethodInfo
    {
        public string Name { get; set; }
        public Type Type { get; set; }
        public MethodInfo MethodInfo { get; set; }
        public HttpMethod HttpMethod { get; set; }
        public string RelativePath { get; set; }
        public bool IsMultipart { get; private set; }
        public Dictionary<int, Tuple<string, ParameterType>> ParameterMap { get; set; }
        public ParameterInfo CancellationToken { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public Dictionary<int, string> HeaderParameterMap { get; set; }
        public Tuple<BodySerializationMethod, bool, int> BodyParameterInfo { get; set; }
        public Dictionary<int, string> QueryParameterMap { get; set; }
        public Dictionary<int, Tuple<string, string>> AttachmentNameMap { get; set; }
        public Dictionary<int, ParameterInfo> ParameterInfoMap { get; set; }
        public Type ReturnType { get; set; }
        public Type SerializedReturnType { get; set; }
        public RefitSettings RefitSettings { get; set; }
        public Type SerializedGenericArgument { get; set; }
        public bool IsApiResponse { get; }

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

            VerifyUrlPathIsSane(RelativePath);
            DetermineReturnTypeInfo(methodInfo);

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

                    AttachmentNameMap[i] = Tuple.Create(attachmentName, GetUrlNameForParameter(parameterList[i]).ToLowerInvariant());
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

                if (parameterList[i].GetCustomAttribute<QueryAttribute>() != null)
                {
                    var typeInfo = parameterList[i].ParameterType.GetTypeInfo();
                    var isValueType = typeInfo.IsValueType && !typeInfo.IsPrimitive && !typeInfo.IsEnum;
                    if (typeInfo.IsArray || parameterList[i].ParameterType.GetInterfaces().Contains(typeof(IEnumerable)) || isValueType)
                    {
                        QueryParameterMap.Add(QueryParameterMap.Count, GetUrlNameForParameter(parameterList[i]));
                    }
                    else
                    {
                        foreach (var member in parameterList[i].ParameterType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                        {
                            QueryParameterMap.Add(QueryParameterMap.Count, GetUrlNameForMember(member));
                        }
                    }
                    continue;
                }

                QueryParameterMap[i] = GetUrlNameForParameter(parameterList[i]);
            }

            var ctParams = methodInfo.GetParameters().Where(p => p.ParameterType == typeof(CancellationToken)).ToList();
            if (ctParams.Count > 1)
            {
                throw new ArgumentException($"Argument list to method \"{methodInfo.Name}\" can only contain a single CancellationToken");
            }

            CancellationToken = ctParams.FirstOrDefault();

            IsApiResponse = SerializedReturnType.GetTypeInfo().IsGenericType &&
                                SerializedReturnType.GetGenericTypeDefinition() == typeof(ApiResponse<>);
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

        Dictionary<int, Tuple<string, ParameterType>> BuildParameterMap(string relativePath, List<ParameterInfo> parameterInfo)
        {
            var ret = new Dictionary<int, Tuple<string, ParameterType>>();

            // This section handles pattern matching in the URL. We also need it to add parameter key/values for any attribute with a [Query]
            var parameterizedParts = relativePath.Split('/', '?')
                .SelectMany(x => ParameterRegex.Matches(x).Cast<Match>())
                .ToList();

            if (parameterizedParts.Count > 0)
            {
                var paramValidationDict = parameterInfo.ToDictionary(k => GetUrlNameForParameter(k).ToLowerInvariant(), v => v);

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

                    if (!paramValidationDict.ContainsKey(name))
                    {
                        throw new ArgumentException($"URL {relativePath} has parameter {rawName}, but no method parameter matches");
                    }

                    var paramType = paramValidationDict[name].ParameterType;
                    if (isRoundTripping && paramType != typeof(string))
                    {
                        throw new ArgumentException($"URL {relativePath} has round-tripping parameter {rawName}, but the type of matched method parameter is {paramType.FullName}. It must be a string.");
                    }

                    var parameterType = isRoundTripping ? ParameterType.RoundTripping : ParameterType.Normal;
                    ret.Add(parameterInfo.IndexOf(paramValidationDict[name]), Tuple.Create(name, parameterType));
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

        string GetUrlNameForMember(MemberInfo memberInfo)
        {
            var aliasAttr = memberInfo.GetCustomAttributes(true)
                .OfType<AliasAsAttribute>()
                .FirstOrDefault();
            return aliasAttr != null ? aliasAttr.Name : memberInfo.Name;
        }

        string GetAttachmentNameForParameter(ParameterInfo paramInfo)
        {
            var nameAttr = paramInfo.GetCustomAttributes(true)
#pragma warning disable 618
                .OfType<AttachmentNameAttribute>()
#pragma warning restore 618
                .FirstOrDefault();
            return nameAttr?.Name;
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
            if (methodInfo.ReturnType.GetTypeInfo().IsGenericType == false)
            {
                if (methodInfo.ReturnType != typeof(Task))
                {
                    goto bogusMethod;
                }

                ReturnType = methodInfo.ReturnType;
                SerializedReturnType = typeof(void);
                return;
            }

            var genericType = methodInfo.ReturnType.GetGenericTypeDefinition();
            if (genericType != typeof(Task<>) && genericType != typeof(IObservable<>))
            {
                goto bogusMethod;
            }

            ReturnType = methodInfo.ReturnType;
            SerializedReturnType = methodInfo.ReturnType.GetGenericArguments()[0];

            if (SerializedReturnType.GetTypeInfo().IsGenericType)
                SerializedGenericArgument = SerializedReturnType.GetGenericArguments()[0];

            return;

bogusMethod:
            throw new ArgumentException($"Method \"{methodInfo.Name}\" is invalid. All REST Methods must return either Task<T> or IObservable<T>");
        }
    }
}
