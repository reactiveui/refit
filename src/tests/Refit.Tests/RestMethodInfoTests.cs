// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Refit.Tests;

/// <summary>Tests for <see cref="RestMethodInfoInternal"/> parameter and header parsing.</summary>
[RequiresUnreferencedCode("Refit's reflection-based serialization and request building are exercised by these tests.")]
[RequiresDynamicCode("Refit's reflection-based serialization and request building are exercised by these tests.")]
public partial class RestMethodInfoTests
{
    /// <summary>Filter values used by path-bound object query tests.</summary>
    private static readonly string[] _filterValues = ["A", "B"];

    /// <summary>Verifies a method with too many complex types throws.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task TooManyComplexTypesThrows()
    {
        var input = typeof(IRestMethodInfoTests);

        await Assert.That(() =>
        {
            _ = new RestMethodInfoInternal(
                input,
                input
                    .GetMethods()
                    .First(x => x.Name == nameof(IRestMethodInfoTests.TooManyComplexTypes)));
        }).ThrowsExactly<ArgumentException>();
    }

    /// <summary>Verifies a method with many complex types maps the body parameter.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ManyComplexTypes()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(x => x.Name == nameof(IRestMethodInfoTests.ManyComplexTypes)));

        await Assert.That(fixture.QueryParameterMap).HasSingleItem();
        await Assert.That(fixture.BodyParameterInfo).IsNotNull();
        await Assert.That(fixture.BodyParameterInfo!.Item3).IsEqualTo(1);
    }

    /// <summary>Verifies the body parameter is detected by default for put, post and patch.</summary>
    /// <param name="interfaceMethodName">The name of the interface method under test.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [Arguments(nameof(IRestMethodInfoTests.PutWithBodyDetected))]
    [Arguments(nameof(IRestMethodInfoTests.PostWithBodyDetected))]
    [Arguments(nameof(IRestMethodInfoTests.PatchWithBodyDetected))]
    public async Task DefaultBodyParameterDetected(string interfaceMethodName)
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input.GetMethods().First(x => x.Name == interfaceMethodName));

        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo).IsNotNull();
    }

    /// <summary>Verifies the body parameter is not inferred for a GET method.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DefaultBodyParameterNotDetectedForGet()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(x => x.Name == nameof(IRestMethodInfoTests.GetWithBodyDetected)));

        await Assert.That(fixture.QueryParameterMap).HasSingleItem();
        await Assert.That(fixture.BodyParameterInfo).IsNull();
    }

    /// <summary>Verifies a dictionary query parameter maps to a single query entry.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task PostWithDictionaryQueryParameter()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(x => x.Name == nameof(IRestMethodInfoTests.PostWithDictionaryQuery)));

        await Assert.That(fixture.QueryParameterMap).HasSingleItem();
        await Assert.That(fixture.BodyParameterInfo).IsNull();
    }

    /// <summary>Verifies an object query parameter produces a single query parameter value.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task PostWithObjectQueryParameterHasSingleQueryParameterValue()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixtureParams = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(x => x.Name == nameof(IRestMethodInfoTests.PostWithComplexTypeQuery)));

        await Assert.That(fixtureParams.QueryParameterMap).HasSingleItem();
        await Assert.That(fixtureParams.QueryParameterMap[0]).IsEqualTo("queryParams");
        await Assert.That(fixtureParams.BodyParameterInfo).IsNull();
    }

    /// <summary>Verifies a CultureInfo query parameter does not cause a stack overflow.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task CultureInfoQueryParameterDoesNotStackOverflow()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();

        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IDummyHttpApi.QueryWithCultureInfo));

        var output = factory([new System.Globalization.CultureInfo("en-US")]);

        var uri = new Uri(new("http://api"), output.RequestUri!);

        await Assert.That(uri.PathAndQuery).IsEqualTo("/foo?culture=en-US");
    }

    /// <summary>Verifies a base address with a trailing slash does not produce a double slash.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task BaseAddressWithTrailingSlashDoesNotProduceDoubleSlash()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();

        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IDummyHttpApi.FetchSomeStuff),
            baseAddress: "http://api/v1/");

        var output = factory([42]);

        var uri = new Uri(new("http://api"), output.RequestUri!);

        await Assert.That(uri.AbsolutePath).IsEqualTo("/v1/foo/bar/42");
    }

    /// <summary>Verifies a derived path-bound object does not duplicate the path property as a query.</summary>
    /// <param name="expectedQuery">The expected request path and query string.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [Arguments("/api/123?SomeProperty2=test&text=title&filters=A&filters=B")]
    public async Task DerivedPathBoundObjectDoesNotDuplicatePathPropertyAsQuery(string expectedQuery)
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            "QueryWithOptionalParametersPathBoundObject");
        var output = factory(
        [
            new DerivedPathBoundObject { SomeProperty = 123, SomeProperty2 = "test" },
            "title",
            null!,
            _filterValues
        ]);

        var uri = new Uri(new("http://api"), output.RequestUri!);
        await Assert.That(uri.PathAndQuery).IsEqualTo(expectedQuery);
    }

    /// <summary>Verifies an object query parameter produces the correct query string.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task PostWithObjectQueryParameterHasCorrectQuerystring()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();

        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IDummyHttpApi.PostWithComplexTypeQuery));

        var param = new ComplexQueryObject { TestAlias1 = "one", TestAlias2 = "two" };

        var output = factory([param]);

        var uri = new Uri(new("http://api"), output.RequestUri!);

        await Assert.That(uri.PathAndQuery).IsEqualTo("/foo?test-query-alias=one&TestAlias2=two");
    }

    /// <summary>Verifies an object query parameter skips ignored properties.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task PostWithObjectQueryParameterSkipsIgnoredProperties()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();

        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IDummyHttpApi.PostWithComplexTypeQuery));

        var param = new ComplexQueryObject
        {
            TestAlias1 = "one",
            InternalUseOnlyIgnoredByDataMember = "nope",
            InternalUseOnlyIgnoredBySystemTextJson = "nope"
        };

        var output = factory([param]);

        await Assert.That(output.RequestUri!.PathAndQuery).IsEqualTo("/foo?test-query-alias=one");
    }

    /// <summary>Verifies an enum list query parameter uses the multi collection format.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task PostWithObjectQueryParameterWithEnumList_Multi()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();

        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IDummyHttpApi.PostWithComplexTypeQuery));

        var param = new ComplexQueryObject
        {
            EnumCollectionMulti = [TestEnum.A, TestEnum.B]
        };

        var output = factory([param]);

        await Assert.That(output.RequestUri!.PathAndQuery).IsEqualTo(
            "/foo?listOfEnumMulti=A&listOfEnumMulti=B");
    }

    /// <summary>Verifies an object list with provided enum values uses the multi collection format.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task PostWithObjectQueryParameterWithObjectListWithProvidedEnumValues_Multi()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();

        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IDummyHttpApi.PostWithComplexTypeQuery));

        var param = new ComplexQueryObject
        {
            ObjectCollectionMulti = [TestEnum.A, TestEnum.B]
        };

        var output = factory([param]);

        await Assert.That(output.RequestUri!.PathAndQuery).IsEqualTo(
            "/foo?ObjectCollectionMulti=A&ObjectCollectionMulti=B");
    }

    /// <summary>Verifies an enum list query parameter uses the CSV collection format.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task PostWithObjectQueryParameterWithEnumList_Csv()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();

        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IDummyHttpApi.PostWithComplexTypeQuery));

        var param = new ComplexQueryObject
        {
            EnumCollectionCsv = [TestEnum.A, TestEnum.B]
        };

        var output = factory([param]);

        await Assert.That(output.RequestUri!.PathAndQuery).IsEqualTo("/foo?EnumCollectionCsv=A%2CB");
    }

    /// <summary>Verifies an object list with provided enum values uses the CSV collection format.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task PostWithObjectQueryParameterWithObjectListWithProvidedEnumValues_Csv()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();

        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IDummyHttpApi.PostWithComplexTypeQuery));

        var param = new ComplexQueryObject
        {
            ObjectCollectionCcv = [TestEnum.A, TestEnum.B]
        };

        var output = factory([param]);

        await Assert.That(output.RequestUri!.PathAndQuery).IsEqualTo("/foo?listOfObjectsCsv=A%2CB");
    }

    /// <summary>Verifies an object query parameter with an inner collection produces the correct query string.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ObjectQueryParameterWithInnerCollectionHasCorrectQuerystring()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IDummyHttpApi.ComplexTypeQueryWithInnerCollection));

        var param = new ComplexQueryObject { TestCollection = [1, 2, 3] };
        var output = factory([param]);
        var uri = new Uri(new("http://api"), output.RequestUri!);

        await Assert.That(uri.PathAndQuery).IsEqualTo("/foo?TestCollection=1%2C2%2C3");
    }

    /// <summary>Verifies an object query parameter without a query attribute honors the multi collection format.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ObjectQueryParameterWithoutQueryAttributeHonorsMultiCollectionFormat()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IDummyHttpApi.ComplexTypeQueryWithoutQueryAttribute));

        var param = new ComplexQueryObject
        {
            EnumCollectionMulti = [TestEnum.A, TestEnum.B]
        };
        var output = factory([param]);
        var uri = new Uri(new("http://api"), output.RequestUri!);

        await Assert.That(uri.PathAndQuery).IsEqualTo("/foo?listOfEnumMulti=A&listOfEnumMulti=B");
    }

    /// <summary>Verifies a parameter-level collection format applies to inner collections without their own query attribute.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ParameterLevelCollectionFormatAppliesToInnerCollectionsWithoutOwnQueryAttribute()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IDummyHttpApi.ComplexTypeQueryParameterLevelMulti));

        var param = new ComplexQueryObject { TestCollection = [1, 2, 3] };
        var output = factory([param]);
        var uri = new Uri(new("http://api"), output.RequestUri!);

        await Assert.That(uri.PathAndQuery).IsEqualTo("/foo?TestCollection=1&TestCollection=2&TestCollection=3");
    }

    /// <summary>Verifies multiple query attributes with nulls produce the expected number of query entries.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task MultipleQueryAttributesWithNulls()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixtureParams = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(x => x.Name == nameof(IRestMethodInfoTests.MultipleQueryAttributes)));

        await Assert.That(fixtureParams.QueryParameterMap.Count).IsEqualTo(3);
    }

    /// <summary>Verifies a method with a garbage path throws an argument exception.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task GarbagePathsShouldThrow()
    {
        var shouldDie = true;

        try
        {
            var input = typeof(IRestMethodInfoTests);
            _ = new RestMethodInfoInternal(
                input,
                input
                    .GetMethods()
                    .First(x => x.Name == nameof(IRestMethodInfoTests.GarbagePath)));
        }
        catch (ArgumentException)
        {
            shouldDie = false;
        }

        await Assert.That(shouldDie).IsFalse();
    }

    /// <summary>Verifies a method with missing parameters throws an argument exception.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task MissingParametersShouldBlowUp()
    {
        var shouldDie = true;

        try
        {
            var input = typeof(IRestMethodInfoTests);
            _ = new RestMethodInfoInternal(
                input,
                input
                    .GetMethods()
                    .First(
                        x =>
                            x.Name
                            == nameof(IRestMethodInfoTests.FetchSomeStuffMissingParameters)));
        }
        catch (ArgumentException)
        {
            shouldDie = false;
        }

        await Assert.That(shouldDie).IsFalse();
    }

    /// <summary>Verifies basic parameter mapping resolves names and types correctly.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ParameterMappingSmokeTest()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.FetchSomeStuff)));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo).IsNull();
    }

    /// <summary>Verifies parameter mapping when the same id appears in a few places.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ParameterMappingWithTheSameIdInAFewPlaces()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(x => x.Name == nameof(IRestMethodInfoTests.FetchSomeStuffWithTheSameId)));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo).IsNull();
    }

    /// <summary>Verifies parameter mapping when the same id appears in the query parameter.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ParameterMappingWithTheSameIdInTheQueryParameter()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(
                    x =>
                        x.Name
                        == nameof(
                            IRestMethodInfoTests.FetchSomeStuffWithTheIdInAParameterMultipleTimes)));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo).IsNull();
    }

    /// <summary>Verifies parameter mapping for a round-tripping parameter.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ParameterMappingWithRoundTrippingSmokeTest()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(
                    x =>
                        x.Name
                        == nameof(IRestMethodInfoTests.FetchSomeStuffWithRoundTrippingParam)));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("path");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.RoundTripping);
        await Assert.That(fixture.ParameterMap[1].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[1].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo).IsNull();
    }

    /// <summary>Verifies a non-string round-tripping parameter throws an argument exception.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ParameterMappingWithNonStringRoundTrippingShouldThrow()
    {
        var input = typeof(IRestMethodInfoTests);
        await Assert.That(() =>
        {
            _ = new RestMethodInfoInternal(
                input,
                input
                    .GetMethods()
                    .First(
                        x =>
                            x.Name
                            == nameof(
                                IRestMethodInfoTests.FetchSomeStuffWithNonStringRoundTrippingParam)));
        }).ThrowsExactly<ArgumentException>();
    }

    /// <summary>Verifies parameter mapping with a query parameter.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ParameterMappingWithQuerySmokeTest()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(x => x.Name == nameof(IRestMethodInfoTests.FetchSomeStuffWithQueryParam)));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(fixture.QueryParameterMap[1]).IsEqualTo("search");
        await Assert.That(fixture.BodyParameterInfo).IsNull();
    }

    /// <summary>Verifies parameter mapping with a hardcoded query parameter.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ParameterMappingWithHardcodedQuerySmokeTest()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(
                    x =>
                        x.Name
                        == nameof(IRestMethodInfoTests.FetchSomeStuffWithHardcodedQueryParam)));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo).IsNull();
    }

    /// <summary>Verifies an alias on a parameter maps correctly.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AliasMappingShouldWork()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(x => x.Name == nameof(IRestMethodInfoTests.FetchSomeStuffWithAlias)));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo).IsNull();
    }

    /// <summary>Verifies multiple parameters per URL segment map correctly.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task MultipleParametersPerSegmentShouldWork()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.FetchAnImage)));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("width");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(fixture.ParameterMap[1].Name).IsEqualTo("height");
        await Assert.That(fixture.ParameterMap[1].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo).IsNull();
    }

    /// <summary>Verifies the body parameter is located and indexed correctly.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task FindTheBodyParameter()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(x => x.Name == nameof(IRestMethodInfoTests.FetchSomeStuffWithBody)));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);

        await Assert.That(fixture.BodyParameterInfo).IsNotNull();
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo!.Item3).IsEqualTo(1);
    }

    /// <summary>Verifies the authorize parameter is located and indexed correctly.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task FindTheAuthorizeParameter()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(
                    x =>
                        x.Name
                        == nameof(
                            IRestMethodInfoTests.FetchSomeStuffWithAuthorizationSchemeSpecified)));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);

        await Assert.That(fixture.AuthorizeParameterInfo).IsNotNull();
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.AuthorizeParameterInfo!.Item2).IsEqualTo(1);
    }

    /// <summary>Verifies URL-encoded content is allowed and reflected in the body serialization method.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AllowUrlEncodedContent()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(x => x.Name == nameof(IRestMethodInfoTests.PostSomeUrlEncodedStuff)));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);

        await Assert.That(fixture.BodyParameterInfo).IsNotNull();
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo!.Item1).IsEqualTo(BodySerializationMethod.UrlEncoded);
    }

    /// <summary>Verifies hardcoded headers are parsed into the headers dictionary.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task HardcodedHeadersShouldWork()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(
                    x =>
                        x.Name
                        == nameof(IRestMethodInfoTests.FetchSomeStuffWithHardcodedHeaders)));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo).IsNull();

        await Assert.That(fixture.Headers.ContainsKey("Api-Version")).IsTrue().Because("Headers include Api-Version header");
        await Assert.That(fixture.Headers["Api-Version"]).IsEqualTo("2");
        await Assert.That(fixture.Headers.ContainsKey("User-Agent")).IsTrue().Because("Headers include User-Agent header");
        await Assert.That(fixture.Headers["User-Agent"]).IsEqualTo("RefitTestClient");
        await Assert.That(fixture.Headers.ContainsKey("Accept")).IsTrue().Because("Headers include Accept header");
        await Assert.That(fixture.Headers["Accept"]).IsEqualTo("application/json");
        await Assert.That(fixture.Headers.Count).IsEqualTo(3);
    }

    /// <summary>Verifies dynamic headers are mapped and merged with hardcoded headers.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DynamicHeadersShouldWork()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(
                    x => x.Name == nameof(IRestMethodInfoTests.FetchSomeStuffWithDynamicHeader)));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.PropertyParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo).IsNull();

        await Assert.That(fixture.HeaderParameterMap[1]).IsEqualTo("Authorization");
        await Assert.That(fixture.Headers.ContainsKey("User-Agent")).IsTrue().Because("Headers include User-Agent header");
        await Assert.That(fixture.Headers["User-Agent"]).IsEqualTo("RefitTestClient");
        await Assert.That(fixture.Headers.Count).IsEqualTo(2);
    }

    /// <summary>Verifies constructor guard and missing HTTP method branches.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ConstructorGuardsAndMissingHttpMethodThrow()
    {
        var input = typeof(IRestMethodInfoTests);
        var method = input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.FetchSomeStuff));
        var missingInput = typeof(IMissingHttpMethodApi);
        var missingHttpMethod = missingInput.GetMethod(nameof(IMissingHttpMethodApi.MethodWithoutHttpMethod))!;

        await Assert.That(() => new RestMethodInfoInternal(null!, method))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => new RestMethodInfoInternal(input, null!))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => new RestMethodInfoInternal(missingInput, missingHttpMethod))
            .ThrowsExactly<InvalidOperationException>();
    }

    /// <summary>Verifies CR/LF paths are rejected.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task PathContainingNewlineThrows()
    {
        var input = typeof(IRestMethodInfoTests);
        var method = input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.NewlinePath));

        await Assert.That(() => new RestMethodInfoInternal(input, method))
            .ThrowsExactly<ArgumentException>();
    }

    /// <summary>Verifies object route binding ignores unreadable public properties.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ObjectRouteBindingIgnoresUnreadableProperties()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.ObjectRouteWithUnreadableProperty)));

        await Assert.That(fixture.ParameterMap).HasSingleItem();
        await Assert.That(fixture.ParameterMap[0].IsObjectPropertyParameter).IsTrue();
        await Assert.That(fixture.ParameterMap[0].ParameterProperties).HasSingleItem();
        await Assert.That(fixture.ParameterMap[0].ParameterProperties[0].PropertyInfo.Name)
            .IsEqualTo(nameof(RouteObjectWithUnreadableProperty.Visible));
    }

    /// <summary>Verifies a path cannot bind a parameter both directly and through one of its properties.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ObjectRouteBindingConflictingDirectAndPropertyMatchThrows()
    {
        var input = typeof(IRestMethodInfoTests);
        var method = input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.ConflictingObjectRoute));

        await Assert.That(() => new RestMethodInfoInternal(input, method))
            .ThrowsExactly<ArgumentException>();
    }

    /// <summary>Verifies only one authorization parameter may be declared.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DuplicateAuthorizeParametersThrow()
    {
        var input = typeof(IRestMethodInfoTests);
        var method = input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.DuplicateAuthorize));

        await Assert.That(() => new RestMethodInfoInternal(input, method))
            .ThrowsExactly<ArgumentException>();
    }
}
