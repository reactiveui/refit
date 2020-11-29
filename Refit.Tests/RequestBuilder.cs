﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace Refit.Tests
{
    [Headers("User-Agent: RefitTestClient", "Api-Version: 1")]
    public interface IRestMethodInfoTests
    {
        [Get("@)!@_!($_!@($\\\\|||::::")]
        Task<string> GarbagePath();

        [Get("/foo/bar/{id}")]
        Task<string> FetchSomeStuffMissingParameters();

        [Get("/foo/bar/{id}")]
        Task<string> FetchSomeStuff(int id);

        [Get("/foo/bar/{**path}/{id}")]
        Task<string> FetchSomeStuffWithRoundTrippingParam(string path, int id);

        [Get("/foo/bar/{**path}/{id}")]
        Task<string> FetchSomeStuffWithNonStringRoundTrippingParam(int path, int id);

        [Get("/foo/bar/{id}?baz=bamf")]
        Task<string> FetchSomeStuffWithHardcodedQueryParam(int id);

        [Get("/foo/bar/{id}?baz=bamf")]
        Task<string> FetchSomeStuffWithQueryParam(int id, string search);

        [Get("/foo/bar/{id}")]
        Task<string> FetchSomeStuffWithAlias([AliasAs("id")] int anId);

        [Get("/foo/bar/{width}x{height}")]
        Task<string> FetchAnImage(int width, int height);

        [Get("/foo/bar/{id}")]
        IObservable<string> FetchSomeStuffWithBody([AliasAs("id")] int anId, [Body] Dictionary<int, string> theData);

        [Post("/foo/bar/{id}")]
        IObservable<string> PostSomeUrlEncodedStuff([AliasAs("id")] int anId, [Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, string> theData);

        [Get("/foo/bar/{id}")]
        IObservable<string> FetchSomeStuffWithAuthorizationSchemeSpecified([AliasAs("id")] int anId, [Authorize("Bearer")] string token);

        [Get("/foo/bar/{id}")]
        [Headers("Api-Version: 2", "Accept: application/json")]
        Task<string> FetchSomeStuffWithHardcodedHeaders(int id);

        [Get("/foo/bar/{id}")]
        Task<string> FetchSomeStuffWithDynamicHeader(int id, [Header("Authorization")] string authorization);

        [Get("/foo")]
        Task<string> FetchSomeStuffWithDynamicHeaderQueryParamAndArrayQueryParam([Header("Authorization")] string authorization, int id, [Query(CollectionFormat.Multi)] string[] someArray, [Property("SomeProperty")] object someValue);

        //header collection tests

        //get request with header collection
        [Get("/foo/bar/{id}")]
        [Headers("Authorization: SRSLY aHR0cDovL2kuaW1ndXIuY29tL0NGRzJaLmdpZg==", "Accept: application/json")]
        Task<string> FetchSomeStuffWithDynamicHeaderCollection(int id, [HeaderCollection] IDictionary<string, string> headers);

        //post request with header collection
        [Post("/foo/bar/{id}")]
        Task<string> PostSomeStuffWithCustomHeaderCollection(int id, [Body] object body, [HeaderCollection] IDictionary<string, string> headers);

        //request with method level headers, AND header collection, AND authorize?
        [Get("/foo/bar/{id}")]
        Task<string> FetchSomeStuffWithDynamicHeaderCollectionAndAuthorize(int id, [Authorize] string value, [HeaderCollection] IDictionary<string, string> headers);

        //request with method level headers, AND header collection, AND header?
        [Get("/foo/bar/{id}")]
        Task<string> FetchSomeStuffWithDynamicHeaderCollectionAndDynamicHeader(int id, [Header("Authorization")] string value, [HeaderCollection] IDictionary<string, string> headers);

        [Get("/foo/bar/{id}")]
        Task<string> FetchSomeStuffWithDynamicHeaderCollectionAndDynamicHeaderOrderFlipped(int id, [HeaderCollection] IDictionary<string, string> headers, [Header("Authorization")] string value);

        //request with method level headers, AND header, AND header collection (same as above but flip order to see overwriting headers)
        [Get("/foo/bar/{id}")]
        Task<string> FetchSomeStuffWithPathMemberInCustomHeaderAndDynamicHeaderCollection([Header("X-PathMember")] int id, [HeaderCollection] IDictionary<string, string> headers);

        //request with header collection in middle of params
        [Get("/foo/bar/{id}")]
        Task<string> FetchSomeStuffWithHeaderCollection(int id, [HeaderCollection] IDictionary<string, string> headers, int baz);

        //request with duplicate header collection
        [Get("/foo/bar")]
        Task<string> FetchSomeStuffWithDuplicateHeaderCollection([HeaderCollection] IDictionary<string, string> headers, [HeaderCollection] IDictionary<string, string> headers2);

        //request with header collection + query attr / property
        [Get("/foo")]
        Task<string> FetchSomeStuffWithHeaderCollectionQueryParamAndArrayQueryParam([HeaderCollection] IDictionary<string, string> headers, int id, [Query(CollectionFormat.Multi)] string[] someArray, [Property("SomeProperty")] object someValue);

        //request with header collection on something that doesn't support IDictionary<string, string> semantics
        [Get("/foo")]
        Task<string> FetchSomeStuffWithHeaderCollectionOfUnsupportedType([HeaderCollection] string headers);

        //request with header collection on something that supports IEnumerable<KeyValuePair<string, string>> semantics
        [Get("/foo/{bar}")]
        Task<string> FetchSomeStuffWithHeaderCollectionWithEnumerableKvpSemantics(int bar, [Query] MySimpleQueryParams query, [HeaderCollection] IEnumerable<KeyValuePair<string, string>> headers);

        //[Post("/foo")] Task PostWithBodyDetectedAndHeaderCollection(Dictionary<int, string> theData, [HeaderCollection] IDictionary<string, string> headers);
        //[Get("/foo")] Task GetWithBodyDetectedAndHeaderCollection(Dictionary<int, string> theData, [HeaderCollection] IDictionary<string, string> headers);
        //[Put("/foo")] Task PutWithBodyDetectedAndHeaderCollection(Dictionary<int, string> theData, [HeaderCollection] IDictionary<string, string> headers);
        //[Patch("/foo") Task PatchWithBodyDetectedAndHeaderCollection(Dictionary<int, string> theData, [HeaderCollection] IDictionary<string, string> headers);

        //request with header collection with custom headers
        //request with header collection with empty headers (over writing / unsetting etc)
        //request with header collection where headers are being overwritten by duplicate entries in the collection itself!
        //request with header collection that is empty or null?

        [Get("/foo/bar/{id}")]
        Task<string> FetchSomeStuffWithDynamicRequestProperty(int id, [Property("SomeProperty")] object someValue);

        [Get("/foo/bar/{id}")]
        Task<string> FetchSomeStuffWithDynamicRequestPropertyWithDuplicateKey(int id, [Property("SomeProperty")] object someValue1, [Property("SomeProperty")] object someValue2);

        [Get("/foo/bar/{id}")]
        Task<string> FetchSomeStuffWithDynamicRequestPropertyWithoutKey(int id, [Property] object someValue, [Property("")] object someOtherValue);

        [Post("/foo/{id}")]
        Task<bool> OhYeahValueTypes(int id, [Body] int whatever);

        [Post("/foo/{id}")]
        Task<bool> OhYeahValueTypesUnbuffered(int id, [Body(buffered: false)] int whatever);

        [Post("/foo/{id}")]
        Task<bool> PullStreamMethod(int id, [Body(buffered: true)] Dictionary<int, string> theData);

        [Post("/foo/{id}")]
        Task VoidPost(int id);

        [Post("/foo/{id}")]
        string AsyncOnlyBuddy(int id);

        [Patch("/foo/{id}")]
        IObservable<string> PatchSomething(int id, [Body] string someAttribute);

        [Options("/foo/{id}")]
        Task<string> SendOptions(int id, [Body] string someAttribute);

        [Post("/foo/{id}")]
        Task<ApiResponse<bool>> PostReturnsApiResponse(int id);

        [Post("/foo/{id}")]
        Task<bool> PostReturnsNonApiResponse(int id);

        [Post("/foo")]
        Task PostWithBodyDetected(Dictionary<int, string> theData);

        [Get("/foo")]
        Task GetWithBodyDetected(Dictionary<int, string> theData);

        [Put("/foo")]
        Task PutWithBodyDetected(Dictionary<int, string> theData);

        [Patch("/foo")]
        Task PatchWithBodyDetected(Dictionary<int, string> theData);

        [Post("/foo")]
        Task TooManyComplexTypes(Dictionary<int, string> theData, Dictionary<int, string> theData1);

        [Post("/foo")]
        Task ManyComplexTypes(Dictionary<int, string> theData, [Body] Dictionary<int, string> theData1);

        [Post("/foo")]
        Task PostWithDictionaryQuery([Query]Dictionary<int, string> theData);

        [Post("/foo")]
        Task PostWithComplexTypeQuery([Query]ComplexQueryObject queryParams);

        [Post("/foo")]
        Task ImpliedComplexQueryType(ComplexQueryObject queryParams, [Body] Dictionary<int, string> theData1);

        [Get("/api/{id}")]
        Task MultipleQueryAttributes(int id, [Query]string text = null, [Query]int? optionalId = null, [Query(CollectionFormat = CollectionFormat.Multi)]string[] filters = null);

        [Get("/api/{id}")]
        Task NullableValues(int id, string text = null, int? optionalId = null, [Query(CollectionFormat = CollectionFormat.Multi)]string[] filters = null);

        [Get("/api/{id}")]
        Task IEnumerableThrowingError([Query(CollectionFormat.Multi)] IEnumerable<string> values);
    }

    public enum TestEnum { A, B, C }

    public class ComplexQueryObject
    {
        [AliasAs("test-query-alias")]
        public string TestAlias1 { get; set; }

        public string TestAlias2 { get; set; }

        public IEnumerable<int> TestCollection { get; set; }

        [AliasAs("test-dictionary-alias")]
        public Dictionary<TestEnum, string> TestAliasedDictionary { get; set; }

        public Dictionary<TestEnum, string> TestDictionary { get; set; }

        [AliasAs("listOfEnumMulti")]
        [Query(CollectionFormat.Multi)]
        public List<TestEnum> EnumCollectionMulti { get; set; }

        [Query(CollectionFormat.Multi)]
        public List<object> ObjectCollectionMulti { get; set; }

        [Query(CollectionFormat.Csv)]
        public List<TestEnum> EnumCollectionCsv { get; set; }

        [AliasAs("listOfObjectsCsv")]
        [Query(CollectionFormat.Csv)]
        public List<object> ObjectCollectionCcv { get; set; }
    }

    public class RestMethodInfoTests
    {

        [Fact]
        public void TooManyComplexTypesThrows()
        {
            var input = typeof(IRestMethodInfoTests);

            Assert.Throws<ArgumentException>(() =>
            {
                var fixture = new RestMethodInfo(
                    input,
                    input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.TooManyComplexTypes)));
            });

        }

        [Fact]
        public void ManyComplexTypes()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.ManyComplexTypes)));

            Assert.Single(fixture.QueryParameterMap);
            Assert.NotNull(fixture.BodyParameterInfo);
            Assert.Equal(1, fixture.BodyParameterInfo.Item3);
        }

        [Fact]
        public void DefaultBodyParameterDetectedForPost()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.PostWithBodyDetected)));

            Assert.Empty(fixture.QueryParameterMap);
            Assert.NotNull(fixture.BodyParameterInfo);
        }

        [Fact]
        public void DefaultBodyParameterDetectedForPut()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.PutWithBodyDetected)));

            Assert.Empty(fixture.QueryParameterMap);
            Assert.NotNull(fixture.BodyParameterInfo);
        }

        [Fact]
        public void PostWithDictionaryQueryParameter()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.PostWithDictionaryQuery)));

            Assert.Single(fixture.QueryParameterMap);
            Assert.Null(fixture.BodyParameterInfo);
        }

        [Fact]
        public void PostWithObjectQueryParameterHasSingleQueryParameterValue()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixtureParams = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.PostWithComplexTypeQuery)));

            Assert.Single(fixtureParams.QueryParameterMap);
            Assert.Equal("queryParams", fixtureParams.QueryParameterMap[0]);
            Assert.Null(fixtureParams.BodyParameterInfo);
        }

        [Fact]
        public void PostWithObjectQueryParameterHasCorrectQuerystring()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();

            var factory = fixture.BuildRequestFactoryForMethod(nameof(IDummyHttpApi.PostWithComplexTypeQuery));

            var param = new ComplexQueryObject
            {
                TestAlias1 = "one",
                TestAlias2 = "two"
            };

            var output = factory(new object[] { param });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);

            Assert.Equal("/foo?test-query-alias=one&TestAlias2=two", uri.PathAndQuery);
        }

        [Fact]
        public void PostWithObjectQueryParameterWithEnumList_Multi()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();

            var factory = fixture.BuildRequestFactoryForMethod(nameof(IDummyHttpApi.PostWithComplexTypeQuery));

            var param = new ComplexQueryObject
            {
                EnumCollectionMulti = new List<TestEnum> { TestEnum.A, TestEnum.B }
            };

            var output = factory(new object[] { param });

            Assert.Equal("/foo?listOfEnumMulti=A&listOfEnumMulti=B", output.RequestUri.PathAndQuery);
        }

        [Fact]
        public void PostWithObjectQueryParameterWithObjectListWithProvidedEnumValues_Multi()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();

            var factory = fixture.BuildRequestFactoryForMethod(nameof(IDummyHttpApi.PostWithComplexTypeQuery));

            var param = new ComplexQueryObject
            {
                ObjectCollectionMulti = new List<object> { TestEnum.A, TestEnum.B }
            };

            var output = factory(new object[] { param });

            Assert.Equal("/foo?ObjectCollectionMulti=A&ObjectCollectionMulti=B", output.RequestUri.PathAndQuery);
        }

        [Fact]
        public void PostWithObjectQueryParameterWithEnumList_Csv()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();

            var factory = fixture.BuildRequestFactoryForMethod(nameof(IDummyHttpApi.PostWithComplexTypeQuery));

            var param = new ComplexQueryObject
            {
                EnumCollectionCsv = new List<TestEnum> { TestEnum.A, TestEnum.B }
            };

            var output = factory(new object[] { param });

            Assert.Equal("/foo?EnumCollectionCsv=A%2CB", output.RequestUri.PathAndQuery);
        }

        [Fact]
        public void PostWithObjectQueryParameterWithObjectListWithProvidedEnumValues_Csv()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();

            var factory = fixture.BuildRequestFactoryForMethod(nameof(IDummyHttpApi.PostWithComplexTypeQuery));

            var param = new ComplexQueryObject
            {
                ObjectCollectionCcv = new List<object> { TestEnum.A, TestEnum.B }
            };

            var output = factory(new object[] { param });

            Assert.Equal("/foo?listOfObjectsCsv=A%2CB", output.RequestUri.PathAndQuery);
        }

        [Fact]
        public void ObjectQueryParameterWithInnerCollectionHasCorrectQuerystring()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.BuildRequestFactoryForMethod(nameof(IDummyHttpApi.ComplexTypeQueryWithInnerCollection));

            var param = new ComplexQueryObject { TestCollection = new[] { 1, 2, 3 } };
            var output = factory(new object[] { param });
            var uri = new Uri(new Uri("http://api"), output.RequestUri);

            Assert.Equal("/foo?TestCollection=1%2C2%2C3", uri.PathAndQuery);
        }

        [Fact]
        public void MultipleQueryAttributesWithNulls()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixtureParams = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.MultipleQueryAttributes)));

            Assert.Equal(3, fixtureParams.QueryParameterMap.Count);
        }

        [Fact]
        public void DefaultBodyParameterDetectedForPatch()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.PatchWithBodyDetected)));

            Assert.Empty(fixture.QueryParameterMap);
            Assert.NotNull(fixture.BodyParameterInfo);
        }

        [Fact]
        public void DefaultBodyParameterNotDetectedForGet()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.GetWithBodyDetected)));

            Assert.Single(fixture.QueryParameterMap);
            Assert.Null(fixture.BodyParameterInfo);
        }

        [Fact]
        public void GarbagePathsShouldThrow()
        {
            var shouldDie = true;

            try
            {
                var input = typeof(IRestMethodInfoTests);
                var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.GarbagePath)));
            }
            catch (ArgumentException)
            {
                shouldDie = false;
            }

            Assert.False(shouldDie);
        }

        [Fact]
        public void MissingParametersShouldBlowUp()
        {
            var shouldDie = true;

            try
            {
                var input = typeof(IRestMethodInfoTests);
                var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.FetchSomeStuffMissingParameters)));
            }
            catch (ArgumentException)
            {
                shouldDie = false;
            }

            Assert.False(shouldDie);
        }

        [Fact]
        public void ParameterMappingSmokeTest()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.FetchSomeStuff)));
            Assert.Equal("id", fixture.ParameterMap[0].Name);
            Assert.Equal(ParameterType.Normal, fixture.ParameterMap[0].Type);
            Assert.Empty(fixture.QueryParameterMap);
            Assert.Null(fixture.BodyParameterInfo);
        }

        [Fact]
        public void ParameterMappingWithRoundTrippingSmokeTest()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.FetchSomeStuffWithRoundTrippingParam)));
            Assert.Equal("path", fixture.ParameterMap[0].Name);
            Assert.Equal(ParameterType.RoundTripping, fixture.ParameterMap[0].Type);
            Assert.Equal("id", fixture.ParameterMap[1].Name);
            Assert.Equal(ParameterType.Normal, fixture.ParameterMap[1].Type);
            Assert.Empty(fixture.QueryParameterMap);
            Assert.Null(fixture.BodyParameterInfo);
        }

        [Fact]
        public void ParameterMappingWithNonStringRoundTrippingShouldThrow()
        {
            var input = typeof(IRestMethodInfoTests);
            Assert.Throws<ArgumentException>(() =>
            {
                var fixture = new RestMethodInfo(
                    input,
                    input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.FetchSomeStuffWithNonStringRoundTrippingParam))
                    );
            });
        }

        [Fact]
        public void ParameterMappingWithQuerySmokeTest()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.FetchSomeStuffWithQueryParam)));
            Assert.Equal("id", fixture.ParameterMap[0].Name);
            Assert.Equal(ParameterType.Normal, fixture.ParameterMap[0].Type);
            Assert.Equal("search", fixture.QueryParameterMap[1]);
            Assert.Null(fixture.BodyParameterInfo);
        }

        [Fact]
        public void ParameterMappingWithHardcodedQuerySmokeTest()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.FetchSomeStuffWithHardcodedQueryParam)));
            Assert.Equal("id", fixture.ParameterMap[0].Name);
            Assert.Equal(ParameterType.Normal, fixture.ParameterMap[0].Type);
            Assert.Empty(fixture.QueryParameterMap);
            Assert.Null(fixture.BodyParameterInfo);
        }

        [Fact]
        public void AliasMappingShouldWork()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.FetchSomeStuffWithAlias)));
            Assert.Equal("id", fixture.ParameterMap[0].Name);
            Assert.Equal(ParameterType.Normal, fixture.ParameterMap[0].Type);
            Assert.Empty(fixture.QueryParameterMap);
            Assert.Null(fixture.BodyParameterInfo);
        }

        [Fact]
        public void MultipleParametersPerSegmentShouldWork()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.FetchAnImage)));
            Assert.Equal("width", fixture.ParameterMap[0].Name);
            Assert.Equal(ParameterType.Normal, fixture.ParameterMap[0].Type);
            Assert.Equal("height", fixture.ParameterMap[1].Name);
            Assert.Equal(ParameterType.Normal, fixture.ParameterMap[1].Type);
            Assert.Empty(fixture.QueryParameterMap);
            Assert.Null(fixture.BodyParameterInfo);
        }

        [Fact]
        public void FindTheBodyParameter()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.FetchSomeStuffWithBody)));
            Assert.Equal("id", fixture.ParameterMap[0].Name);
            Assert.Equal(ParameterType.Normal, fixture.ParameterMap[0].Type);

            Assert.NotNull(fixture.BodyParameterInfo);
            Assert.Empty(fixture.QueryParameterMap);
            Assert.Equal(1, fixture.BodyParameterInfo.Item3);
        }

        [Fact]
        public void FindTheAuthorizeParameter()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.FetchSomeStuffWithAuthorizationSchemeSpecified)));
            Assert.Equal("id", fixture.ParameterMap[0].Name);
            Assert.Equal(ParameterType.Normal, fixture.ParameterMap[0].Type);

            Assert.NotNull(fixture.AuthorizeParameterInfo);
            Assert.Empty(fixture.QueryParameterMap);
            Assert.Equal(1, fixture.AuthorizeParameterInfo.Item2);
        }

        [Fact]
        public void AllowUrlEncodedContent()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.PostSomeUrlEncodedStuff)));
            Assert.Equal("id", fixture.ParameterMap[0].Name);
            Assert.Equal(ParameterType.Normal, fixture.ParameterMap[0].Type);

            Assert.NotNull(fixture.BodyParameterInfo);
            Assert.Empty(fixture.QueryParameterMap);
            Assert.Equal(BodySerializationMethod.UrlEncoded, fixture.BodyParameterInfo.Item1);
        }

        [Fact]
        public void HardcodedHeadersShouldWork()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.FetchSomeStuffWithHardcodedHeaders)));
            Assert.Equal("id", fixture.ParameterMap[0].Name);
            Assert.Equal(ParameterType.Normal, fixture.ParameterMap[0].Type);
            Assert.Empty(fixture.QueryParameterMap);
            Assert.Null(fixture.BodyParameterInfo);

            Assert.True(fixture.Headers.ContainsKey("Api-Version"), "Headers include Api-Version header");
            Assert.Equal("2", fixture.Headers["Api-Version"]);
            Assert.True(fixture.Headers.ContainsKey("User-Agent"), "Headers include User-Agent header");
            Assert.Equal("RefitTestClient", fixture.Headers["User-Agent"]);
            Assert.True(fixture.Headers.ContainsKey("Accept"), "Headers include Accept header");
            Assert.Equal("application/json", fixture.Headers["Accept"]);
            Assert.Equal(3, fixture.Headers.Count);
        }

        [Fact]
        public void DynamicHeadersShouldWork()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.FetchSomeStuffWithDynamicHeader)));
            Assert.Equal("id", fixture.ParameterMap[0].Name);
            Assert.Equal(ParameterType.Normal, fixture.ParameterMap[0].Type);
            Assert.Empty(fixture.QueryParameterMap);
            Assert.Empty(fixture.PropertyParameterMap);
            Assert.Null(fixture.BodyParameterInfo);

            Assert.Equal("Authorization", fixture.HeaderParameterMap[1]);
            Assert.True(fixture.Headers.ContainsKey("User-Agent"), "Headers include User-Agent header");
            Assert.Equal("RefitTestClient", fixture.Headers["User-Agent"]);
            Assert.Equal(2, fixture.Headers.Count);
        }

        [Fact]
        public void DynamicHeaderCollectionShouldWork()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.FetchSomeStuffWithDynamicHeaderCollection)));
            Assert.Equal("id", fixture.ParameterMap[0].Name);
            Assert.Equal(ParameterType.Normal, fixture.ParameterMap[0].Type);
            Assert.Empty(fixture.QueryParameterMap);
            Assert.Empty(fixture.HeaderParameterMap);
            Assert.Empty(fixture.PropertyParameterMap);
            Assert.Null(fixture.BodyParameterInfo);

            Assert.True(fixture.Headers.ContainsKey("Authorization"), "Headers include Authorization header");
            Assert.Equal("SRSLY aHR0cDovL2kuaW1ndXIuY29tL0NGRzJaLmdpZg==", fixture.Headers["Authorization"]);
            Assert.True(fixture.Headers.ContainsKey("Accept"), "Headers include Accept header");
            Assert.Equal("application/json", fixture.Headers["Accept"]);
            Assert.True(fixture.Headers.ContainsKey("User-Agent"), "Headers include User-Agent header");
            Assert.Equal("RefitTestClient", fixture.Headers["User-Agent"]);
            Assert.True(fixture.Headers.ContainsKey("Api-Version"), "Headers include Api-Version header");
            Assert.Equal("1", fixture.Headers["Api-Version"]);

            Assert.Equal(4, fixture.Headers.Count);
            Assert.Equal(1, fixture.HeaderCollectionParameterMap.Count);
            Assert.True(fixture.HeaderCollectionParameterMap.Contains(1));
        }

        [Fact]
        public void DynamicHeaderCollectionShouldWorkWithBody()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.PostSomeStuffWithCustomHeaderCollection)));
            Assert.Equal("id", fixture.ParameterMap[0].Name);
            Assert.Equal(ParameterType.Normal, fixture.ParameterMap[0].Type);
            Assert.Empty(fixture.QueryParameterMap);
            Assert.Empty(fixture.HeaderParameterMap);
            Assert.Empty(fixture.PropertyParameterMap);
            Assert.NotNull(fixture.BodyParameterInfo);
            Assert.Null(fixture.AuthorizeParameterInfo);

            Assert.Equal(1, fixture.HeaderCollectionParameterMap.Count);
            Assert.True(fixture.HeaderCollectionParameterMap.Contains(2));
        }

        [Fact]
        public void DynamicHeaderCollectionShouldWorkWithAuthorize()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.FetchSomeStuffWithDynamicHeaderCollectionAndAuthorize)));
            Assert.Equal("id", fixture.ParameterMap[0].Name);
            Assert.Equal(ParameterType.Normal, fixture.ParameterMap[0].Type);
            Assert.Empty(fixture.QueryParameterMap);
            Assert.Empty(fixture.HeaderParameterMap);
            Assert.Empty(fixture.PropertyParameterMap);
            Assert.Null(fixture.BodyParameterInfo);

            Assert.NotNull(fixture.AuthorizeParameterInfo);
            Assert.Equal(1, fixture.HeaderCollectionParameterMap.Count);
            Assert.True(fixture.HeaderCollectionParameterMap.Contains(2));
        }

        [Fact]
        public void DynamicHeaderCollectionShouldWorkWithDynamicHeader()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.FetchSomeStuffWithDynamicHeaderCollectionAndDynamicHeader)));
            Assert.Equal("id", fixture.ParameterMap[0].Name);
            Assert.Equal(ParameterType.Normal, fixture.ParameterMap[0].Type);
            Assert.Empty(fixture.QueryParameterMap);
            Assert.Null(fixture.AuthorizeParameterInfo);
            Assert.Empty(fixture.PropertyParameterMap);
            Assert.Null(fixture.BodyParameterInfo);

            Assert.Single(fixture.HeaderParameterMap);
            Assert.Equal("Authorization", fixture.HeaderParameterMap[1]);
            Assert.Equal(1, fixture.HeaderCollectionParameterMap.Count);
            Assert.True(fixture.HeaderCollectionParameterMap.Contains(2));

            input = typeof(IRestMethodInfoTests);
            fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.FetchSomeStuffWithDynamicHeaderCollectionAndDynamicHeaderOrderFlipped)));
            Assert.Equal("id", fixture.ParameterMap[0].Name);
            Assert.Equal(ParameterType.Normal, fixture.ParameterMap[0].Type);
            Assert.Empty(fixture.QueryParameterMap);
            Assert.Null(fixture.AuthorizeParameterInfo);
            Assert.Empty(fixture.PropertyParameterMap);
            Assert.Null(fixture.BodyParameterInfo);

            Assert.Single(fixture.HeaderParameterMap);
            Assert.Equal("Authorization", fixture.HeaderParameterMap[2]);
            Assert.Equal(1, fixture.HeaderCollectionParameterMap.Count);
            Assert.True(fixture.HeaderCollectionParameterMap.Contains(1));
        }

        [Fact]
        public void DynamicHeaderCollectionShouldWorkWithPathMemberDynamicHeader()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.FetchSomeStuffWithPathMemberInCustomHeaderAndDynamicHeaderCollection)));
            Assert.Equal("id", fixture.ParameterMap[0].Name);
            Assert.Equal(ParameterType.Normal, fixture.ParameterMap[0].Type);
            Assert.Empty(fixture.QueryParameterMap);
            Assert.Null(fixture.AuthorizeParameterInfo);
            Assert.Empty(fixture.PropertyParameterMap);
            Assert.Null(fixture.BodyParameterInfo);

            Assert.Single(fixture.HeaderParameterMap);
            Assert.Equal("X-PathMember", fixture.HeaderParameterMap[0]);
            Assert.Equal(1, fixture.HeaderCollectionParameterMap.Count);
            Assert.True(fixture.HeaderCollectionParameterMap.Contains(1));
        }

        [Fact]
        public void DynamicHeaderCollectionInMiddleOfParamsShouldWork()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.FetchSomeStuffWithHeaderCollection)));
            Assert.Equal("id", fixture.ParameterMap[0].Name);
            Assert.Equal(ParameterType.Normal, fixture.ParameterMap[0].Type);
            Assert.Null(fixture.AuthorizeParameterInfo);
            Assert.Empty(fixture.PropertyParameterMap);
            Assert.Null(fixture.BodyParameterInfo);

            Assert.Equal("baz", fixture.QueryParameterMap[2]);
            Assert.Equal(1, fixture.HeaderCollectionParameterMap.Count);
            Assert.True(fixture.HeaderCollectionParameterMap.Contains(1));
        }

        [Fact]
        public void DynamicHeaderCollectionShouldOnlyAllowOne()
        {
            var input = typeof(IRestMethodInfoTests);

            Assert.Throws<ArgumentException>(() => new RestMethodInfo(input, input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.FetchSomeStuffWithDuplicateHeaderCollection))));
        }

        [Fact]
        public void DynamicHeaderCollectionShouldWorkWithProperty()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.FetchSomeStuffWithHeaderCollectionQueryParamAndArrayQueryParam)));
            Assert.Null(fixture.BodyParameterInfo);
            Assert.Null(fixture.AuthorizeParameterInfo);

            Assert.Equal(2, fixture.QueryParameterMap.Count);
            Assert.Equal("id", fixture.QueryParameterMap[1]);
            Assert.Equal("someArray", fixture.QueryParameterMap[2]);

            Assert.Single(fixture.PropertyParameterMap);

            Assert.Equal(1, fixture.HeaderCollectionParameterMap.Count);
            Assert.True(fixture.HeaderCollectionParameterMap.Contains(0));
        }

        [Fact]
        public void DynamicHeaderCollectionShouldOnlyWorkWithSupportedSemantics()
        {
            var input = typeof(IRestMethodInfoTests);
            Assert.Throws<ArgumentException>(() => new RestMethodInfo(input, input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.FetchSomeStuffWithHeaderCollectionOfUnsupportedType))));
        }

        [Fact]
        public void DynamicRequestPropertiesShouldWork()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.FetchSomeStuffWithDynamicRequestProperty)));
            Assert.Equal("id", fixture.ParameterMap[0].Name);
            Assert.Equal(ParameterType.Normal, fixture.ParameterMap[0].Type);
            Assert.Empty(fixture.QueryParameterMap);
            Assert.Empty(fixture.HeaderParameterMap);
            Assert.Null(fixture.BodyParameterInfo);

            Assert.Equal("SomeProperty", fixture.PropertyParameterMap[1]);
        }

        [Fact]
        public void DynamicRequestPropertiesWithoutKeysShouldDefaultKeyToParameterName()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.FetchSomeStuffWithDynamicRequestPropertyWithoutKey)));
            Assert.Equal("id", fixture.ParameterMap[0].Name);
            Assert.Equal(ParameterType.Normal, fixture.ParameterMap[0].Type);
            Assert.Empty(fixture.QueryParameterMap);
            Assert.Empty(fixture.HeaderParameterMap);
            Assert.Null(fixture.BodyParameterInfo);

            Assert.Equal("someValue", fixture.PropertyParameterMap[1]);
            Assert.Equal("someOtherValue", fixture.PropertyParameterMap[2]);
        }

        [Fact]
        public void DynamicRequestPropertiesWithDuplicateKeysDontBlowUp()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.FetchSomeStuffWithDynamicRequestPropertyWithDuplicateKey)));
            Assert.Equal("id", fixture.ParameterMap[0].Name);
            Assert.Equal(ParameterType.Normal, fixture.ParameterMap[0].Type);
            Assert.Empty(fixture.QueryParameterMap);
            Assert.Empty(fixture.HeaderParameterMap);
            Assert.Null(fixture.BodyParameterInfo);

            Assert.Equal("SomeProperty", fixture.PropertyParameterMap[1]);
            Assert.Equal("SomeProperty", fixture.PropertyParameterMap[2]);
        }

        [Fact]
        public void ValueTypesDontBlowUpBuffered()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.OhYeahValueTypes)));
            Assert.Equal("id", fixture.ParameterMap[0].Name);
            Assert.Equal(ParameterType.Normal, fixture.ParameterMap[0].Type);
            Assert.Empty(fixture.QueryParameterMap);
            Assert.Equal(BodySerializationMethod.Default, fixture.BodyParameterInfo.Item1);
            Assert.True(fixture.BodyParameterInfo.Item2); // buffered default
            Assert.Equal(1, fixture.BodyParameterInfo.Item3);

            Assert.Equal(typeof(bool), fixture.ReturnResultType);
        }

        [Fact]
        public void ValueTypesDontBlowUpUnBuffered()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.OhYeahValueTypesUnbuffered)));
            Assert.Equal("id", fixture.ParameterMap[0].Name);
            Assert.Equal(ParameterType.Normal, fixture.ParameterMap[0].Type);
            Assert.Empty(fixture.QueryParameterMap);
            Assert.Equal(BodySerializationMethod.Default, fixture.BodyParameterInfo.Item1);
            Assert.False(fixture.BodyParameterInfo.Item2); // unbuffered specified
            Assert.Equal(1, fixture.BodyParameterInfo.Item3);

            Assert.Equal(typeof(bool), fixture.ReturnResultType);
        }

        [Fact]
        public void StreamMethodPullWorks()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.PullStreamMethod)));
            Assert.Equal("id", fixture.ParameterMap[0].Name);
            Assert.Equal(ParameterType.Normal, fixture.ParameterMap[0].Type);
            Assert.Empty(fixture.QueryParameterMap);
            Assert.Equal(BodySerializationMethod.Default, fixture.BodyParameterInfo.Item1);
            Assert.True(fixture.BodyParameterInfo.Item2);
            Assert.Equal(1, fixture.BodyParameterInfo.Item3);

            Assert.Equal(typeof(bool), fixture.ReturnResultType);
        }

        [Fact]
        public void ReturningTaskShouldWork()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.VoidPost)));
            Assert.Equal("id", fixture.ParameterMap[0].Name);
            Assert.Equal(ParameterType.Normal, fixture.ParameterMap[0].Type);

            Assert.Equal(typeof(Task), fixture.ReturnType);
            Assert.Equal(typeof(void), fixture.ReturnResultType);
        }

        [Fact]
        public void SyncMethodsShouldThrow()
        {
            var shouldDie = true;

            try
            {
                var input = typeof(IRestMethodInfoTests);
                var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.AsyncOnlyBuddy)));
            }
            catch (ArgumentException)
            {
                shouldDie = false;
            }

            Assert.False(shouldDie);
        }

        [Fact]
        public void UsingThePatchAttributeSetsTheCorrectMethod()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.PatchSomething)));

            Assert.Equal("PATCH", fixture.HttpMethod.Method);
        }

        [Fact]
        public void UsingOptionsAttribute()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == nameof(IDummyHttpApi.SendOptions)));

            Assert.Equal("OPTIONS", fixture.HttpMethod.Method);
        }

        [Fact]
        public void ApiResponseShouldBeSet()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.PostReturnsApiResponse)));

            Assert.True(fixture.IsApiResponse);
        }

        [Fact]
        public void ApiResponseShouldNotBeSet()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.PostReturnsNonApiResponse)));

            Assert.False(fixture.IsApiResponse);
        }

        [Fact]
        public void ParameterMappingWithHeaderQueryParamAndQueryArrayParam()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.FetchSomeStuffWithDynamicHeaderQueryParamAndArrayQueryParam)));

            Assert.Equal("GET", fixture.HttpMethod.Method);
            Assert.Equal(2, fixture.QueryParameterMap.Count);
            Assert.Single(fixture.HeaderParameterMap);
            Assert.Single(fixture.PropertyParameterMap);
        }
    }

    [Headers("User-Agent: RefitTestClient", "Api-Version: 1")]
    public interface IDummyHttpApi
    {
        [Get("/foo/bar/{id}")]
        Task<ApiResponse<string>> FetchSomeStringWithMetadata(int id);

        [Get("/foo/bar/{id}")]
        Task<string> FetchSomeStuff(int id);

        [Get("/foo/bar/{**path}/{id}")]
        Task<string> FetchSomeStuffWithRoundTrippingParam(string path, int id);

        [Get("/foo/bar/{id}?baz=bamf")]
        Task<string> FetchSomeStuffWithHardcodedQueryParameter(int id);

        [Get("/foo/bar/{id}?baz=bamf")]
        Task<string> FetchSomeStuffWithHardcodedAndOtherQueryParameters(int id, [AliasAs("search_for")] string searchQuery);

        [Get("/{id}/{width}x{height}/foo")]
        Task<string> FetchSomethingWithMultipleParametersPerSegment(int id, int width, int height);

        [Get("/foo/bar/{id}")]
        [Headers("Api-Version: 2", "Accept: application/json")]
        Task<string> FetchSomeStuffWithHardcodedHeaders(int id);

        [Get("/foo/bar/{id}")]
        [Headers("Api-Version")]
        Task<string> FetchSomeStuffWithNullHardcodedHeader(int id);

        [Get("/foo/bar/{id}")]
        [Headers("Api-Version: ")]
        Task<string> FetchSomeStuffWithEmptyHardcodedHeader(int id);

        [Post("/foo/bar/{id}")]
        [Headers("Content-Type: literally/anything")]
        Task<string> PostSomeStuffWithHardCodedContentTypeHeader(int id, [Body] string content);

        [Get("/foo/bar/{id}")]
        [Headers("Authorization: SRSLY aHR0cDovL2kuaW1ndXIuY29tL0NGRzJaLmdpZg==")]
        Task<string> FetchSomeStuffWithDynamicHeader(int id, [Header("Authorization")] string authorization);

        [Get("/foo/bar/{id}")]
        Task<string> FetchSomeStuffWithCustomHeader(int id, [Header("X-Emoji")] string custom);

        [Get("/foo/bar/{id}")]
        Task<string> FetchSomeStuffWithPathMemberInCustomHeader([Header("X-PathMember")]int id, [Header("X-Emoji")] string custom);

        [Post("/foo/bar/{id}")]
        Task<string> PostSomeStuffWithCustomHeader(int id, [Body] object body, [Header("X-Emoji")] string emoji);

        //get request with header collection
        [Get("/foo/bar/{id}")]
        [Headers("Authorization: SRSLY aHR0cDovL2kuaW1ndXIuY29tL0NGRzJaLmdpZg==", "Accept: application/json")]
        Task<string> FetchSomeStuffWithDynamicHeaderCollection(int id, [HeaderCollection] IDictionary<string, string> headers);

        //post request with header collection
        [Post("/foo/bar/{id}")]
        Task<string> PostSomeStuffWithCustomHeaderCollection(int id, [Body] object body, [HeaderCollection] IDictionary<string, string> headers);

        //request with method level headers, AND header collection
        //request with method level headers, AND header collection, AND header?

        //request with method level headers, AND header, AND header collection (same as above but flip order to see overwriting headers)
        [Get("/foo/bar/{id}")]
        Task<string> FetchSomeStuffWithPathMemberInCustomHeaderAndDynamicHeaderCollection([Header("X-PathMember")] int id, [HeaderCollection] IDictionary<string, string> headers);

        //request with header collection at start of params
        //request with header collection in middle of params
        //request with header collection + query attr / multipart
        //request with header collection with custom headers
        //request with header collection with empty headers (over writing / unsetting etc)
        //request with header collection where headers are being overwritten by duplicate entries in the collection itself!
        //request with header collection that is empty or null?
        //request with header collection on something that doesn't support IEnumerable<KeyValuePair<string, string>> semantics?? bonus points for semantics beyond just IDictionary<string, string> but actually supporting multiple header values

        [Get("/foo/bar/{id}")]
        Task<string> FetchSomeStuffWithDynamicRequestProperty(int id, [Property("SomeProperty")] object someProperty);

        [Get("/foo/bar/{id}")]
        Task<string> FetchSomeStuffWithDynamicRequestPropertyWithDuplicateKey(int id, [Property("SomeProperty")] object someValue1, [Property("SomeProperty")] object someValue2);

        [Get("/foo/bar/{id}")]
        Task<string> FetchSomeStuffWithDynamicRequestPropertyWithoutKey(int id, [Property] object someValue, [Property("")] object someOtherValue);

        [Get("/string")]
        Task<string> FetchSomeStuffWithoutFullPath();

        [Get("/void")]
        Task FetchSomeStuffWithVoid();

        [Get("/void/{id}/path")]
        Task FetchSomeStuffWithVoidAndQueryAlias(string id, [AliasAs("a")] string valueA, [AliasAs("b")] string valueB);

        [Get("/foo")]
        Task FetchSomeStuffWithNonFormattableQueryParams(bool b, char c);

        [Post("/foo/bar/{id}")]
        Task<string> PostSomeUrlEncodedStuff(int id, [Body(BodySerializationMethod.UrlEncoded)] object content);

        [Post("/foo/bar/{id}")]
        Task<string> PostSomeAliasedUrlEncodedStuff(int id, [Body(BodySerializationMethod.UrlEncoded)] SomeRequestData content);

        string SomeOtherMethod();

        [Put("/foo/bar/{id}")]
        Task PutSomeContentWithAuthorization(int id, [Body] object content, [Header("Authorization")] string authorization);

        [Put("/foo/bar/{id}")]
        Task<string> PutSomeStuffWithDynamicContentType(int id, [Body] string content, [Header("Content-Type")] string contentType);

        [Post("/foo/bar/{id}")]
        Task<bool> PostAValueType(int id, [Body] Guid? content);

        [Patch("/foo/bar/{id}")]
        IObservable<string> PatchSomething(int id, [Body] string someAttribute);

        [Options("/foo/bar/{id}")]
        Task<string> SendOptions(int id, [Body] string someAttribute);

        [Get("/foo/bar/{id}")]
        Task<string> FetchSomeStuffWithQueryFormat([Query(Format = "0.0")] int id);

        [Get("/query")]
        Task QueryWithEnumerable(IEnumerable<int> numbers);


        [Get("/query")]
        Task QueryWithArray(int[] numbers);

        [Get("/query")]
        Task QueryWithArrayFormattedAsMulti([Query(CollectionFormat.Multi)]int[] numbers);

        [Get("/query")]
        Task QueryWithArrayFormattedAsCsv([Query(CollectionFormat.Csv)]int[] numbers);

        [Get("/query")]
        Task QueryWithArrayFormattedAsSsv([Query(CollectionFormat.Ssv)]int[] numbers);

        [Get("/query")]
        Task QueryWithArrayFormattedAsTsv([Query(CollectionFormat.Tsv)]int[] numbers);

        [Get("/query")]
        Task QueryWithArrayFormattedAsPipes([Query(CollectionFormat.Pipes)]int[] numbers);

        [Get("/foo")]
        Task ComplexQueryObjectWithDictionary([Query] ComplexQueryObject query);

        [Get("/foo")]
        Task QueryWithDictionaryWithEnumKey([Query] IDictionary<TestEnum, string> query);

        [Get("/foo")]
        Task QueryWithDictionaryWithPrefix([Query(".", "dictionary")] IDictionary<TestEnum, string> query);

        [Get("/foo")]
        Task QueryWithDictionaryWithNumericKey([Query] IDictionary<int, string> query);

        [Get("/query")]
        Task QueryWithEnumerableFormattedAsMulti([Query(CollectionFormat.Multi)]IEnumerable<string> lines);

        [Get("/query")]
        Task QueryWithEnumerableFormattedAsCsv([Query(CollectionFormat.Csv)]IEnumerable<string> lines);

        [Get("/query")]
        Task QueryWithEnumerableFormattedAsSsv([Query(CollectionFormat.Ssv)]IEnumerable<string> lines);

        [Get("/query")]
        Task QueryWithEnumerableFormattedAsTsv([Query(CollectionFormat.Tsv)]IEnumerable<string> lines);

        [Get("/query")]
        Task QueryWithEnumerableFormattedAsPipes([Query(CollectionFormat.Pipes)]IEnumerable<string> lines);

        [Get("/query")]
        Task QueryWithObjectWithPrivateGetters(Person person);

        [Multipart]
        [Post("/foo?&name={name}")]
        Task<HttpResponseMessage> PostWithQueryStringParameters(FileInfo source, string name);

        [Get("/query")]
        Task QueryWithEnum(FooWithEnumMember foo);

        [Get("/query")]
        Task QueryWithTypeWithEnum(TypeFooWithEnumMember foo);

        [Get("/api/{id}")]
        Task QueryWithOptionalParameters(int id, [Query]string text = null, [Query]int? optionalId = null, [Query(CollectionFormat = CollectionFormat.Multi)]string[] filters = null);

        [Delete("/api/bar")]
        Task ClearWithEnumMember([Query] FooWithEnumMember foo);

        [Delete("/api/v1/video")]
        Task Clear([Query] int playerIndex);

        [Multipart]
        [Post("/blobstorage/{**filepath}")]
        Task Blob_Post_Byte(string filepath, [AliasAs("attachment")] ByteArrayPart byteArray);

        [Multipart]
        [Post("/companies/{companyId}/{path}")]
        Task<ApiResponse<object>> UploadFile(int companyId,
                                             string path,
                                             [AliasAs("file")] StreamPart stream,
                                             [Header("Authorization")] string authorization,
                                             bool overwrite = false,
                                             [AliasAs("fileMetadata")] string metadata = null);


        [Post("/foo")]
        Task PostWithComplexTypeQuery([Query]ComplexQueryObject queryParams);

        [Get("/foo")]
        Task ComplexTypeQueryWithInnerCollection([Query]ComplexQueryObject queryParams);

        [Get("/api/{obj.someProperty}")]
        Task QueryWithOptionalParametersPathBoundObject(PathBoundObject obj, [Query]string text = null, [Query]int? optionalId = null, [Query(CollectionFormat = CollectionFormat.Multi)]string[] filters = null);

        [Headers("Accept:application/json", "X-API-V: 125")]
        [Get("/api/someModule/deviceList?controlId={control_id}")]
        Task QueryWithHeadersBeforeData([Header("Authorization")] string authorization, [Header("X-Lng")] string twoLetterLang, string search, [AliasAs("control_id")] string controlId, string secret);

        [Get("/query")]
        [QueryUriFormat(UriFormat.Unescaped)]
        Task UnescapedQueryParams(string q);

        [Get("/query")]
        [QueryUriFormat(UriFormat.Unescaped)]
        Task UnescapedQueryParamsWithFilter(string q, string filter);
    }

    interface ICancellableMethods
    {
        [Get("/foo")]
        Task GetWithCancellation(CancellationToken token = default);
        [Get("/foo")]
        Task<string> GetWithCancellationAndReturn(CancellationToken token = default);
    }


    public enum FooWithEnumMember
    {
        A,

        [EnumMember(Value = "b")]
        B
    }

    public class TypeFooWithEnumMember
    {
        [AliasAs("foo")]
        public FooWithEnumMember Foo { get; set; }
    }

    public class SomeRequestData
    {
        [AliasAs("rpn")]
        public int ReadablePropertyName { get; set; }
    }

    public class Person
    {
        public string FirstName { private get; set; }
        public string LastName { private get; set; }
        public string FullName => $"{FirstName} {LastName}";
    }

    public class TestHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage RequestMessage { get; private set; }
        public int MessagesSent { get; set; }
        public HttpContent Content { get; set; }
        public Func<HttpContent> ContentFactory { get; set; }
        public CancellationToken CancellationToken { get; set; }
        public string SendContent { get; set; }

        public TestHttpMessageHandler(string content = "test")
        {
            Content = new StringContent(content);
            ContentFactory = () => Content;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestMessage = request;
            if (request.Content != null)
            {
                SendContent = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
            }

            CancellationToken = cancellationToken;
            MessagesSent++;

            return new HttpResponseMessage(HttpStatusCode.OK) { Content = ContentFactory() };
        }
    }

    public class TestUrlParameterFormatter : IUrlParameterFormatter
    {
        readonly string constantParameterOutput;

        public TestUrlParameterFormatter(string constantOutput)
        {
            constantParameterOutput = constantOutput;
        }

        public string Format(object value, ICustomAttributeProvider attributeProvider, Type type)
        {
            return constantParameterOutput;
        }
    }

    // Converts enums to ints and adds a suffix to strings to test that both dictionary keys and values are formatted.
    public class TestEnumUrlParameterFormatter : DefaultUrlParameterFormatter
    {
        public override string Format(object parameterValue, ICustomAttributeProvider attributeProvider, Type type)
        {
            if (parameterValue is TestEnum enumValue)
            {
                var enumBackingValue = (int)enumValue;
                return enumBackingValue.ToString();
            }

            if (parameterValue is string stringValue)
            {
                return $"{stringValue}{StringParameterSuffix}";
            }

            return base.Format(parameterValue, attributeProvider, type);
        }

        public string StringParameterSuffix => "suffix";
    }

    public class TestEnumerableUrlParameterFormatter : DefaultUrlParameterFormatter
    {
        public override string Format(object parameterValue, ICustomAttributeProvider attributeProvider, Type type)
        {
            if (parameterValue is IEnumerable<object> enu)
            {
                return string.Join(",", enu.Select(o => base.Format(o, attributeProvider, type)));
            }
            if (parameterValue is IEnumerable en)
            {
                return string.Join(",", en.Cast<object>().Select(o => base.Format(o, attributeProvider, type)));
            }

            return base.Format(parameterValue, attributeProvider, type);
        }
    }

    public class RequestBuilderTests
    {

        [Fact]
        public void MethodsShouldBeCancellableDefault()
        {
            var fixture = new RequestBuilderImplementation<ICancellableMethods>();
            var factory = fixture.RunRequest("GetWithCancellation");
            var output = factory(new object[0]);

            var uri = new Uri(new Uri("http://api"), output.RequestMessage.RequestUri);
            Assert.Equal("/foo", uri.PathAndQuery);
            Assert.False(output.CancellationToken.IsCancellationRequested);
        }

        [Fact]
        public void MethodsShouldBeCancellableWithToken()
        {
            var fixture = new RequestBuilderImplementation<ICancellableMethods>();
            var factory = fixture.RunRequest("GetWithCancellation");

            var cts = new CancellationTokenSource();

            var output = factory(new object[] { cts.Token });

            var uri = new Uri(new Uri("http://api"), output.RequestMessage.RequestUri);
            Assert.Equal("/foo", uri.PathAndQuery);
            Assert.False(output.CancellationToken.IsCancellationRequested);
        }

        [Fact]
        public void MethodsShouldBeCancellableWithTokenDoesCancel()
        {
            var fixture = new RequestBuilderImplementation<ICancellableMethods>();
            var factory = fixture.RunRequest("GetWithCancellation");

            var cts = new CancellationTokenSource();
            cts.Cancel();

            var output = factory(new object[] { cts.Token });
            Assert.True(output.CancellationToken.IsCancellationRequested);
        }

        [Fact]
        public void HttpContentAsApiResponseTest()
        {
            var fixture = new RequestBuilderImplementation<IHttpContentApi>();
            var factory = fixture.BuildRestResultFuncForMethod("PostFileUploadWithMetadata");
            var testHttpMessageHandler = new TestHttpMessageHandler();
            var retContent = new StreamContent(new MemoryStream());
            testHttpMessageHandler.Content = retContent;

            var mpc = new MultipartContent("foosubtype");

            var task = (Task<ApiResponse<HttpContent>>)factory(new HttpClient(testHttpMessageHandler) { BaseAddress = new Uri("http://api/") }, new object[] { mpc });
            task.Wait();

            Assert.NotNull(task.Result.Headers);
            Assert.True(task.Result.IsSuccessStatusCode);
            Assert.NotNull(task.Result.ReasonPhrase);
            Assert.False(task.Result.StatusCode == default);
            Assert.NotNull(task.Result.Version);

            Assert.Equal(testHttpMessageHandler.RequestMessage.Content, mpc);
            Assert.Equal(retContent, task.Result.Content);
        }

        [Fact]
        public void HttpContentTest()
        {
            var fixture = new RequestBuilderImplementation<IHttpContentApi>();
            var factory = fixture.BuildRestResultFuncForMethod("PostFileUpload");
            var testHttpMessageHandler = new TestHttpMessageHandler();
            var retContent = new StreamContent(new MemoryStream());
            testHttpMessageHandler.Content = retContent;

            var mpc = new MultipartContent("foosubtype");

            var task = (Task<HttpContent>)factory(new HttpClient(testHttpMessageHandler) { BaseAddress = new Uri("http://api/") }, new object[] { mpc });
            task.Wait();

            Assert.Equal(testHttpMessageHandler.RequestMessage.Content, mpc);
            Assert.Equal(retContent, task.Result);
        }

        [Fact]
        public void StreamResponseAsApiResponseTest()
        {
            var fixture = new RequestBuilderImplementation<IStreamApi>();
            var factory = fixture.BuildRestResultFuncForMethod("GetRemoteFileWithMetadata");
            var testHttpMessageHandler = new TestHttpMessageHandler();
            var streamResponse = new MemoryStream();
            var reponseContent = "A remote file";
            testHttpMessageHandler.Content = new StreamContent(streamResponse);

            var writer = new StreamWriter(streamResponse);
            writer.Write(reponseContent);
            writer.Flush();
            streamResponse.Seek(0L, SeekOrigin.Begin);

            var task = (Task<ApiResponse<Stream>>)factory(new HttpClient(testHttpMessageHandler) { BaseAddress = new Uri("http://api/") }, new object[] { "test-file" });
            task.Wait();

            Assert.NotNull(task.Result.Headers);
            Assert.True(task.Result.IsSuccessStatusCode);
            Assert.NotNull(task.Result.ReasonPhrase);
            Assert.False(task.Result.StatusCode == default);
            Assert.NotNull(task.Result.Version);

            using var reader = new StreamReader(task.Result.Content);
            Assert.Equal(reponseContent, reader.ReadToEnd());
        }

        [Fact]
        public void StreamResponseTest()
        {
            var fixture = new RequestBuilderImplementation<IStreamApi>();
            var factory = fixture.BuildRestResultFuncForMethod("GetRemoteFile");
            var testHttpMessageHandler = new TestHttpMessageHandler();
            var streamResponse = new MemoryStream();
            var reponseContent = "A remote file";
            testHttpMessageHandler.Content = new StreamContent(streamResponse);

            var writer = new StreamWriter(streamResponse);
            writer.Write(reponseContent);
            writer.Flush();
            streamResponse.Seek(0L, SeekOrigin.Begin);

            var task = (Task<Stream>)factory(new HttpClient(testHttpMessageHandler) { BaseAddress = new Uri("http://api/") }, new object[] { "test-file" });
            task.Wait();

            using var reader = new StreamReader(task.Result);
            Assert.Equal(reponseContent, reader.ReadToEnd());
        }

        [Fact]
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

            foreach (var v in failureMethods)
            {
                var shouldDie = true;

                try
                {
                    var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
                    fixture.BuildRequestFactoryForMethod(v);
                }
                catch (Exception)
                {
                    shouldDie = false;
                }
                Assert.False(shouldDie);
            }

            foreach (var v in successMethods)
            {
                var shouldDie = false;

                try
                {
                    var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
                    fixture.BuildRequestFactoryForMethod(v);
                }
                catch (Exception)
                {
                    shouldDie = true;
                }

                Assert.False(shouldDie);
            }
        }

        [Fact]
        public void HardcodedQueryParamShouldBeInUrl()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithHardcodedQueryParameter");
            var output = factory(new object[] { 6 });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);
            Assert.Equal("/foo/bar/6?baz=bamf", uri.PathAndQuery);
        }

        [Fact]
        public void ParameterizedQueryParamsShouldBeInUrl()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithHardcodedAndOtherQueryParameters");
            var output = factory(new object[] { 6, "foo" });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);
            Assert.Equal("/foo/bar/6?baz=bamf&search_for=foo", uri.PathAndQuery);
        }

        [Theory]
        [InlineData("aaa/bbb", "/foo/bar/aaa/bbb/1")]
        [InlineData("aaa/bbb/ccc", "/foo/bar/aaa/bbb/ccc/1")]
        [InlineData("aaa", "/foo/bar/aaa/1")]
        [InlineData("aa a/bb-b", "/foo/bar/aa%20a/bb-b/1")]
        public void RoundTrippingParameterizedQueryParamsShouldBeInUrl(string path, string expectedQuery)
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();

            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithRoundTrippingParam");
            var output = factory(new object[] { path, 1 });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);
            Assert.Equal(expectedQuery, uri.PathAndQuery);
        }

        [Fact]
        public void ParameterizedNullQueryParamsShouldBeBlankInUrl()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.BuildRequestFactoryForMethod("PostWithQueryStringParameters");
            var output = factory(new object[] { new FileInfo(typeof(RequestBuilderTests).Assembly.Location), null });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);
            Assert.Equal("/foo?name=", uri.PathAndQuery);
        }

        [Fact]
        public void QueryParamShouldFormat()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithQueryFormat");
            var output = factory(new object[] { 6 });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);
            Assert.Equal("/foo/bar/6.0", uri.PathAndQuery);
        }

        [Fact]
        public void ParameterizedQueryParamsShouldBeInUrlAndValuesEncoded()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithHardcodedAndOtherQueryParameters");
            var output = factory(new object[] { 6, "push!=pull&push" });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);

            Assert.Equal("/foo/bar/6?baz=bamf&search_for=push%21%3Dpull%26push", uri.PathAndQuery);
        }

        [Fact]
        public void ParameterizedQueryParamsShouldBeInUrlAndValuesEncodedWhenMixedReplacementAndQuery()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithVoidAndQueryAlias");
            var output = factory(new object[] { "6 & 7/8", "test@example.com", "push!=pull" });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);

            Assert.Equal("/void/6%20%26%207%2F8/path?a=test%40example.com&b=push%21%3Dpull", uri.PathAndQuery);
        }

        [Fact]
        public void QueryParamWithPathDelimiterShouldBeEncoded()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithVoidAndQueryAlias");
            var output = factory(new object[] { "6/6", "test@example.com", "push!=pull" });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);

            Assert.Equal("/void/6%2F6/path?a=test%40example.com&b=push%21%3Dpull", uri.PathAndQuery);
        }

        [Fact]
        public void ParameterizedQueryParamsShouldBeInUrlAndValuesEncodedWhenMixedReplacementAndQueryBadId()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithVoidAndQueryAlias");
            var output = factory(new object[] { "6", "test@example.com", "push!=pull" });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);

            Assert.Equal("/void/6/path?a=test%40example.com&b=push%21%3Dpull", uri.PathAndQuery);
        }

        [Fact]
        public void NonFormattableQueryParamsShouldBeIncluded()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithNonFormattableQueryParams");
            var output = factory(new object[] { true, 'x' });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);

            Assert.Equal("/foo?b=True&c=x", uri.PathAndQuery);
        }

        [Fact]
        public void MultipleParametersInTheSameSegmentAreGeneratedProperly()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.BuildRequestFactoryForMethod("FetchSomethingWithMultipleParametersPerSegment");
            var output = factory(new object[] { 6, 1024, 768 });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);
            Assert.Equal("/6/1024x768/foo", uri.PathAndQuery);
        }

        [Fact]
        public void HardcodedHeadersShouldBeInHeaders()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();

            var factory = fixture.BuildRequestFactoryForMethod(nameof(IDummyHttpApi.FetchSomeStuffWithHardcodedHeaders));
            var output = factory(new object[] { 6 });

            Assert.True(output.Headers.Contains("User-Agent"), "Headers include User-Agent header");
            Assert.Equal("RefitTestClient", output.Headers.UserAgent.ToString());
            Assert.True(output.Headers.Contains("Api-Version"), "Headers include Api-Version header");
            Assert.Equal("2", output.Headers.GetValues("Api-Version").Single());
            Assert.True(output.Headers.Contains("Accept"), "Headers include Accept header");
            Assert.Equal("application/json", output.Headers.Accept.ToString());
        }

        [Fact]
        public void EmptyHardcodedHeadersShouldBeInHeaders()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithEmptyHardcodedHeader");
            var output = factory(new object[] { 6 });

            Assert.True(output.Headers.Contains("User-Agent"), "Headers include User-Agent header");
            Assert.Equal("RefitTestClient", output.Headers.UserAgent.ToString());
            Assert.True(output.Headers.Contains("Api-Version"), "Headers include Api-Version header");
            Assert.Equal("", output.Headers.GetValues("Api-Version").Single());
        }
        [Fact]
        public void NullHardcodedHeadersShouldNotBeInHeaders()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithNullHardcodedHeader");
            var output = factory(new object[] { 6 });

            Assert.True(output.Headers.Contains("User-Agent"), "Headers include User-Agent header");
            Assert.Equal("RefitTestClient", output.Headers.UserAgent.ToString());
            Assert.False(output.Headers.Contains("Api-Version"), "Headers include Api-Version header");
        }

        [Fact]
        public void ReadStringContentWithMetadata()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.BuildRestResultFuncForMethod("FetchSomeStringWithMetadata");
            var testHttpMessageHandler = new TestHttpMessageHandler();

            var task = (Task<ApiResponse<string>>)factory(new HttpClient(testHttpMessageHandler) { BaseAddress = new Uri("http://api/") }, new object[] { 42 });
            task.Wait();

            Assert.NotNull(task.Result.Headers);
            Assert.True(task.Result.IsSuccessStatusCode);
            Assert.NotNull(task.Result.ReasonPhrase);
            Assert.False(task.Result.StatusCode == default);
            Assert.NotNull(task.Result.Version);

            Assert.Equal("test", task.Result.Content);
        }

        [Fact]
        public void ContentHeadersCanBeHardcoded()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.BuildRequestFactoryForMethod("PostSomeStuffWithHardCodedContentTypeHeader");
            var output = factory(new object[] { 6, "stuff" });

            Assert.True(output.Content.Headers.Contains("Content-Type"), "Content headers include Content-Type header");
            Assert.Equal("literally/anything", output.Content.Headers.ContentType.ToString());
        }

        [Fact]
        public void DynamicHeaderShouldBeInHeaders()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithDynamicHeader");
            var output = factory(new object[] { 6, "Basic RnVjayB5ZWFoOmhlYWRlcnMh" });

            Assert.NotNull(output.Headers.Authorization);//, "Headers include Authorization header");
            Assert.Equal("RnVjayB5ZWFoOmhlYWRlcnMh", output.Headers.Authorization.Parameter);
        }

        [Fact]
        public void CustomDynamicHeaderShouldBeInHeaders()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithCustomHeader");
            var output = factory(new object[] { 6, ":joy_cat:" });

            Assert.True(output.Headers.Contains("X-Emoji"), "Headers include X-Emoji header");
            Assert.Equal(":joy_cat:", output.Headers.GetValues("X-Emoji").First());
        }

        [Fact]
        public void EmptyDynamicHeaderShouldBeInHeaders()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithCustomHeader");
            var output = factory(new object[] { 6, "" });

            Assert.True(output.Headers.Contains("X-Emoji"), "Headers include X-Emoji header");
            Assert.Equal("", output.Headers.GetValues("X-Emoji").First());
        }

        [Fact]
        public void NullDynamicHeaderShouldNotBeInHeaders()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithDynamicHeader");
            var output = factory(new object[] { 6, null });

            Assert.Null(output.Headers.Authorization);//, "Headers include Authorization header");
        }

        [Fact]
        public void PathMemberAsCustomDynamicHeaderShouldBeInHeaders()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithPathMemberInCustomHeader");
            var output = factory(new object[] { 6, ":joy_cat:" });

            Assert.True(output.Headers.Contains("X-PathMember"), "Headers include X-PathMember header");
            Assert.Equal("6", output.Headers.GetValues("X-PathMember").First());
        }

        [Fact]
        public void AddCustomHeadersToRequestHeadersOnly()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.BuildRequestFactoryForMethod("PostSomeStuffWithCustomHeader");
            var output = factory(new object[] { 6, new { Foo = "bar" }, ":smile_cat:" });

            Assert.True(output.Headers.Contains("Api-Version"), "Headers include Api-Version header");
            Assert.True(output.Headers.Contains("X-Emoji"), "Headers include X-Emoji header");
            Assert.False(output.Content.Headers.Contains("Api-Version"), "Content headers include Api-Version header");
            Assert.False(output.Content.Headers.Contains("X-Emoji"), "Content headers include X-Emoji header");
        }

        [Fact]
        public void DynamicRequestPropertiesShouldBeInProperties()
        {
            var someProperty = new object();
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.BuildRequestFactoryForMethod(nameof(IDummyHttpApi.FetchSomeStuffWithDynamicRequestProperty));
            var output = factory(new object[] { 6, someProperty });

            Assert.NotEmpty(output.Properties);
            Assert.Equal(someProperty, output.Properties["SomeProperty"]);
        }

        [Fact]
        public void DynamicRequestPropertiesWithDefaultKeysShouldBeInProperties()
        {
            var someProperty = new object();
            var someOtherProperty = new object();
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.BuildRequestFactoryForMethod(nameof(IDummyHttpApi.FetchSomeStuffWithDynamicRequestPropertyWithoutKey));
            var output = factory(new object[] { 6, someProperty, someOtherProperty });

            Assert.NotEmpty(output.Properties);
            Assert.Equal(someProperty, output.Properties["someValue"]);
            Assert.Equal(someOtherProperty, output.Properties["someOtherValue"]);
        }

        [Fact]
        public void DynamicRequestPropertiesWithDuplicateKeyShouldOverwritePreviousProperty()
        {
            var someProperty = new object();
            var someOtherProperty = new object();
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.BuildRequestFactoryForMethod(nameof(IDummyHttpApi.FetchSomeStuffWithDynamicRequestPropertyWithDuplicateKey));
            var output = factory(new object[] { 6, someProperty, someOtherProperty });

            Assert.Single(output.Properties);
            Assert.Equal(someOtherProperty, output.Properties["SomeProperty"]);
        }

        [Fact]
        public void HttpClientShouldPrefixedAbsolutePathToTheRequestUri()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.BuildRestResultFuncForMethod("FetchSomeStuffWithoutFullPath");
            var testHttpMessageHandler = new TestHttpMessageHandler();

            var task = (Task)factory(new HttpClient(testHttpMessageHandler) { BaseAddress = new Uri("http://api/foo/bar") }, new object[0]);
            task.Wait();

            Assert.Equal("http://api/foo/bar/string", testHttpMessageHandler.RequestMessage.RequestUri.ToString());
        }

        [Fact]
        public void HttpClientForVoidMethodShouldPrefixedAbsolutePathToTheRequestUri()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.BuildRestResultFuncForMethod("FetchSomeStuffWithVoid");
            var testHttpMessageHandler = new TestHttpMessageHandler();

            var task = (Task)factory(new HttpClient(testHttpMessageHandler) { BaseAddress = new Uri("http://api/foo/bar") }, new object[0]);
            task.Wait();

            Assert.Equal("http://api/foo/bar/void", testHttpMessageHandler.RequestMessage.RequestUri.ToString());
        }

        [Fact]
        public void HttpClientShouldNotPrefixEmptyAbsolutePathToTheRequestUri()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.BuildRestResultFuncForMethod("FetchSomeStuff");
            var testHttpMessageHandler = new TestHttpMessageHandler();

            var task = (Task)factory(new HttpClient(testHttpMessageHandler) { BaseAddress = new Uri("http://api/") }, new object[] { 42 });
            task.Wait();

            Assert.Equal("http://api/foo/bar/42", testHttpMessageHandler.RequestMessage.RequestUri.ToString());
        }

        [Fact]
        public void DontBlowUpWithDynamicAuthorizationHeaderAndContent()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.BuildRequestFactoryForMethod("PutSomeContentWithAuthorization");
            var output = factory(new object[] { 7, new { Octocat = "Dunetocat" }, "Basic RnVjayB5ZWFoOmhlYWRlcnMh" });

            Assert.NotNull(output.Headers.Authorization);//, "Headers include Authorization header");
            Assert.Equal("RnVjayB5ZWFoOmhlYWRlcnMh", output.Headers.Authorization.Parameter);
        }

        [Fact]
        public void SuchFlexibleContentTypeWow()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.BuildRequestFactoryForMethod("PutSomeStuffWithDynamicContentType");
            var output = factory(new object[] { 7, "such \"refit\" is \"amaze\" wow", "text/dson" });

            Assert.NotNull(output.Content);//, "Request has content");
            Assert.NotNull(output.Content.Headers.ContentType);//, "Headers include Content-Type header");
            Assert.Equal("text/dson", output.Content.Headers.ContentType.MediaType);//, "Content-Type header has the expected value");
        }

        [Fact]
        public void BodyContentGetsUrlEncoded()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.RunRequest("PostSomeUrlEncodedStuff");
            var output = factory(
                new object[] {
                    6,
                    new {
                        Foo = "Something",
                        Bar = 100,
                        Baz = "" // explicitly use blank to preserve value that would be stripped if null
                    }
                });

            Assert.Equal("Foo=Something&Bar=100&Baz=", output.SendContent);
        }

        [Fact]
        public void FormFieldGetsAliased()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.RunRequest("PostSomeAliasedUrlEncodedStuff");
            var output = factory(
                new object[] {
                    6,
                    new SomeRequestData {
                        ReadablePropertyName = 99
                    }
                });



            Assert.Equal("rpn=99", output.SendContent);
        }

        [Fact]
        public void CustomParmeterFormatter()
        {
            var settings = new RefitSettings { UrlParameterFormatter = new TestUrlParameterFormatter("custom-parameter") };
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>(settings);

            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuff");
            var output = factory(new object[] { 5 });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);
            Assert.Equal("/foo/bar/custom-parameter", uri.PathAndQuery);
        }

        [Fact]
        public void QueryStringWithEnumerablesCanBeFormatted()
        {
            var settings = new RefitSettings { UrlParameterFormatter = new TestEnumerableUrlParameterFormatter() };
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>(settings);

            var factory = fixture.BuildRequestFactoryForMethod("QueryWithEnumerable");
            var output = factory(new object[] { new int[] { 1, 2, 3 } });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);
            Assert.Equal("/query?numbers=1%2C2%2C3", uri.PathAndQuery);
        }

        [Fact]
        public void QueryStringWithArrayCanBeFormatted()
        {
            var settings = new RefitSettings { UrlParameterFormatter = new TestEnumerableUrlParameterFormatter() };
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>(settings);

            var factory = fixture.BuildRequestFactoryForMethod("QueryWithArray");
            var output = factory(new object[] { new int[] { 1, 2, 3 } });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);
            Assert.Equal("/query?numbers=1%2C2%2C3", uri.PathAndQuery);
        }

        [Fact]
        public void QueryStringWithArrayCanBeFormattedByAttribute()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();

            var factory = fixture.BuildRequestFactoryForMethod("UnescapedQueryParams");
            var output = factory(new object[] { "Select+Id,Name+From+Account" });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);
            Assert.Equal("/query?q=Select+Id,Name+From+Account", uri.PathAndQuery);
        }

        [Fact]
        public void QueryStringWithArrayCanBeFormattedByAttributeWithMultiple()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();

            var factory = fixture.BuildRequestFactoryForMethod("UnescapedQueryParamsWithFilter");
            var output = factory(new object[] { "Select+Id+From+Account", "*" });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);
            Assert.Equal("/query?q=Select+Id+From+Account&filter=*", uri.PathAndQuery);
        }

        [Fact]
        public void QueryStringWithArrayCanBeFormattedByDefaultSetting()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>(new RefitSettings
            {
                CollectionFormat = CollectionFormat.Multi
            });

            var factory = fixture.BuildRequestFactoryForMethod("QueryWithArray");
            var output = factory(new object[] { new[] { 1, 2, 3 } });

            Assert.Equal("/query?numbers=1&numbers=2&numbers=3", output.RequestUri.PathAndQuery);
        }

        [Fact]
        public void DefaultCollectionFormatCanBeOverridenByQueryAttribute()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>(new RefitSettings
            {
                CollectionFormat = CollectionFormat.Multi
            });

            var factory = fixture.BuildRequestFactoryForMethod("QueryWithArrayFormattedAsCsv");
            var output = factory(new object[] { new[] { 1, 2, 3 } });

            Assert.Equal("/query?numbers=1%2C2%2C3", output.RequestUri.PathAndQuery);
        }

        [Theory]
        [InlineData("QueryWithArrayFormattedAsMulti", "/query?numbers=1&numbers=2&numbers=3")]
        [InlineData("QueryWithArrayFormattedAsCsv", "/query?numbers=1%2C2%2C3")]
        [InlineData("QueryWithArrayFormattedAsSsv", "/query?numbers=1%202%203")]
        [InlineData("QueryWithArrayFormattedAsTsv", "/query?numbers=1%092%093")]
        [InlineData("QueryWithArrayFormattedAsPipes", "/query?numbers=1%7C2%7C3")]
        public void QueryStringWithArrayFormatted(string apiMethodName, string expectedQuery)
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();

            var factory = fixture.BuildRequestFactoryForMethod(apiMethodName);
            var output = factory(new object[] { new[] { 1, 2, 3 } });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);
            Assert.Equal(expectedQuery, uri.PathAndQuery);
        }

        [Fact]
        public void QueryStringWithArrayFormattedAsSsvAndItemsFormattedIndividually()
        {
            var settings = new RefitSettings { UrlParameterFormatter = new TestUrlParameterFormatter("custom-parameter") };
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>(settings);

            var factory = fixture.BuildRequestFactoryForMethod("QueryWithArrayFormattedAsSsv");
            var output = factory(new object[] { new int[] { 1, 2, 3 } });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);
            Assert.Equal("/query?numbers=custom-parameter%20custom-parameter%20custom-parameter", uri.PathAndQuery);
        }

        [Fact]
        public void QueryStringWithEnumerablesCanBeFormattedEnumerable()
        {
            var settings = new RefitSettings { UrlParameterFormatter = new TestEnumerableUrlParameterFormatter() };
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>(settings);

            var factory = fixture.BuildRequestFactoryForMethod("QueryWithEnumerable");

            var list = new List<int>
            {
                1, 2, 3
            };

            var output = factory(new object[] { list });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);
            Assert.Equal("/query?numbers=1%2C2%2C3", uri.PathAndQuery);
        }

        [Theory]
        [InlineData("QueryWithEnumerableFormattedAsMulti", "/query?lines=first&lines=second&lines=third")]
        [InlineData("QueryWithEnumerableFormattedAsCsv", "/query?lines=first%2Csecond%2Cthird")]
        [InlineData("QueryWithEnumerableFormattedAsSsv", "/query?lines=first%20second%20third")]
        [InlineData("QueryWithEnumerableFormattedAsTsv", "/query?lines=first%09second%09third")]
        [InlineData("QueryWithEnumerableFormattedAsPipes", "/query?lines=first%7Csecond%7Cthird")]
        public void QueryStringWithEnumerableFormatted(string apiMethodName, string expectedQuery)
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();

            var factory = fixture.BuildRequestFactoryForMethod(apiMethodName);

            var lines = new List<string>
            {
                "first",
                "second",
                "third"
            };

            var output = factory(new object[] { lines });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);
            Assert.Equal(expectedQuery, uri.PathAndQuery);
        }

        [Fact]
        public void QueryStringExcludesPropertiesWithPrivateGetters()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();

            var factory = fixture.BuildRequestFactoryForMethod("QueryWithObjectWithPrivateGetters");

            var person = new Person
            {
                FirstName = "Mickey",
                LastName = "Mouse"
            };

            var output = factory(new object[] { person });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);
            Assert.Equal("/query?FullName=Mickey%20Mouse", uri.PathAndQuery);
        }

        [Theory]
        [InlineData(FooWithEnumMember.A, "/query?foo=A")]
        [InlineData(FooWithEnumMember.B, "/query?foo=b")]
        public void QueryStringUsesEnumMemberAttribute(FooWithEnumMember queryParameter, string expectedQuery)
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.BuildRequestFactoryForMethod("QueryWithEnum");

            var output = factory(new object[] { queryParameter });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);
            Assert.Equal(expectedQuery, uri.PathAndQuery);
        }

        [Theory]
        [InlineData(FooWithEnumMember.A, "/query?foo=A")]
        [InlineData(FooWithEnumMember.B, "/query?foo=b")]
        public void QueryStringUsesEnumMemberAttributeInTypeWithEnum(FooWithEnumMember queryParameter, string expectedQuery)
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.BuildRequestFactoryForMethod("QueryWithTypeWithEnum");

            var output = factory(new object[] { new TypeFooWithEnumMember { Foo = queryParameter } });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);
            Assert.Equal(expectedQuery, uri.PathAndQuery);
        }

        [Theory]
        [InlineData("/api/123?text=title&optionalId=999&filters=A&filters=B")]
        public void TestNullableQueryStringParams(string expectedQuery)
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.BuildRequestFactoryForMethod("QueryWithOptionalParameters");
            var output = factory(new object[] { 123, "title", 999, new string[] { "A", "B" } });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);
            Assert.Equal(expectedQuery, uri.PathAndQuery);
        }

        [Theory]
        [InlineData("/api/123?text=title&filters=A&filters=B")]
        public void TestNullableQueryStringParamsWithANull(string expectedQuery)
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.BuildRequestFactoryForMethod("QueryWithOptionalParameters");
            var output = factory(new object[] { 123, "title", null, new string[] { "A", "B" } });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);
            Assert.Equal(expectedQuery, uri.PathAndQuery);
        }

        [Theory]
        [InlineData("/api/123?SomeProperty2=test&text=title&filters=A&filters=B")]
        public void TestNullableQueryStringParamsWithANullAndPathBoundObject(string expectedQuery)
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.BuildRequestFactoryForMethod("QueryWithOptionalParametersPathBoundObject");
            var output = factory(new object[] { new PathBoundObject() { SomeProperty = 123, SomeProperty2 = "test" }, "title", null, new string[] { "A", "B" } });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);
            Assert.Equal(expectedQuery, uri.PathAndQuery);
        }


        [Fact]
        [UseCulture("es-ES")] // Spain uses a , instead of a .
        public void DefaultParameterFormatterIsInvariant()
        {
            var settings = new RefitSettings();
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>(settings);

            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuff");
            var output = factory(new object[] { 5.4 });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);
            Assert.Equal("/foo/bar/5.4", uri.PathAndQuery);
        }

        [Fact]
        public void ICanPostAValueTypeIfIWantYoureNotTheBossOfMe()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.RunRequest("PostAValueType", "true");
            var guid = Guid.NewGuid();
            var expected = string.Format("\"{0}\"", guid);
            var output = factory(new object[] { 7, guid });

            Assert.Equal(expected, output.SendContent);
        }

        [Fact]
        public void DeleteWithQuery()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.BuildRequestFactoryForMethod("Clear");

            var output = factory(new object[] { 1 });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);

            Assert.Equal("/api/v1/video?playerIndex=1", uri.PathAndQuery);
        }

        [Fact]
        public void ClearWithQuery()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.BuildRequestFactoryForMethod("ClearWithEnumMember");

            var output = factory(new object[] { FooWithEnumMember.B });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);

            Assert.Equal("/api/bar?foo=b", uri.PathAndQuery);
        }

        [Fact]
        public void MultipartPostWithAliasAndHeader()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.RunRequest("UploadFile", "true");

            using var file = MultipartTests.GetTestFileStream("Test Files/Test.pdf");

            var sp = new StreamPart(file, "aFile");

            var output = factory(new object[] { 42, "aPath", sp, "theAuth", false, "theMeta" });

            var uri = new Uri(new Uri("http://api"), output.RequestMessage.RequestUri);

            Assert.Equal("/companies/42/aPath", uri.PathAndQuery);
            Assert.Equal("theAuth", output.RequestMessage.Headers.Authorization.ToString());
        }

        [Fact]
        public void PostBlobByteWithAlias()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.BuildRequestFactoryForMethod(nameof(IDummyHttpApi.Blob_Post_Byte));

            var bytes = new byte[10] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            var bap = new ByteArrayPart(bytes, "theBytes");

            var output = factory(new object[] { "the/path", bap });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);

            Assert.Equal("/blobstorage/the/path", uri.PathAndQuery);
        }

        [Fact]
        public void QueryWithAliasAndHeadersWorks()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.BuildRequestFactoryForMethod(nameof(IDummyHttpApi.QueryWithHeadersBeforeData));

            var authHeader = "theAuth";
            var langHeader = "LnG";
            var searchParam = "theSearchParam";
            var controlIdParam = "theControlId";
            var secretValue = "theSecret";



            var output = factory(new object[] { authHeader, langHeader, searchParam, controlIdParam, secretValue });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);

            Assert.Equal($"/api/someModule/deviceList?controlId={controlIdParam}&search={searchParam}&secret={secretValue}", uri.PathAndQuery);
            Assert.Equal(langHeader, output.Headers.GetValues("X-LnG").FirstOrDefault());
            Assert.Equal(authHeader, output.Headers.Authorization?.Scheme);
        }

        class RequestBuilderMock : IRequestBuilder
        {
            public int CallCount { get; private set; }

            public Func<HttpClient, object[], object> BuildRestResultFuncForMethod(string methodName, Type[] parameterTypes = null, Type[] genericArgumentTypes = null)
            {
                CallCount++;
                return null;
            }
        }

        [Fact]
        public void CachedRequestBuilderCallInternalBuilderForParametersWithSameNamesButDifferentNamespaces()
        {
            var internalBuilder = new RequestBuilderMock();
            var cachedBuilder = new CachedRequestBuilderImplementation(internalBuilder);

            cachedBuilder.BuildRestResultFuncForMethod("TestMethodName", new[] { typeof(CollisionA.SomeType) });
            cachedBuilder.BuildRestResultFuncForMethod("TestMethodName", new[] { typeof(CollisionB.SomeType) });
            cachedBuilder.BuildRestResultFuncForMethod("TestMethodName", null, new[] { typeof(CollisionA.SomeType) });
            cachedBuilder.BuildRestResultFuncForMethod("TestMethodName", null, new[] { typeof(CollisionB.SomeType) });

            Assert.Equal(4, internalBuilder.CallCount);
        }

        [Fact]
        public void DictionaryQueryWithEnumKeyProducesCorrectQueryString()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.BuildRequestFactoryForMethod(nameof(IDummyHttpApi.QueryWithDictionaryWithEnumKey));

            var dict = new Dictionary<TestEnum, string>
            {
                { TestEnum.A, "value1" },
                { TestEnum.B, "value2" },
            };

            var output = factory(new object[] { dict });
            var uri = new Uri(new Uri("http://api"), output.RequestUri);

            Assert.Equal("/foo?A=value1&B=value2", uri.PathAndQuery);
        }

        [Fact]
        public void DictionaryQueryWithPrefix()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.BuildRequestFactoryForMethod(nameof(IDummyHttpApi.QueryWithDictionaryWithPrefix));

            var dict = new Dictionary<TestEnum, string>
            {
                { TestEnum.A, "value1" },
                { TestEnum.B, "value2" },
            };

            var output = factory(new object[] { dict });
            var uri = new Uri(new Uri("http://api"), output.RequestUri);

            Assert.Equal("/foo?dictionary.A=value1&dictionary.B=value2", uri.PathAndQuery);
        }

        [Fact]
        public void DictionaryQueryWithNumericKeyProducesCorrectQueryString()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.BuildRequestFactoryForMethod(nameof(IDummyHttpApi.QueryWithDictionaryWithNumericKey));

            var dict = new Dictionary<int, string>
            {
                { 1, "value1" },
                { 2, "value2" },
            };

            var output = factory(new object[] { dict });
            var uri = new Uri(new Uri("http://api"), output.RequestUri);

            Assert.Equal("/foo?1=value1&2=value2", uri.PathAndQuery);
        }

        [Fact]
        public void DictionaryQueryWithCustomFormatterProducesCorrectQueryString()
        {
            var urlParameterFormatter = new TestEnumUrlParameterFormatter();

            var refitSettings = new RefitSettings { UrlParameterFormatter = urlParameterFormatter };
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>(refitSettings);
            var factory = fixture.BuildRequestFactoryForMethod(nameof(IDummyHttpApi.QueryWithDictionaryWithEnumKey));

            var dict = new Dictionary<TestEnum, string>
            {
                { TestEnum.A, "value1" },
                { TestEnum.B, "value2" },
            };

            var output = factory(new object[] { dict });
            var uri = new Uri(new Uri("http://api"), output.RequestUri);

            Assert.Equal($"/foo?{(int)TestEnum.A}=value1{urlParameterFormatter.StringParameterSuffix}&{(int)TestEnum.B}=value2{urlParameterFormatter.StringParameterSuffix}", uri.PathAndQuery);
        }

        [Fact]
        public void ComplexQueryObjectWithAliasedDictionaryProducesCorrectQueryString()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.BuildRequestFactoryForMethod(nameof(IDummyHttpApi.ComplexQueryObjectWithDictionary));

            var complexQuery = new ComplexQueryObject
            {
                TestAliasedDictionary = new Dictionary<TestEnum, string>
                {
                    { TestEnum.A, "value1" },
                    { TestEnum.B, "value2" },
                },
            };

            var output = factory(new object[] { complexQuery });
            var uri = new Uri(new Uri("http://api"), output.RequestUri);

            Assert.Equal("/foo?test-dictionary-alias.A=value1&test-dictionary-alias.B=value2", uri.PathAndQuery);
        }

        [Fact]
        public void ComplexQueryObjectWithDictionaryProducesCorrectQueryString()
        {
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
            var factory = fixture.BuildRequestFactoryForMethod(nameof(IDummyHttpApi.ComplexQueryObjectWithDictionary));

            var complexQuery = new ComplexQueryObject
            {
                TestDictionary = new Dictionary<TestEnum, string>
                {
                    { TestEnum.A, "value1" },
                    { TestEnum.B, "value2" },
                },
            };

            var output = factory(new object[] { complexQuery });
            var uri = new Uri(new Uri("http://api"), output.RequestUri);

            Assert.Equal("/foo?TestDictionary.A=value1&TestDictionary.B=value2", uri.PathAndQuery);
        }

        [Fact]
        public void ComplexQueryObjectWithDictionaryAndCustomFormatterProducesCorrectQueryString()
        {
            var urlParameterFormatter = new TestEnumUrlParameterFormatter();
            var refitSettings = new RefitSettings { UrlParameterFormatter = urlParameterFormatter };
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>(refitSettings);
            var factory = fixture.BuildRequestFactoryForMethod(nameof(IDummyHttpApi.ComplexQueryObjectWithDictionary));

            var complexQuery = new ComplexQueryObject
            {
                TestDictionary = new Dictionary<TestEnum, string>
                {
                    { TestEnum.A, "value1" },
                    { TestEnum.B, "value2" },
                },
            };

            var output = factory(new object[] { complexQuery });
            var uri = new Uri(new Uri("http://api"), output.RequestUri);

            Assert.Equal($"/foo?TestDictionary.{(int)TestEnum.A}=value1{urlParameterFormatter.StringParameterSuffix}&TestDictionary.{(int)TestEnum.B}=value2{urlParameterFormatter.StringParameterSuffix}", uri.PathAndQuery);
        }
    }

    static class RequestBuilderTestExtensions
    {
        public static Func<object[], HttpRequestMessage> BuildRequestFactoryForMethod(this IRequestBuilder builder, string methodName, string baseAddress = "http://api/")
        {
            var factory = builder.BuildRestResultFuncForMethod(methodName);
            var testHttpMessageHandler = new TestHttpMessageHandler();


            return paramList =>
            {
                var task = (Task)factory(new HttpClient(testHttpMessageHandler) { BaseAddress = new Uri(baseAddress) }, paramList);
                task.Wait();
                return testHttpMessageHandler.RequestMessage;
            };
        }


        public static Func<object[], TestHttpMessageHandler> RunRequest(this IRequestBuilder builder, string methodName, string returnContent = null, string baseAddress = "http://api/")
        {
            var factory = builder.BuildRestResultFuncForMethod(methodName);
            var testHttpMessageHandler = new TestHttpMessageHandler();
            if (returnContent != null)
            {
                testHttpMessageHandler.Content = new StringContent(returnContent);
            }

            return paramList =>
            {
                var task = (Task)factory(new HttpClient(testHttpMessageHandler) { BaseAddress = new Uri(baseAddress) }, paramList);
                try
                {
                    task.Wait();
                }
                catch(AggregateException e) when (e.InnerException is TaskCanceledException)
                {

                }

                return testHttpMessageHandler;
            };
        }
    }
}
