// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net.Http;
using Refit.Testing;

namespace Refit.Tests;

/// <summary>Tests that the reflection-based Refit implementation passes the expected attribute providers and types to the URL parameter formatter.</summary>
public sealed class ReflectionTests
{
    /// <summary>The base address supplied to the reflection-based Refit service.</summary>
    private const string BaseUrl = "https://foo";

    /// <summary>The base address with a trailing slash used for expected request URLs.</summary>
    private const string BaseUrlWithSlash = "https://foo/";

    /// <summary>The query value exercised by the query parameter tests.</summary>
    private const string QueryValue = "queryValue";

    /// <summary>Verifies a simple string path parameter is formatted with the expected metadata.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task UrlParameterShouldBeExpectedReflection()
    {
        var handler = new StubHttp
        {
            {
                Route.Get("https://foo/bar"),
                Reply.Json(nameof(IBasicApi.GetParam))
            },
        };

        var methodInfo = typeof(IBasicApi).GetMethod(nameof(IBasicApi.GetParam))!;
        var parameterInfo = methodInfo.GetParameters()[0];

        var formatter = new TestUrlFormatter(parameterInfo, typeof(string));
        var service = handler.CreateClient<IBasicApi>(BaseUrl, new RefitSettings { UrlParameterFormatter = formatter });

        await service.GetParam("bar");
        await formatter.AssertNoOutstandingAssertions();
    }

    /// <summary>Verifies a derived record path parameter is formatted with the expected metadata.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task DerivedUrlParameterShouldBeExpectedReflection()
    {
        var handler = new StubHttp
        {
            {
                Route.Get("https://foo/DerivedRecord%20%7B%20Value%20%3D%20Derived%20%7D"),
                Reply.Json(nameof(IBasicApi.GetDerivedParam))
            },
        };

        var methodInfo = typeof(IBasicApi).GetMethod(nameof(IBasicApi.GetDerivedParam))!;
        var parameterInfo = methodInfo.GetParameters()[0];

        var formatter = new TestUrlFormatter(parameterInfo, typeof(BaseRecord));
        var service = handler.CreateClient<IBasicApi>(BaseUrl, new RefitSettings { UrlParameterFormatter = formatter });

        await service.GetDerivedParam(new DerivedRecord("Derived"));
        await formatter.AssertNoOutstandingAssertions();
    }

    /// <summary>Verifies a record property path parameter is formatted with the expected metadata.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task PropertyParameterShouldBeExpectedReflection()
    {
        var handler = new StubHttp
        {
            {
                Route.Get("https://foo/propVal"),
                Reply.Json(nameof(IBasicApi.GetPropertyParam))
            },
        };

        var propertyInfo = typeof(MyParams).GetProperties()[0];

        var formatter = new TestUrlFormatter(propertyInfo, typeof(string));
        var service = handler.CreateClient<IBasicApi>(BaseUrl, new RefitSettings { UrlParameterFormatter = formatter });

        await service.GetPropertyParam(new("propVal"));
        await formatter.AssertNoOutstandingAssertions();
    }

    /// <summary>Verifies a generic path parameter is formatted with the expected metadata.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task GenericParameterShouldBeExpectedReflection()
    {
        var handler = new StubHttp
        {
            {
                Route.Get("https://foo/genericVal"),
                Reply.Json(nameof(IBasicApi.GetGenericParam))
            },
        };

        var methodInfo = typeof(IBasicApi).GetMethod(nameof(IBasicApi.GetGenericParam))!;
        var stringMethod = methodInfo.MakeGenericMethod(typeof(string));
        var parameterInfo = stringMethod.GetParameters()[0];

        var formatter = new TestUrlFormatter(parameterInfo, typeof(string));
        var service = handler.CreateClient<IBasicApi>(BaseUrl, new RefitSettings { UrlParameterFormatter = formatter });

        await service.GetGenericParam("genericVal");
        await formatter.AssertNoOutstandingAssertions();
    }

