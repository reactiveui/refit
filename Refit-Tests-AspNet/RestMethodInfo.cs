using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using NUnit.Framework;

namespace Refit.Tests.AspNet
{
    public interface IRestMethodInfoTests
    {
        [Route("@)!@_!($_!@($\\\\|||::::")]
        Task<string> GarbagePath();

        [Route("/foo/bar/{id}")]
        Task<string> FetchSomeStuffMissingParameters();

        [Route("/foo/bar/{id}")]
        Task<string> FetchSomeStuff(int id);

        [Route("/foo/bar/{id}?baz=bamf")]
        Task<string> FetchSomeStuffWithHardcodedQueryParam(int id);

        [Route("/foo/bar/{id}?baz=bamf")]
        Task<string> FetchSomeStuffWithQueryParam(int id, string search);

        // Hrm Routes don't indicate Http Action

        //[Get("/foo/bar/{id}")]
        //Task<string> FetchSomeStuffWithAlias([AliasAs("id")] int anId);

        //[Get("/foo/bar/{id}")]
        //IObservable<string> FetchSomeStuffWithBody([AliasAs("id")] int anId, [Body] Dictionary<int, string> theData);

        //[Post("/foo/{id}")]
        //string AsyncOnlyBuddy(int id);
    }

    [TestFixture]
    public class RestMethodInfoTests
    {
        [Test]
        public void GarbagePathsShouldThrow()
        {
            bool shouldDie = true;

            try
            {
                var defaultRestMethodResolver = new Refit.AspNet.RestMethodResolver();
                var input = typeof(IRestMethodInfoTests);
                var fixture = defaultRestMethodResolver.buildRestMethodInfo(input, input.GetMethods().First(x => x.Name == "GarbagePath"));
            }
            catch (ArgumentException)
            {
                shouldDie = false;
            }

            Assert.IsFalse(shouldDie);
        }

        [Test]
        public void MissingParametersShouldBlowUp()
        {
            bool shouldDie = true;

            try
            {
                var defaultRestMethodResolver = new Refit.AspNet.RestMethodResolver();
                var input = typeof(IRestMethodInfoTests);
                var fixture = defaultRestMethodResolver.buildRestMethodInfo(input, input.GetMethods().First(x => x.Name == "FetchSomeStuffMissingParameters"));
            }
            catch (ArgumentException)
            {
                shouldDie = false;
            }

            Assert.IsFalse(shouldDie);
        }

        [Test]
        public void ParameterMappingSmokeTest()
        {
            var defaultRestMethodResolver = new Refit.AspNet.RestMethodResolver();
            var input = typeof(IRestMethodInfoTests);
            var fixture = defaultRestMethodResolver.buildRestMethodInfo(input, input.GetMethods().First(x => x.Name == "FetchSomeStuff"));
            Assert.AreEqual("id", fixture.ParameterMap[0]);
            Assert.AreEqual(0, fixture.QueryParameterMap.Count);
            Assert.IsNull(fixture.BodyParameterInfo);
        }

        [Test]
        public void ParameterMappingWithQuerySmokeTest()
        {
            var defaultRestMethodResolver = new Refit.AspNet.RestMethodResolver();
            var input = typeof(IRestMethodInfoTests);
            var fixture = defaultRestMethodResolver.buildRestMethodInfo(input, input.GetMethods().First(x => x.Name == "FetchSomeStuffWithQueryParam"));
            Assert.AreEqual("id", fixture.ParameterMap[0]);
            Assert.AreEqual("search", fixture.QueryParameterMap[1]);
            Assert.IsNull(fixture.BodyParameterInfo);
        }

        [Test]
        public void ParameterMappingWithHardcodedQuerySmokeTest()
        {
            var defaultRestMethodResolver = new Refit.AspNet.RestMethodResolver();
            var input = typeof(IRestMethodInfoTests);
            var fixture = defaultRestMethodResolver.buildRestMethodInfo(input, input.GetMethods().First(x => x.Name == "FetchSomeStuffWithHardcodedQueryParam"));
            Assert.AreEqual("id", fixture.ParameterMap[0]);
            Assert.AreEqual(0, fixture.QueryParameterMap.Count);
            Assert.IsNull(fixture.BodyParameterInfo);
        }

        [Test]
        public void AliasMappingShouldWork()
        {
            var defaultRestMethodResolver = new Refit.AspNet.RestMethodResolver();
            var input = typeof(IRestMethodInfoTests);
            var fixture = defaultRestMethodResolver.buildRestMethodInfo(input, input.GetMethods().First(x => x.Name == "FetchSomeStuffWithAlias"));
            Assert.AreEqual("id", fixture.ParameterMap[0]);
            Assert.AreEqual(0, fixture.QueryParameterMap.Count);
            Assert.IsNull(fixture.BodyParameterInfo);
        }

        [Test]
        public void FindTheBodyParameter()
        {
            var defaultRestMethodResolver = new Refit.AspNet.RestMethodResolver();
            var input = typeof(IRestMethodInfoTests);
            var fixture = defaultRestMethodResolver.buildRestMethodInfo(input, input.GetMethods().First(x => x.Name == "FetchSomeStuffWithBody"));
            Assert.AreEqual("id", fixture.ParameterMap[0]);

            Assert.IsNotNull(fixture.BodyParameterInfo);
            Assert.AreEqual(0, fixture.QueryParameterMap.Count);
            Assert.AreEqual(1, fixture.BodyParameterInfo.Item2);
        }

        [Test]
        public void SyncMethodsShouldThrow()
        {
            bool shouldDie = true;

            try
            {
                var defaultRestMethodResolver = new Refit.AspNet.RestMethodResolver();
                var input = typeof(IRestMethodInfoTests);
                var fixture = defaultRestMethodResolver.buildRestMethodInfo(input, input.GetMethods().First(x => x.Name == "AsyncOnlyBuddy"));
            }
            catch (ArgumentException)
            {
                shouldDie = false;
            }

            Assert.IsFalse(shouldDie);
        }
    }
}
