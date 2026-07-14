// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Microsoft.AspNetCore.WebUtilities;

namespace Refit.Tests;

/// <summary>Tests for <see cref="RequestBuilderImplementation{T}"/> dictionary and complex query handling.</summary>
public partial class RequestBuilderTests
{
    /// <summary>The method name used by the cached request-builder collision tests.</summary>
    private const string TestMethodName = "TestMethodName";

    /// <summary>The first dictionary/query test value.</summary>
    private const string Value1 = "value1";

    /// <summary>The second dictionary/query test value.</summary>
    private const string Value2 = "value2";

    /// <summary>The identifier value reused across path and query segments.</summary>
    private const string TheId = "theId";

    /// <summary>The title argument reused across the dictionary query tests.</summary>
    private const string TitleValue = "title";

    /// <summary>The identifier rendered into the "/api/{id}" path across the optional-parameter query tests.</summary>
    private const int ResourceId = 123;

    /// <summary>A query string with an array can be formatted by attribute.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task QueryStringWithArrayCanBeFormattedByAttribute()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();

        var factory = fixture.BuildRequestFactoryForMethod("UnescapedQueryParams");
        var output = await factory(["Select+Id,Name+From+Account"]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);
        await Assert.That(uri.PathAndQuery).IsEqualTo("/query?q=Select+Id,Name+From+Account");
    }

    /// <summary>A query string with an array can be formatted by attribute with multiple values.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task QueryStringWithArrayCanBeFormattedByAttributeWithMultiple()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();

        var factory = fixture.BuildRequestFactoryForMethod("UnescapedQueryParamsWithFilter");
        var output = await factory(["Select+Id+From+Account", "*"]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);
        await Assert.That(uri.PathAndQuery).IsEqualTo("/query?q=Select+Id+From+Account&filter=*");
    }

    /// <summary>A query string with an array can be formatted by the default setting.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task QueryStringWithArrayCanBeFormattedByDefaultSetting()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>(
            new() { CollectionFormat = CollectionFormat.Multi });

        var factory = fixture.BuildRequestFactoryForMethod("QueryWithArray");
        var output = await factory([_intArray123]);

        await Assert.That(output.RequestUri!.PathAndQuery).IsEqualTo("/query?numbers=1&numbers=2&numbers=3");
    }

    /// <summary>The default collection format can be overridden by a query attribute.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DefaultCollectionFormatCanBeOverridenByQueryAttribute()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>(
            new() { CollectionFormat = CollectionFormat.Multi });

        var factory = fixture.BuildRequestFactoryForMethod("QueryWithArrayFormattedAsCsv");
        var output = await factory([_intArray123]);

        await Assert.That(output.RequestUri!.PathAndQuery).IsEqualTo("/query?numbers=1%2C2%2C3");
    }

    /// <summary>A request with a parameter used in multiple places renders correctly.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task RequestWithParameterInMultiplePlaces()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();

        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IDummyHttpApi.FetchSomeStuffWithTheSameId));
        var output = await factory([TheId]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);

        var builder = new UriBuilder(uri);
        var qs = QueryHelpers.ParseQuery(uri.Query);
        await Assert.That(builder.Path).IsEqualTo("/foo/bar/theId");
        await Assert.That(qs["param1"].ToString()).IsEqualTo(TheId);
        await Assert.That(qs["param2"].ToString()).IsEqualTo(TheId);
    }

    /// <summary>A request with a parameter used multiple times in a query parameter renders correctly.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task RequestWithParameterInAQueryParameterMultipleTimes()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();

        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IDummyHttpApi.FetchSomeStuffWithTheIdInAParameterMultipleTimes));
        var output = await factory([TheId]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);
        await Assert.That(uri.PathAndQuery).IsEqualTo("/foo/bar?param=first%20theId%20and%20second%20theId");
    }

    /// <summary>A query string with an array is formatted according to the method's format.</summary>
    /// <param name="apiMethodName">The name of the interface method to build.</param>
    /// <param name="expectedQuery">The expected resulting path and query.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [Arguments("QueryWithArrayFormattedAsMulti", "/query?numbers=1&numbers=2&numbers=3")]
    [Arguments("QueryWithArrayFormattedAsCsv", "/query?numbers=1%2C2%2C3")]
    [Arguments("QueryWithArrayFormattedAsSsv", "/query?numbers=1%202%203")]
    [Arguments("QueryWithArrayFormattedAsTsv", "/query?numbers=1%092%093")]
    [Arguments("QueryWithArrayFormattedAsPipes", "/query?numbers=1%7C2%7C3")]
    public async Task QueryStringWithArrayFormatted(string apiMethodName, string expectedQuery)
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();

        var factory = fixture.BuildRequestFactoryForMethod(apiMethodName);
        var output = await factory([_intArray123]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);
        await Assert.That(uri.PathAndQuery).IsEqualTo(expectedQuery);
    }

    /// <summary>A query string array formatted as ssv has its items formatted individually.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task QueryStringWithArrayFormattedAsSsvAndItemsFormattedIndividually()
    {
        var settings = new RefitSettings
        {
            UrlParameterFormatter = new TestUrlParameterFormatter("custom-parameter")
        };
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>(settings);

        var factory = fixture.BuildRequestFactoryForMethod("QueryWithArrayFormattedAsSsv");
        var output = await factory([_intArray123]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);
        await Assert.That(uri.PathAndQuery).IsEqualTo("/query?numbers=custom-parameter%20custom-parameter%20custom-parameter");
    }

    /// <summary>A query string with enumerables can be formatted for an enumerable type.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task QueryStringWithEnumerablesCanBeFormattedEnumerable()
    {
        var settings = new RefitSettings
        {
            UrlParameterFormatter = new TestEnumerableUrlParameterFormatter()
        };
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>(settings);

        var factory = fixture.BuildRequestFactoryForMethod("QueryWithEnumerable");

        const int secondSampleValue = 2;
        const int thirdSampleValue = 3;
        var list = new List<int> { 1, secondSampleValue, thirdSampleValue };

        var output = await factory([list]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);
        await Assert.That(uri.PathAndQuery).IsEqualTo("/query?numbers=1%2C2%2C3");
    }

    /// <summary>A query string with an enumerable is formatted according to the method's format.</summary>
    /// <param name="apiMethodName">The name of the interface method to build.</param>
    /// <param name="expectedQuery">The expected resulting path and query.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [Arguments(
        "QueryWithEnumerableFormattedAsMulti",
        "/query?lines=first&lines=second&lines=third")]
    [Arguments("QueryWithEnumerableFormattedAsCsv", "/query?lines=first%2Csecond%2Cthird")]
    [Arguments("QueryWithEnumerableFormattedAsSsv", "/query?lines=first%20second%20third")]
    [Arguments("QueryWithEnumerableFormattedAsTsv", "/query?lines=first%09second%09third")]
    [Arguments("QueryWithEnumerableFormattedAsPipes", "/query?lines=first%7Csecond%7Cthird")]
    public async Task QueryStringWithEnumerableFormatted(string apiMethodName, string expectedQuery)
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();

        var factory = fixture.BuildRequestFactoryForMethod(apiMethodName);

        var lines = new List<string> { "first", "second", "third" };

        var output = await factory([lines]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);
        await Assert.That(uri.PathAndQuery).IsEqualTo(expectedQuery);
    }

    /// <summary>The query string excludes properties with private getters.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task QueryStringExcludesPropertiesWithPrivateGetters()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();

        var factory = fixture.BuildRequestFactoryForMethod("QueryWithObjectWithPrivateGetters");

        var person = new Person { FirstName = "Mickey", LastName = "Mouse" };

        var output = await factory([person]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);
        await Assert.That(uri.PathAndQuery).IsEqualTo("/query?FullName=Mickey%20Mouse");
    }

    /// <summary>The query string uses the EnumMember attribute.</summary>
    /// <param name="queryParameter">The enum value to render in the query.</param>
    /// <param name="expectedQuery">The expected resulting path and query.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [Arguments(FooWithEnumMember.A, "/query?foo=A")]
    [Arguments(FooWithEnumMember.B, "/query?foo=b")]
    public async Task QueryStringUsesEnumMemberAttribute(
        FooWithEnumMember queryParameter,
        string expectedQuery)
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod("QueryWithEnum");

        var output = await factory([queryParameter]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);
        await Assert.That(uri.PathAndQuery).IsEqualTo(expectedQuery);
    }

    /// <summary>The query string uses the EnumMember attribute in a type containing an enum.</summary>
    /// <param name="queryParameter">The enum value to render in the query.</param>
    /// <param name="expectedQuery">The expected resulting path and query.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [Arguments(FooWithEnumMember.A, "/query?foo=A")]
    [Arguments(FooWithEnumMember.B, "/query?foo=b")]
    public async Task QueryStringUsesEnumMemberAttributeInTypeWithEnum(
        FooWithEnumMember queryParameter,
        string expectedQuery)
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod("QueryWithTypeWithEnum");

        var output = await factory(
            [new TypeFooWithEnumMember { Foo = queryParameter }]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);
        await Assert.That(uri.PathAndQuery).IsEqualTo(expectedQuery);
    }

    /// <summary>Nullable query string parameters are rendered.</summary>
    /// <param name="expectedQuery">The expected resulting path and query.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [Arguments("/api/123?text=title&optionalId=999&foo=foo&filters=A&filters=B")]
    public async Task TestNullableQueryStringParams(string expectedQuery)
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod("QueryWithOptionalParameters");
        const int optionalId = 999;
        var output = await factory([ResourceId, TitleValue, optionalId, new Foo(), _stringArrayAb]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);
        await Assert.That(uri.PathAndQuery).IsEqualTo(expectedQuery);
    }

    /// <summary>Nullable query string parameters with a null value are rendered.</summary>
    /// <param name="expectedQuery">The expected resulting path and query.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [Arguments("/api/123?text=title&filters=A&filters=B")]
    public async Task TestNullableQueryStringParamsWithANull(string expectedQuery)
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod("QueryWithOptionalParameters");
        var output = await factory([ResourceId, TitleValue, null!, null!, _stringArrayAb]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);
        await Assert.That(uri.PathAndQuery).IsEqualTo(expectedQuery);
    }

    /// <summary>Nullable query string parameters with a null value and a path-bound object are rendered.</summary>
    /// <param name="expectedQuery">The expected resulting path and query.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [Arguments("/api/123?SomeProperty2=test&text=title&filters=A&filters=B")]
    public async Task TestNullableQueryStringParamsWithANullAndPathBoundObject(string expectedQuery)
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            "QueryWithOptionalParametersPathBoundObject");
        var output = await factory(
            [
                new PathBoundObject { SomeProperty = ResourceId, SomeProperty2 = "test" },
                TitleValue,
                null!,
                _stringArrayAb
            ]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);
        await Assert.That(uri.PathAndQuery).IsEqualTo(expectedQuery);
    }

    /// <summary>The default parameter formatter is culture invariant.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DefaultParameterFormatterIsInvariant()
    {
        var originalCulture = System.Globalization.CultureInfo.CurrentCulture;
        var originalUICulture = System.Globalization.CultureInfo.CurrentUICulture;

        try
        {
            // Spain uses a comma instead of a period for decimal values.
            var culture = new System.Globalization.CultureInfo("es-ES");
            System.Globalization.CultureInfo.CurrentCulture = culture;
            System.Globalization.CultureInfo.CurrentUICulture = culture;

            var settings = new RefitSettings();
            var fixture = new RequestBuilderImplementation<IDummyHttpApi>(settings);

            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuff");
            const double fractionalValue = 5.4;
            var output = await factory([fractionalValue]);

            var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);
            await Assert.That(uri.PathAndQuery).IsEqualTo("/foo/bar/5.4");
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = originalCulture;
            System.Globalization.CultureInfo.CurrentUICulture = originalUICulture;
        }
    }

    /// <summary>A value type can be posted as the body.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ICanPostAValueTypeIfIWantYoureNotTheBossOfMe()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.RunRequest("PostAValueType", "true");
        const int valueTypeId = 7;
        var guid = Guid.NewGuid();
        var expected = "\"" + guid + "\"";
        var output = await factory([valueTypeId, guid]);

        await Assert.That(output.SendContent).IsEqualTo(expected);
    }

    /// <summary>A delete request with a query renders correctly.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DeleteWithQuery()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod("Clear");

        var output = await factory([1]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);

        await Assert.That(uri.PathAndQuery).IsEqualTo("/api/v1/video?playerIndex=1");
    }

    /// <summary>A clear request with a query renders correctly.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ClearWithQuery()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod("ClearWithEnumMember");

        var output = await factory([FooWithEnumMember.B]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);

        await Assert.That(uri.PathAndQuery).IsEqualTo("/api/bar?foo=b");
    }

    /// <summary>A multipart post with an alias and header renders correctly.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task MultipartPostWithAliasAndHeader()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.RunRequest("UploadFile", "true");

        await using var file = MultipartTests.GetTestFileStream("Test Files/Test.pdf");

        var sp = new StreamPart(file, "aFile");

        const int companyId = 42;
        var output = await factory([companyId, "aPath", sp, "theAuth", false, "theMeta"]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestMessage!.RequestUri!);

        await Assert.That(uri.PathAndQuery).IsEqualTo("/companies/42/aPath");
        await Assert.That(output.RequestMessage.Headers.Authorization!.ToString()).IsEqualTo("theAuth");
    }

    /// <summary>A blob byte array part post with an alias renders correctly.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task PostBlobByteWithAlias()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IDummyHttpApi.Blob_Post_Byte));

        const int sampleByteCount = 10;
        var bytes = Enumerable.Range(1, sampleByteCount).Select(static i => (byte)i).ToArray();

        var bap = new ByteArrayPart(bytes, "theBytes");

        var output = await factory(["the/path", bap]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);

        await Assert.That(uri.PathAndQuery).IsEqualTo("/blobstorage/the/path");
    }

    /// <summary>A query with an alias and headers works.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task QueryWithAliasAndHeadersWorks()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IDummyHttpApi.QueryWithHeadersBeforeData));

        const string authHeader = "theAuth";
        const string langHeader = "LnG";
        const string searchParam = "theSearchParam";
        const string controlIdParam = "theControlId";
        const string secretValue = "theSecret";

        var output = await factory(
            [authHeader, langHeader, searchParam, controlIdParam, secretValue]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);

        await Assert.That(uri.PathAndQuery).IsEqualTo(
            $"/api/someModule/deviceList?controlId={controlIdParam}&search={searchParam}&secret={secretValue}");
        await Assert.That(output.Headers.GetValues("X-LnG").FirstOrDefault()).IsEqualTo(langHeader);
        await Assert.That(output.Headers.Authorization?.Scheme).IsEqualTo(authHeader);
    }

    /// <summary>The cached request builder calls the internal builder for parameters with the same names but different namespaces.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task CachedRequestBuilderCallInternalBuilderForParametersWithSameNamesButDifferentNamespaces()
    {
        var internalBuilder = new RequestBuilderMock();
        var cachedBuilder = new CachedRequestBuilderImplementation(internalBuilder);

        _ = cachedBuilder.BuildRestResultFuncForMethod(
            TestMethodName,
            [typeof(CollisionA.SomeType)]);
        _ = cachedBuilder.BuildRestResultFuncForMethod(
            TestMethodName,
            [typeof(CollisionB.SomeType)]);
        _ = cachedBuilder.BuildRestResultFuncForMethod(
            TestMethodName,
            null,
            [typeof(CollisionA.SomeType)]);
        _ = cachedBuilder.BuildRestResultFuncForMethod(
            TestMethodName,
            null,
            [typeof(CollisionB.SomeType)]);

        const int expectedCallCount = 4;
        await Assert.That(internalBuilder.CallCount).IsEqualTo(expectedCallCount);
    }

    /// <summary>A dictionary query with an enum key produces the correct query string.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DictionaryQueryWithEnumKeyProducesCorrectQueryString()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IDummyHttpApi.QueryWithDictionaryWithEnumKey));

        var dict = new Dictionary<TestEnum, string>
        {
            { TestEnum.A, Value1 },
            { TestEnum.B, Value2 },
        };

        var output = await factory([dict]);
        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);

        await Assert.That(uri.PathAndQuery).IsEqualTo("/foo?A=value1&B=value2");
    }

    /// <summary>A dictionary query with a prefix produces the correct query string.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DictionaryQueryWithPrefix()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IDummyHttpApi.QueryWithDictionaryWithPrefix));

        var dict = new Dictionary<TestEnum, string>
        {
            { TestEnum.A, Value1 },
            { TestEnum.B, Value2 },
        };

        var output = await factory([dict]);
        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);

        await Assert.That(uri.PathAndQuery).IsEqualTo("/foo?dictionary.A=value1&dictionary.B=value2");
    }

    /// <summary>A dictionary query with a numeric key produces the correct query string.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DictionaryQueryWithNumericKeyProducesCorrectQueryString()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IDummyHttpApi.QueryWithDictionaryWithNumericKey));

        const int secondNumericKey = 2;
        var dict = new Dictionary<int, string> { { 1, Value1 }, { secondNumericKey, Value2 }, };

        var output = await factory([dict]);
        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);

        await Assert.That(uri.PathAndQuery).IsEqualTo("/foo?1=value1&2=value2");
    }

    /// <summary>A dictionary query with a custom formatter produces the correct query string.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DictionaryQueryWithCustomFormatterProducesCorrectQueryString()
    {
        var urlParameterFormatter = new TestEnumUrlParameterFormatter();

        var refitSettings = new RefitSettings { UrlParameterFormatter = urlParameterFormatter };
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>(refitSettings);
        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IDummyHttpApi.QueryWithDictionaryWithEnumKey));

        var dict = new Dictionary<TestEnum, string>
        {
            { TestEnum.A, Value1 },
            { TestEnum.B, Value2 },
        };

        var output = await factory([dict]);
        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);

        await Assert.That(uri.PathAndQuery).IsEqualTo(
            $"/foo?{(int)TestEnum.A}=value1{TestEnumUrlParameterFormatter.StringParameterSuffix}&{(int)TestEnum.B}=value2{TestEnumUrlParameterFormatter.StringParameterSuffix}");
    }

    /// <summary>A complex query object with the default key formatter produces the correct query string.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ComplexQueryObjectWithDefaultKeyFormatterProducesCorrectQueryString()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IDummyHttpApi.ComplexQueryObjectWithDictionary));

        var complexQuery = new ComplexQueryObject { TestAlias2 = Value1 };

        var output = await factory([complexQuery]);
        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);

        await Assert.That(uri.PathAndQuery).IsEqualTo("/foo?TestAlias2=value1");
    }

    /// <summary>A complex query object with a custom key formatter produces the correct query string.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ComplexQueryObjectWithCustomKeyFormatterProducesCorrectQueryString()
    {
        var urlParameterKeyFormatter = new CamelCaseUrlParameterKeyFormatter();

        var refitSettings = new RefitSettings
        {
            UrlParameterKeyFormatter = urlParameterKeyFormatter
        };
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>(refitSettings);
        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IDummyHttpApi.ComplexQueryObjectWithDictionary));

        var complexQuery = new ComplexQueryObject { TestAlias2 = Value1 };

        var output = await factory([complexQuery]);
        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);

        await Assert.That(uri.PathAndQuery).IsEqualTo("/foo?testAlias2=value1");
    }

    /// <summary>A complex query object with an aliased dictionary produces the correct query string.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ComplexQueryObjectWithAliasedDictionaryProducesCorrectQueryString()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IDummyHttpApi.ComplexQueryObjectWithDictionary));

        var complexQuery = new ComplexQueryObject
        {
            TestAliasedDictionary = new()
            {
                { TestEnum.A, Value1 },
                { TestEnum.B, Value2 },
            },
        };

        var output = await factory([complexQuery]);
        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);

        await Assert.That(uri.PathAndQuery).IsEqualTo(
            "/foo?test-dictionary-alias.A=value1&test-dictionary-alias.B=value2");
    }

    /// <summary>A complex query object with a dictionary produces the correct query string.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ComplexQueryObjectWithDictionaryProducesCorrectQueryString()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IDummyHttpApi.ComplexQueryObjectWithDictionary));

        var complexQuery = new ComplexQueryObject
        {
            TestDictionary = new()
            {
                { TestEnum.A, Value1 },
                { TestEnum.B, Value2 },
            },
        };

        var output = await factory([complexQuery]);
        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);

        await Assert.That(uri.PathAndQuery).IsEqualTo("/foo?TestDictionary.A=value1&TestDictionary.B=value2");
    }

    /// <summary>A complex query object with a dictionary and a custom formatter produces the correct query string.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ComplexQueryObjectWithDictionaryAndCustomFormatterProducesCorrectQueryString()
    {
        var urlParameterFormatter = new TestEnumUrlParameterFormatter();
        var refitSettings = new RefitSettings { UrlParameterFormatter = urlParameterFormatter };
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>(refitSettings);
        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IDummyHttpApi.ComplexQueryObjectWithDictionary));

        var complexQuery = new ComplexQueryObject
        {
            TestDictionary = new()
            {
                { TestEnum.A, Value1 },
                { TestEnum.B, Value2 },
            },
        };

        var output = await factory([complexQuery]);
        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);

        var suffix = TestEnumUrlParameterFormatter.StringParameterSuffix;
        var expectedQuery =
            $"/foo?TestDictionary.{(int)TestEnum.A}=value1{suffix}&TestDictionary.{(int)TestEnum.B}=value2{suffix}";
        await Assert.That(uri.PathAndQuery).IsEqualTo(expectedQuery);
    }

    /// <summary>A mock request builder that counts invocations.</summary>
    private sealed class RequestBuilderMock : IRequestBuilder
    {
        /// <summary>Gets the number of times the builder was invoked.</summary>
        public int CallCount { get; private set; }

        /// <inheritdoc/>
        public RefitSettings Settings { get; } = new();

        /// <summary>Records the invocation and returns null.</summary>
        /// <param name="methodName">The name of the method being built.</param>
        /// <param name="parameterTypes">The parameter types of the method, if any.</param>
        /// <param name="genericArgumentTypes">The generic argument types of the method, if any.</param>
        /// <returns>Always returns null for this mock.</returns>
        public Func<HttpClient, object[], object?> BuildRestResultFuncForMethod(
            string methodName,
            Type[]? parameterTypes = null,
            Type[]? genericArgumentTypes = null)
        {
            CallCount++;
            return null!;
        }
    }
}
