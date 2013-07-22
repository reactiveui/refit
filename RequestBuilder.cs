using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using NUnit.Framework;
using System.Text;
using Newtonsoft.Json;
using System.IO;
using System.Web;

namespace Refit
{
    public class RequestBuilder
    {
        readonly Type targetType;
        readonly Dictionary<string, RestMethodInfo> interfaceHttpMethods;

        public RequestBuilder(Type targetInterface)
        {
            if (targetInterface == null || !targetInterface.IsInterface) {
                throw new ArgumentException("targetInterface must be an Interface");
            }

            targetType = targetInterface;
            interfaceHttpMethods = targetInterface.GetMethods()
                .SelectMany(x => {
                    var attrs = x.GetCustomAttributes(true);
                    var httpMethod = attrs.Select(y => y as HttpMethodAttribute).FirstOrDefault(y => y != null);
                    if (httpMethod == null) return Enumerable.Empty<RestMethodInfo>();

                    return EnumerableEx.Return(new RestMethodInfo(targetInterface, x));
                })
                .ToDictionary(k => k.Name, v => v);
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
                        if (streamParam != null) {
                            ret.Content = new StreamContent(streamParam);
                        } else {
                            ret.Content = new StringContent(JsonConvert.SerializeObject(paramList[i]), Encoding.UTF8);
                        }
                        continue;
                    }

                    queryParamsToAdd[restMethod.QueryParameterMap[i]] = paramList[i].ToString();
                }

                // NB: The URI methods in .NET are dumb. Also, we do this 
                // UriBuilder business so that we preserve any hardcoded query 
                // parameters as well as add the parameterized ones.
                var uri = new UriBuilder(new Uri(new Uri("http://api"), urlTarget.ToString()));
                var query = HttpUtility.ParseQueryString(uri.Query);
                foreach(var kvp in queryParamsToAdd) {
                    query.Add(kvp.Key, kvp.Value);
                }

