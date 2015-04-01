using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Refit
{
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

            var names = RefitSettings.UriTemplateHandler.GetParameterNames(relativePath); //new UriTemplate(relativePath).GetParameterNames();
            if (names.Count() == 0) {
                return ret;
            }

            var paramValidationDict = parameterInfo.ToDictionary(k => getUrlNameForParameter(k).ToLowerInvariant(), v => v);

            foreach (var name in names)
            {
                var normalizedName = name.ToLowerInvariant();
                if (!paramValidationDict.ContainsKey(normalizedName))
                {
                    throw new ArgumentException(String.Format("URL has parameter {0}, but no method parameter matches", name));
                }

                ret.Add(parameterInfo.IndexOf(paramValidationDict[normalizedName]), normalizedName);
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
}