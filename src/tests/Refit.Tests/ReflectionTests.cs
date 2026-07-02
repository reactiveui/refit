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

    /// <summary>A Refit interface with many overloaded generic methods.</summary>
    public interface IGenericMethod
    {
        /// <summary>Request with a generic route parameter.</summary>
        /// <param name="value1">Generic route parameter.</param>
        /// <typeparam name="T1">Generic type of route parameter.</typeparam>
        /// <returns>A task that completes when the request finishes.</returns>
        [Get("/{value1}")]
        Task GetGenericParameter<T1>(T1 value1);

        /// <summary>Request with a generic route parameter and additional parameter.</summary>
        /// <param name="value1">Generic route parameter.</param>
        /// <param name="value2">Integer route parameter.</param>
        /// <typeparam name="T1">Generic type of route parameter.</typeparam>
        /// <returns>A task that completes when the request finishes.</returns>
        [Get("/{value1}/{value2}")]
        Task GetGenericParameter<T1>(T1 value1, int value2);

        /// <summary>Request with two generic route parameters.</summary>
        /// <param name="value1">First generic route parameter.</param>
        /// <param name="value2">Second generic route parameter.</param>
        /// <typeparam name="T1">Generic type of first route parameter.</typeparam>
        /// <typeparam name="T2">Generic type of second route parameter.</typeparam>
        /// <returns>A task that completes when the request finishes.</returns>
        [Get("/{value1}/{value2}")]
        Task GetGenericParameter<T1, T2>(T1 value1, T2 value2);
    }

    /// <summary>A generic Refit interface with many overloaded generic methods.</summary>
    /// <typeparam name="TInterface">Generic type of interface.</typeparam>
    public interface IGenericMethodAndInterface<TInterface>
    {
        /// <summary>Request with a generic route parameter.</summary>
        /// <param name="value1">Generic route parameter.</param>
        /// <typeparam name="T1">Generic type of route parameter.</typeparam>
        /// <returns>A task that completes when the request finishes.</returns>
        [Get("/{value1}")]
        Task GetGenericParameter<T1>(T1 value1);

        /// <summary>Request with a generic route parameter and additional parameter.</summary>
        /// <param name="value1">Generic route parameter.</param>
        /// <param name="value2">Integer route parameter.</param>
        /// <typeparam name="T1">Generic type of route parameter.</typeparam>
        /// <returns>A task that completes when the request finishes.</returns>
        [Get("/{value1}/{value2}")]
        Task GetGenericParameter<T1>(T1 value1, int value2);

        /// <summary>Request with two generic route parameters.</summary>
        /// <param name="value1">First generic route parameter.</param>
        /// <param name="value2">Second generic route parameter.</param>
        /// <typeparam name="T1">Generic type of first route parameter.</typeparam>
        /// <returns>A task that completes when the request finishes.</returns>
        [Get("/{value1}/{value2}")]
        Task GetGenericParameter<T1>(T1 value1, TInterface value2);
    }

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

    /// <summary>Verifies an overloaded generic path parameter is formatted with the expected metadata.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task OverloadedGenericParameterShouldBeExpectedReflection()
    {
        _ = _mockHandler.Fallback
            .Respond("application/json", nameof(IGenericMethod.GetGenericParameter));

        var methodInfo = typeof(IGenericMethod).GetMethod(nameof(IGenericMethod.GetGenericParameter), 1, [Type.MakeGenericMethodParameter(0)])!;
        var parameterInfo = methodInfo.MakeGenericMethod(typeof(string)).GetParameters()[0];

        var formatter = new TestUrlFormatter(
            [parameterInfo],
            [typeof(string)]);
        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => _mockHandler,
            UrlParameterFormatter = formatter
        };
        var service = RestService.For<IGenericMethod>("https://foo", settings);

        await service.GetGenericParameter("Empty");
        await formatter.AssertNoOutstandingAssertions();
    }

    /// <summary>Verifies an overloaded generic path parameter is formatted with the expected metadata.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task OverloadedGenericParameterWithNonGenericShouldBeExpectedReflection()
    {
        _ = _mockHandler.Fallback
            .Respond("application/json", nameof(IGenericMethod.GetGenericParameter));

        var methodInfo = typeof(IGenericMethod).GetMethod(nameof(IGenericMethod.GetGenericParameter), 1, [Type.MakeGenericMethodParameter(0), typeof(int)])!.MakeGenericMethod(typeof(string));
        var parameterInfo1 = methodInfo.GetParameters()[0];
        var parameterInfo2 = methodInfo.GetParameters()[1];

        var formatter = new TestUrlFormatter(
            [parameterInfo1, parameterInfo2],
            [typeof(string), typeof(int)]);
        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => _mockHandler,
            UrlParameterFormatter = formatter
        };
        var service = RestService.For<IGenericMethod>("https://foo", settings);

        await service.GetGenericParameter("Empty", 10);
        await formatter.AssertNoOutstandingAssertions();
    }

    /// <summary>Verifies an overloaded generic path parameter is formatted with the expected metadata.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task OverloadedWithTwoGenericParameterShouldBeExpectedReflection()
    {
        _ = _mockHandler.Fallback
            .Respond("application/json", nameof(IGenericMethod.GetGenericParameter));

        var methodInfo = typeof(IGenericMethod).GetMethod(nameof(IGenericMethod.GetGenericParameter), 2, [Type.MakeGenericMethodParameter(0), Type.MakeGenericMethodParameter(1)])!;
        methodInfo = methodInfo.MakeGenericMethod(typeof(string), typeof(long));
        var parameterInfo1 = methodInfo.GetParameters()[0];
        var parameterInfo2 = methodInfo.GetParameters()[1];

        var formatter = new TestUrlFormatter(
            [parameterInfo1, parameterInfo2],
            [typeof(string), typeof(long)]);
        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => _mockHandler,
            UrlParameterFormatter = formatter
        };
        var service = RestService.For<IGenericMethod>("https://foo", settings);

        await service.GetGenericParameter("Empty", 10L);
        await formatter.AssertNoOutstandingAssertions();
    }

    /// <summary>Verifies an overloaded generic path parameter is formatted with the expected metadata.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task GenericOverloadedInterfaceWithGenericParameterShouldBeExpectedReflection()
    {
        _ = _mockHandler.Fallback
            .Respond("application/json", nameof(IGenericMethodAndInterface<>.GetGenericParameter));

        var methodInfo = typeof(IGenericMethodAndInterface<string>).GetMethod(nameof(IGenericMethodAndInterface<>.GetGenericParameter), 1, [Type.MakeGenericMethodParameter(0)])!;
        methodInfo = methodInfo.MakeGenericMethod(typeof(int));
        var parameterInfo1 = methodInfo.GetParameters()[0];

        var formatter = new TestUrlFormatter(
            [parameterInfo1],
            [typeof(int)]);
        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => _mockHandler,
            UrlParameterFormatter = formatter
        };
        var service = RestService.For<IGenericMethodAndInterface<string>>("https://foo", settings);

        await service.GetGenericParameter(10);
        await formatter.AssertNoOutstandingAssertions();
    }

    /// <summary>Verifies an overloaded generic path parameter is formatted with the expected metadata.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task GenericOverloadedInterfaceWithNonGenericShouldBeExpectedReflection()
    {
        _ = _mockHandler.Fallback
            .Respond("application/json", nameof(IGenericMethodAndInterface<>.GetGenericParameter));

        var methodInfo = typeof(IGenericMethodAndInterface<string>).GetMethod(nameof(IGenericMethodAndInterface<>.GetGenericParameter), 1, [Type.MakeGenericMethodParameter(0), typeof(int)])!;
        methodInfo = methodInfo.MakeGenericMethod(typeof(long));
        var parameterInfo1 = methodInfo.GetParameters()[0];
        var parameterInfo2 = methodInfo.GetParameters()[1];

        var formatter = new TestUrlFormatter(
            [parameterInfo1, parameterInfo2],
            [typeof(long), typeof(int)]);
        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => _mockHandler,
            UrlParameterFormatter = formatter
        };
        var service = RestService.For<IGenericMethodAndInterface<string>>("https://foo", settings);

        await service.GetGenericParameter(10L, 10);
        await formatter.AssertNoOutstandingAssertions();
    }

    /// <summary>Verifies an overloaded generic path parameter is formatted with the expected metadata.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task GenericOverloadedInterfaceWithTwoGenericParameterShouldBeExpectedReflection()
    {
        _ = _mockHandler.Fallback
            .Respond("application/json", nameof(IGenericMethodAndInterface<>.GetGenericParameter));

        var methodInfo = typeof(IGenericMethodAndInterface<string>).GetMethod(
            nameof(IGenericMethodAndInterface<>.GetGenericParameter),
            1,
            [Type.MakeGenericMethodParameter(0), typeof(string)])!;
        methodInfo = methodInfo.MakeGenericMethod(typeof(long));
        var parameterInfo1 = methodInfo.GetParameters()[0];
        var parameterInfo2 = methodInfo.GetParameters()[1];

        var formatter = new TestUrlFormatter(
            [parameterInfo1, parameterInfo2],
            [typeof(long), typeof(string)]);
        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => _mockHandler,
            UrlParameterFormatter = formatter
        };
        var service = RestService.For<IGenericMethodAndInterface<string>>("https://foo", settings);

        await service.GetGenericParameter(10L, "Empty");
        await formatter.AssertNoOutstandingAssertions();
    }

    /// <summary>Disposes the mock HTTP handler owned by the test.</summary>
    public void Dispose() => _mockHandler?.Dispose();
}
