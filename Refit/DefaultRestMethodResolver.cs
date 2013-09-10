using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Refit
{
    

    public class DefaultRestMethodResolver : IRestMethodResolver
    {
        static readonly Regex parameterRegex = new Regex(@"^{(.*)}$");

        public Dictionary<string, RestMethodInfo> GetInterfaceRestMethodInfo(Type targetInterface)
        {
            return targetInterface.GetMethods()
                .SelectMany(x =>
                {
                    var attrs = x.GetCustomAttributes(true);
                    var hasHttpMethod = attrs.OfType<HttpMethodAttribute>().Any();
                    if (!hasHttpMethod) return Enumerable.Empty<RestMethodInfo>();

                    return EnumerableEx.Return(buildRestMethodInfo(targetInterface, x));
                })
                .ToDictionary(k => k.Name, v => v);
        }

        // Public so we can build the items for the tests? Makes sense?
        public RestMethodInfo buildRestMethodInfo(Type targetInterface, MethodInfo methodInfo)
        {
            var restMethodInfo = new RestMethodInfo(targetInterface, methodInfo);

            var hma = methodInfo.GetCustomAttributes(true)
                .OfType<HttpMethodAttribute>()
                .First();

            restMethodInfo.HttpMethod = hma.Method;
            restMethodInfo.RelativePath = hma.Path;

            verifyUrlPathIsSane(restMethodInfo.RelativePath);
            determineReturnTypeInfo(restMethodInfo);

            var parameterList = methodInfo.GetParameters().ToList();

            buildParameterMap(restMethodInfo, parameterList);
            determineBodyParameter(restMethodInfo, parameterList);

            restMethodInfo.QueryParameterMap = new Dictionary<int, string>();
            for (int i=0; i < parameterList.Count; i++) {
                if (restMethodInfo.ParameterMap.ContainsKey(i) || (restMethodInfo.BodyParameterInfo != null && restMethodInfo.BodyParameterInfo.Item2 == i)) {
                    continue;
                }

                restMethodInfo.QueryParameterMap[i] = getUrlNameForParameter(parameterList[i]);
            }

            return restMethodInfo;
        }

        void verifyUrlPathIsSane(string relativePath)
        {
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

        void determineReturnTypeInfo(RestMethodInfo restMethodInfo)
        {
            var methodInfo = restMethodInfo.MethodInfo;
            if (methodInfo.ReturnType.IsGenericType == false && methodInfo.ReturnType != typeof(Task)) {
                goto bogusMethod;
            }

            var genericType = methodInfo.ReturnType.GetGenericTypeDefinition();
            if (genericType != typeof(Task<>) && genericType != typeof(IObservable<>)) {
                goto bogusMethod;
            }

            restMethodInfo.ReturnType = methodInfo.ReturnType;
            restMethodInfo.SerializedReturnType = methodInfo.ReturnType.GetGenericArguments()[0];
            if (restMethodInfo.SerializedReturnType == typeof(HttpResponseMessage)) restMethodInfo.SerializedReturnType = null;
            return;

        bogusMethod:
            throw new ArgumentException("All REST Methods must return either Task<T> or IObservable<T>");
        }

        void buildParameterMap(RestMethodInfo restMethodInfo, List<ParameterInfo> parameterInfo)
        {
            var ret = new Dictionary<int, string>();

            var parameterizedParts = restMethodInfo.RelativePath.Split('/', '?').SelectMany(x => {
                var m = parameterRegex.Match(x);
                return (m.Success ? EnumerableEx.Return(m) : Enumerable.Empty<Match>());
            }).ToList();

            if (parameterizedParts.Count == 0) {
                restMethodInfo.ParameterMap = ret;
                return;
            }

            var paramValidationDict = parameterInfo.ToDictionary(k => getUrlNameForParameter(k).ToLowerInvariant(), v => v);

            foreach (var match in parameterizedParts) {
                var name = match.Groups[1].Value.ToLowerInvariant();
                if (!paramValidationDict.ContainsKey(name)) {
                    throw new ArgumentException(String.Format("URL has parameter {0}, but no method parameter matches", name));
                }

                ret.Add(parameterInfo.IndexOf(paramValidationDict[name]), name);
            }

            restMethodInfo.ParameterMap = ret;
        }

        string getUrlNameForParameter(ParameterInfo paramInfo)
        {
            var aliasAttr = paramInfo.GetCustomAttributes(true)
                .OfType<AliasAsAttribute>()
                .FirstOrDefault();
            return aliasAttr != null ? aliasAttr.Name : paramInfo.Name;
        }

        void determineBodyParameter(RestMethodInfo restMethodInfo, List<ParameterInfo> parameterList)
        {
            var bodyParams = parameterList
                .Select(x => new { Parameter = x, BodyAttribute = x.GetCustomAttributes(true).OfType<BodyAttribute>().FirstOrDefault() })
                .Where(x => x.BodyAttribute != null)
                .ToList();

            if (bodyParams.Count > 1) {
                throw new ArgumentException("Only one parameter can be a Body parameter");
            }

            if (bodyParams.Count == 0) {
                return;
            }

            var ret = bodyParams[0];
            restMethodInfo.BodyParameterInfo = Tuple.Create(ret.BodyAttribute.SerializationMethod, parameterList.IndexOf(ret.Parameter));
        }
    }
}
