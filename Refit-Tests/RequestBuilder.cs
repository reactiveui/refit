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
using System.Threading;

namespace Refit.Tests
{
    [Headers("User-Agent: Refit Test Client", "Api-Version: 1")]
    public interface IRestMethodInfoTests
    {
        [Get("@)!@_!($_!@($\\\\|||::::")]
        Task<string> GarbagePath();
                
        [Get("/foo/bar/{id}")]
        Task<string> FetchSomeStuffMissingParameters();

        [Get("/foo/bar/{id}")]
        Task<string> FetchSomeStuff(int id);

        [Get("/foo/bar/{id}?baz=bamf")]
        Task<string> FetchSomeStuffWithHardcodedQueryParam(int id);

        [Get("/foo/bar/{id}?baz=bamf")]
        Task<string> FetchSomeStuffWithQueryParam(int id, string search);

        [Get("/foo/bar/{id}")]
        Task<string> FetchSomeStuffWithAlias([AliasAs("id")] int anId);
                
        [Get("/foo/bar/{id}")]
        IObservable<string> FetchSomeStuffWithBody([AliasAs("id")] int anId, [Body] Dictionary<int, string> theData);

        [Get("/foo/bar/{id}")]
        [Headers("Api-Version: 2 ")]
        Task<string> FetchSomeStuffWithHardcodedHeaders(int id);

        [Get("/foo/bar/{id}")]
        Task<string> FetchSomeStuffWithDynamicHeader(int id, [Header("Authorization")] string authorization);

        [Post("/foo/{id}")]
        Task VoidPost(int id);

        [Post("/foo/{id}")]
        string AsyncOnlyBuddy(int id);
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
            Assert.AreEqual(0, fixture.QueryParameterMap.Count);
            Assert.IsNull(fixture.BodyParameterInfo);
        }