    /// <summary>Verifies a simple string query parameter is formatted with the expected metadata.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task QueryParameterShouldBeExpectedReflection()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = BaseUrlWithSlash, ExactQueryParams = [("queryKey", QueryValue)] },
                Reply.Json(nameof(IBasicApi.GetQuery))
            },
        };

        var methodInfo = typeof(IBasicApi).GetMethod(nameof(IBasicApi.GetQuery))!;
        var parameterInfo = methodInfo.GetParameters()[0];

        var formatter = new TestUrlFormatter(parameterInfo, typeof(string));
        var service = handler.CreateClient<IBasicApi>(BaseUrl, new RefitSettings { UrlParameterFormatter = formatter });

        await service.GetQuery(QueryValue);
        await formatter.AssertNoOutstandingAssertions();
    }

    /// <summary>Verifies a record property query parameter is formatted with the expected metadata.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task QueryPropertyParameterShouldBeExpectedReflection()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = BaseUrlWithSlash, ExactQueryParams = [("Value", "queryVal")] },
                Reply.Json(nameof(IBasicApi.GetPropertyQuery))
            },
        };

        var methodInfo = typeof(IBasicApi).GetMethod(nameof(IBasicApi.GetPropertyQuery))!;
        var parameterInfo = methodInfo.GetParameters()[0];

        var formatter = new TestUrlFormatter(parameterInfo, typeof(BaseRecord));
        var service = handler.CreateClient<IBasicApi>(BaseUrl, new RefitSettings { UrlParameterFormatter = formatter });

        await service.GetPropertyQuery(new("queryVal"));
        await formatter.AssertNoOutstandingAssertions();
    }

    /// <summary>Verifies a derived record's properties are formatted as query parameters with the expected metadata.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task DerivedQueryPropertyParameterShouldBeExpectedReflection()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = BaseUrlWithSlash, ExactQueryParams = [("Name", "queryName"), ("Value", "value")] },
                Reply.Json(nameof(IBasicApi.GetPropertyQuery))
            },
        };

        var methodInfo = typeof(IBasicApi).GetMethod(nameof(IBasicApi.GetPropertyQuery))!;
        var parameterInfo = methodInfo.GetParameters()[0];

        var formatter = new TestUrlFormatter(
            [parameterInfo, parameterInfo],
            [typeof(BaseRecord), typeof(BaseRecord)]);
        var service = handler.CreateClient<IBasicApi>(BaseUrl, new RefitSettings { UrlParameterFormatter = formatter });

        await service.GetPropertyQuery(new DerivedRecordWithProperty("queryName"));
        await formatter.AssertNoOutstandingAssertions();
    }

    /// <summary>Verifies a generic query parameter is formatted with the expected metadata.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task GenericQueryParameterShouldBeExpectedReflection()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = BaseUrlWithSlash, ExactQueryParams = [("queryKey", QueryValue)] },
                Reply.Json(nameof(IBasicApi.GetGenericQuery))
            },
        };

        var methodInfo = typeof(IBasicApi).GetMethod(nameof(IBasicApi.GetGenericQuery))!;
        var stringMethod = methodInfo.MakeGenericMethod(typeof(string));
        var parameterInfo = stringMethod.GetParameters()[0];

        var formatter = new TestUrlFormatter(parameterInfo, typeof(string));
        var service = handler.CreateClient<IBasicApi>(BaseUrl, new RefitSettings { UrlParameterFormatter = formatter });

        await service.GetGenericQuery(QueryValue);
        await formatter.AssertNoOutstandingAssertions();
    }

    /// <summary>Verifies an enumerable query parameter is formatted with the expected metadata.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task EnumerableQueryParameterShouldBeExpectedReflection()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = BaseUrlWithSlash, ExactQueryParams = [("enums", "k0,k1")] },
                Reply.Json(nameof(IBasicApi.GetEnumerableQuery))
            },
        };

        var methodInfo = typeof(IBasicApi).GetMethod(nameof(IBasicApi.GetEnumerableQuery))!;
        var parameterInfo = methodInfo.GetParameters()[0];

        var formatter = new TestUrlFormatter(
            [parameterInfo, parameterInfo],
            [typeof(IEnumerable<string>), typeof(IEnumerable<string>)]);
        var service = handler.CreateClient<IBasicApi>(BaseUrl, new RefitSettings { UrlParameterFormatter = formatter });

        await service.GetEnumerableQuery(["k0", "k1"]);
        await formatter.AssertNoOutstandingAssertions();
    }

    /// <summary>Verifies a record's enumerable property is formatted as a query parameter with the expected metadata.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task EnumerablePropertyQueryParameterShouldBeExpectedReflection()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = BaseUrlWithSlash, ExactQueryParams = [("Enumerable", "0,1")] },
                Reply.Json(nameof(IBasicApi.GetEnumerablePropertyQuery))
            },
        };

        var methodInfo = typeof(IBasicApi).GetMethod(nameof(IBasicApi.GetEnumerablePropertyQuery))!;
        var parameterInfo = methodInfo.GetParameters()[0];
        var propertyInfo = typeof(MyEnumerableParams).GetProperties()[0];

        var formatter = new TestUrlFormatter(
            [propertyInfo, propertyInfo, parameterInfo],
            [typeof(int[]), typeof(int[]), typeof(MyEnumerableParams)]);
        var service = handler.CreateClient<IBasicApi>(BaseUrl, new RefitSettings { UrlParameterFormatter = formatter });

        await service.GetEnumerablePropertyQuery(new([0, 1]));
        await formatter.AssertNoOutstandingAssertions();
    }

    /// <summary>Verifies a dictionary query parameter is formatted with the expected metadata.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task QueryDictionaryParameterShouldBeExpectedReflection()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = BaseUrlWithSlash, ExactQueryParams = [("key0", "1"), ("key1", "2")] },
                Reply.Json(nameof(IBasicApi.GetDictionaryQuery))
            },
        };

        var methodInfo = typeof(IBasicApi).GetMethod(nameof(IBasicApi.GetDictionaryQuery))!;
        var parameterInfo = methodInfo.GetParameters()[0];

        var formatter = new TestUrlFormatter(
            [typeof(string), typeof(string), parameterInfo, parameterInfo],
            [
                typeof(string),
                typeof(string),
                typeof(IDictionary<string, object>),
                typeof(IDictionary<string, object>)
            ]);
        var service = handler.CreateClient<IBasicApi>(BaseUrl, new RefitSettings { UrlParameterFormatter = formatter });

        var dict = new Dictionary<string, object> { { "key0", 1 }, { "key1", 2 } };
        await service.GetDictionaryQuery(dict);
        await formatter.AssertNoOutstandingAssertions();
    }
}
