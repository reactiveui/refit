// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Tests;

/// <summary>Tests for <see cref="RestMethodInfoInternal"/> query and body parameter parsing.</summary>
public partial class RestMethodInfoTests
{
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
                .First(static x => x.Name == nameof(IRestMethodInfoTests.ManyComplexTypes)));

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
                .First(static x => x.Name == nameof(IRestMethodInfoTests.GetWithBodyDetected)));

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
                .First(static x => x.Name == nameof(IRestMethodInfoTests.PostWithDictionaryQuery)));

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
                .First(static x => x.Name == nameof(IRestMethodInfoTests.PostWithComplexTypeQuery)));

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

        var output = await factory([new System.Globalization.CultureInfo("en-US")]);

        var uri = new Uri(new(BaseAddressUri), output.RequestUri!);

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

        const int stuffId = 42;
        var output = await factory([stuffId]);

        var uri = new Uri(new(BaseAddressUri), output.RequestUri!);

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
        const int pathBoundId = 123;
        var output = await factory(
        [
            new DerivedPathBoundObject { SomeProperty = pathBoundId, SomeProperty2 = "test" },
            "title",
            null!,
            _filterValues
        ]);

        var uri = new Uri(new(BaseAddressUri), output.RequestUri!);
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

        var output = await factory([param]);

        var uri = new Uri(new(BaseAddressUri), output.RequestUri!);

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

        var output = await factory([param]);

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

        var output = await factory([param]);

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

        var output = await factory([param]);

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

        var output = await factory([param]);

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

        var output = await factory([param]);

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

        var param = new ComplexQueryObject { TestCollection = _testCollectionValues };
        var output = await factory([param]);
        var uri = new Uri(new(BaseAddressUri), output.RequestUri!);

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
        var output = await factory([param]);
        var uri = new Uri(new(BaseAddressUri), output.RequestUri!);

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

        var param = new ComplexQueryObject { TestCollection = _testCollectionValues };
        var output = await factory([param]);
        var uri = new Uri(new(BaseAddressUri), output.RequestUri!);

        await Assert.That(uri.PathAndQuery).IsEqualTo("/foo?TestCollection=1&TestCollection=2&TestCollection=3");
    }

    /// <summary>Verifies multiple query attributes with nulls produce the expected number of query entries.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task MultipleQueryAttributesWithNulls()
    {
        const int expectedQueryParameterCount = 3;
        var input = typeof(IRestMethodInfoTests);
        var fixtureParams = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(static x => x.Name == nameof(IRestMethodInfoTests.MultipleQueryAttributes)));

        await Assert.That(fixtureParams.QueryParameterMap.Count).IsEqualTo(expectedQueryParameterCount);
    }
}