        [Test]
        public void ParameterMappingWithQuerySmokeTest()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "FetchSomeStuffWithQueryParam"));
            Assert.AreEqual("id", fixture.ParameterMap[0]);
            Assert.AreEqual("search", fixture.QueryParameterMap[1]);
            Assert.IsNull(fixture.BodyParameterInfo);
        }

        [Test]
        public void ParameterMappingWithHardcodedQuerySmokeTest()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "FetchSomeStuffWithHardcodedQueryParam"));
            Assert.AreEqual("id", fixture.ParameterMap[0]);
            Assert.AreEqual(0, fixture.QueryParameterMap.Count);
            Assert.IsNull(fixture.BodyParameterInfo);
        }

        [Test]
        public void AliasMappingShouldWork()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "FetchSomeStuffWithAlias"));
            Assert.AreEqual("id", fixture.ParameterMap[0]);
            Assert.AreEqual(0, fixture.QueryParameterMap.Count);
            Assert.IsNull(fixture.BodyParameterInfo);
        }

        [Test]
        public void FindTheBodyParameter()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "FetchSomeStuffWithBody"));
            Assert.AreEqual("id", fixture.ParameterMap[0]);

            Assert.IsNotNull(fixture.BodyParameterInfo);
            Assert.AreEqual(0, fixture.QueryParameterMap.Count);
            Assert.AreEqual(1, fixture.BodyParameterInfo.Item2);
        }

        [Test]
        public void HardcodedHeadersShouldWork()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "FetchSomeStuffWithHardcodedHeaders"));
            Assert.AreEqual("id", fixture.ParameterMap[0]);
            Assert.AreEqual(0, fixture.QueryParameterMap.Count);
            Assert.IsNull(fixture.BodyParameterInfo);

            Assert.IsTrue(fixture.Headers.ContainsKey("Api-Version"), "Headers include Api-Version header");
            Assert.AreEqual("2", fixture.Headers["Api-Version"]);
            Assert.IsTrue(fixture.Headers.ContainsKey("User-Agent"), "Headers include User-Agent header");
            Assert.AreEqual("Refit Test Client", fixture.Headers["User-Agent"]);
            Assert.AreEqual(2, fixture.Headers.Count);
        }

        [Test]
        public void DynamicHeadersShouldWork()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "FetchSomeStuffWithDynamicHeader"));
            Assert.AreEqual("id", fixture.ParameterMap[0]);
            Assert.AreEqual(0, fixture.QueryParameterMap.Count);
            Assert.IsNull(fixture.BodyParameterInfo);

            Assert.AreEqual("Authorization", fixture.HeaderParameterMap[1]);
            Assert.IsTrue(fixture.Headers.ContainsKey("User-Agent"), "Headers include User-Agent header");
            Assert.AreEqual("Refit Test Client", fixture.Headers["User-Agent"]);
            Assert.AreEqual(2, fixture.Headers.Count);
        }

        [Test]
        public void ReturningTaskShouldWork()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "VoidPost"));
            Assert.AreEqual("id", fixture.ParameterMap[0]);

            Assert.AreEqual(typeof(Task), fixture.ReturnType);
            Assert.AreEqual(typeof(void), fixture.SerializedReturnType);
        }

        [Test]
        public void SyncMethodsShouldThrow()
        {
            bool shouldDie = true;

            try {
                var input = typeof(IRestMethodInfoTests);
                var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "AsyncOnlyBuddy"));
            } catch (ArgumentException) {
                shouldDie = false;
            }

            Assert.IsFalse(shouldDie);
        }
    }

    [Headers("User-Agent: Refit Test Client", "Api-Version: 1")]
    public interface IDummyHttpApi
    {
        [Get("/foo/bar/{id}")]
        Task<string> FetchSomeStuff(int id);

        [Get("/foo/bar/{id}?baz=bamf")]
        Task<string> FetchSomeStuffWithHardcodedQueryParameter(int id);

        [Get("/foo/bar/{id}?baz=bamf")]
        Task<string> FetchSomeStuffWithHardcodedAndOtherQueryParameters(int id, [AliasAs("search_for")] string searchQuery);

        [Get("/foo/bar/{id}")]
        [Headers("Api-Version: 2")]
        Task<string> FetchSomeStuffWithHardcodedHeader(int id);

        [Get("/foo/bar/{id}")]
        [Headers("Authorization: SRSLY aHR0cDovL2kuaW1ndXIuY29tL0NGRzJaLmdpZg==")]
        Task<string> FetchSomeStuffWithDynamicHeader(int id, [Header("Authorization")] string authorization);

        string SomeOtherMethod();
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
                    var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
                    fixture.BuildRequestFactoryForMethod(v);
                } catch (Exception ex) {
                    shouldDie = false;
                }
                Assert.IsFalse(shouldDie);
            }

            foreach (var v in successMethods) {
                bool shouldDie = false;

                try {
                    var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
                    fixture.BuildRequestFactoryForMethod(v);
                } catch (Exception ex) {
                    shouldDie = true;
                }

                Assert.IsFalse(shouldDie);
            }
        }

        [Test]
        public void HardcodedQueryParamShouldBeInUrl()
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithHardcodedQueryParameter");
            var output = factory(new object[] { 6 });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);
            Assert.AreEqual("/foo/bar/6?baz=bamf", uri.PathAndQuery);
        }
                        
        [Test]
        public void ParameterizedQueryParamsShouldBeInUrl()
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithHardcodedAndOtherQueryParameters");
            var output = factory(new object[] { 6, "foo" });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);
            Assert.AreEqual("/foo/bar/6?baz=bamf&search_for=foo", uri.PathAndQuery);
        }

        [Test]
        public void HardcodedHeadersShouldBeInHeaders()
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithHardcodedHeader");
            var output = factory(new object[] { 6 });

            Assert.IsTrue(output.Headers.Contains("User-Agent"), "Headers include User-Agent header");
            Assert.AreEqual("Refit Test Client", output.Headers.UserAgent.ToString());
            Assert.IsTrue(output.Headers.Contains("Api-Version"), "Headers include Api-Version header");
            Assert.AreEqual("2", output.Headers.GetValues("Api-Version").Single());
        }

        [Test]
        public void DynamicHeaderShouldBeInHeaders()
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithDynamicHeader");
            var output = factory(new object[] { 6, "Basic RnVjayB5ZWFoOmhlYWRlcnMh" });

            Assert.IsNotNull(output.Headers.Authorization, "Headers include Authorization header");
            Assert.AreEqual("RnVjayB5ZWFoOmhlYWRlcnMh", output.Headers.Authorization.Parameter);
        }
    }
}