                ret.RequestUri = new Uri(uri.Uri.PathAndQuery, UriKind.Relative);
                return ret;
            };
        }
    }

    class RestMethodInfo
    {
        public string Name { get; set; }
        public Type Type { get; set; }
        public MethodInfo MethodInfo { get; set; }
        public HttpMethod HttpMethod { get; set; }
        public string RelativePath { get; set; }
        public Dictionary<int, string> ParameterMap { get; set; }
        public Tuple<BodySerializationMethod, int> BodyParameterInfo { get; set; }
        public Dictionary<int, string> QueryParameterMap { get; set; }

        static readonly Regex parameterRegex = new Regex(@"^{(.*)}$");

        public RestMethodInfo(Type targetInterface, MethodInfo methodInfo)
        {
            Type = targetInterface;
            Name = methodInfo.Name;
            MethodInfo = methodInfo;

            var hma = methodInfo.GetCustomAttributes(true)
                .Select(y => y as HttpMethodAttribute)
                .First(y => y != null);

            HttpMethod = hma.Method;
            RelativePath = hma.Path;

            verifyUrlPathIsSane(RelativePath);

            var parameterList = methodInfo.GetParameters().ToList();

            ParameterMap = buildParameterMap(RelativePath, parameterList);
            BodyParameterInfo = findBodyParameter(parameterList);

            for (int i=0; i < parameterList.Count; i++) {
                if (ParameterMap.ContainsKey(i) || (BodyParameterInfo != null && BodyParameterInfo.Item2 == i)) {
                    continue;
                }

                QueryParameterMap[i] = getUrlNameForParameter(parameterList[i]);
            }
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

        Dictionary<int, string> buildParameterMap(string relativePath, List<ParameterInfo> parameterInfo)
        {
            var ret = new Dictionary<int, string>();

            var parameterizedParts = relativePath.Split('/').SelectMany(x => {
                var m = parameterRegex.Match(x);
                return (m.Success ? EnumerableEx.Return(m) : Enumerable.Empty<Match>());
            }).ToList();

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
                .Select(x => x as AliasAsAttribute)
                .FirstOrDefault(x => x != null);
            return aliasAttr != null ? aliasAttr.Name : paramInfo.Name;
        }

        Tuple<BodySerializationMethod, int> findBodyParameter(List<ParameterInfo> parameterList)
        {
            var bodyParams = parameterList
                .Select(x => new { Parameter = x, BodyAttribute = x.GetCustomAttributes(true).Select(y => y as BodyAttribute).FirstOrDefault() })
                .ToList();

            if (bodyParams.Count(x => x.BodyAttribute != null) > 1) {
                throw new ArgumentException("Only one parameter can be a Body parameter");
            }

            var ret = bodyParams.FirstOrDefault(x => x.BodyAttribute != null);
            if (ret == null) return null;

            return Tuple.Create(ret.BodyAttribute.SerializationMethod, parameterList.IndexOf(ret.Parameter));
        }
    }

    /*
     * TESTS
     */

    public interface IRestMethodInfoTests
    {
        [Get("@)!@_!($_!@($\\\\|||::::")]
        Task<string> GarbagePath();
                
        [Get("/foo/bar/{id}")]
        Task<string> FetchSomeStuffMissingParameters();

        [Get("/foo/bar/{id}")]
        Task<string> FetchSomeStuff(int id);

        [Get("/foo/bar/{id}")]
        Task<string> FetchSomeStuffWithAlias([AliasAs("id")] int anId);
                
        [Get("/foo/bar/{id}")]
        Task<string> FetchSomeStuffWithBody([AliasAs("id")] int anId, [Body] Dictionary<int, string> theData);
    }

    public interface IDummyHttpApi
    {
        [Get("/foo/bar/{id}")]
        Task<string> FetchSomeStuff(int id);

        string SomeOtherMethod();
    }

    [TestFixture]
    public class RestMethodInfoTests
    {
        [Test]
        public void GarbagePathsShouldThrow()
        {
            bool shouldDie = true;

            try {
                var input = typeof(IRestMethodInfoTests);
                var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "GarbagePath"));
            } catch (ArgumentException) {
                shouldDie = false;
            }

            Assert.IsFalse(shouldDie);
        }

        [Test]
        public void MissingParametersShouldBlowUp()
        {
            bool shouldDie = true;

            try {
                var input = typeof(IRestMethodInfoTests);
                var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "FetchSomeStuffMissingParameters"));
            } catch (ArgumentException) {
                shouldDie = false;
            }

            Assert.IsFalse(shouldDie);
        }

        [Test]
        public void ParameterMappingSmokeTest()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "FetchSomeStuff"));
            Assert.AreEqual("id", fixture.ParameterMap[0]);
            Assert.IsNull(fixture.BodyParameterInfo);
        }

        [Test]
        public void AliasMappingShouldWork()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "FetchSomeStuffWithAlias"));
            Assert.AreEqual("id", fixture.ParameterMap[0]);
            Assert.IsNull(fixture.BodyParameterInfo);
        }

        [Test]
        public void FindTheBodyParameter()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "FetchSomeStuffWithBody"));
            Assert.AreEqual("id", fixture.ParameterMap[0]);

            Assert.IsNotNull(fixture.BodyParameterInfo);
            Assert.AreEqual(1, fixture.BodyParameterInfo.Item2);
        }
    }

    [TestFixture]
    public class RequestBuilderTests
    {
        [Test]
        public void MethodsThatDontHaveAnHttpMethodShouldFail()
        {
            var failureMethods = new[] { 
                "SomeOtherMethod",
                "weofjwoeijfwe",
                null,
            };

            var successMethods = new[] {
                "FetchSomeStuff",
            };

            foreach (var v in failureMethods) {
                bool shouldDie = true;

                try {
                    var fixture = new RequestBuilder(typeof(IDummyHttpApi));
                    fixture.BuildRequestFactoryForMethod(v);
                } catch (Exception ex) {
                    shouldDie = false;
                }
                Assert.IsFalse(shouldDie);
            }

            foreach (var v in successMethods) {
                bool shouldDie = false;

                try {
                    var fixture = new RequestBuilder(typeof(IDummyHttpApi));
                    fixture.BuildRequestFactoryForMethod(v);
                } catch (Exception ex) {
                    shouldDie = true;
                }

                Assert.IsFalse(shouldDie);
            }
        }
    }
}