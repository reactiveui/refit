// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net.Http;
using RichardSzalay.MockHttp;

namespace Refit.Tests;

/// <summary>Tests that the reflection-based Refit implementation passes the expected attribute providers and types to the URL parameter formatter.</summary>
public sealed class ReflectionTests : IDisposable
{
    /// <summary>The mock HTTP message handler used to intercept and assert outgoing requests.</summary>
    private readonly MockHttpMessageHandler _mockHandler = new();

    /// <summary>Verifies a simple string path parameter is formatted with the expected metadata.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task UrlParameterShouldBeExpectedReflection()
    {
        _ = _mockHandler
            .Expect(HttpMethod.Get, "https://foo/bar")
            .Respond("application/json", nameof(IBasicApi.GetParam));

        var methodInfo = typeof(IBasicApi).GetMethod(nameof(IBasicApi.GetParam))!;
        var parameterInfo = methodInfo.GetParameters()[0];

        var formatter = new TestUrlFormatter(parameterInfo, typeof(string));
        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => _mockHandler,
            UrlParameterFormatter = formatter
        };
        var service = RestService.For<IBasicApi>("https://foo", settings);

        await service.GetParam("bar");
        await formatter.AssertNoOutstandingAssertions();
    }

    /// <summary>Verifies a derived record path parameter is formatted with the expected metadata.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task DerivedUrlParameterShouldBeExpectedReflection()
    {
        _ = _mockHandler
            .Expect(HttpMethod.Get, "https://foo/DerivedRecord%20%7B%20Value%20%3D%20Derived%20%7D")
            .Respond("application/json", nameof(IBasicApi.GetDerivedParam));

        var methodInfo = typeof(IBasicApi).GetMethod(nameof(IBasicApi.GetDerivedParam))!;
        var parameterInfo = methodInfo.GetParameters()[0];

        var formatter = new TestUrlFormatter(parameterInfo, typeof(BaseRecord));
        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => _mockHandler,
            UrlParameterFormatter = formatter
        };
        var service = RestService.For<IBasicApi>("https://foo", settings);

        await service.GetDerivedParam(new DerivedRecord("Derived"));
        await formatter.AssertNoOutstandingAssertions();
    }

    /// <summary>Verifies a record property path parameter is formatted with the expected metadata.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task PropertyParameterShouldBeExpectedReflection()
    {
        _ = _mockHandler
            .Expect(HttpMethod.Get, "https://foo/propVal")
            .Respond("application/json", nameof(IBasicApi.GetPropertyParam));

        var propertyInfo = typeof(MyParams).GetProperties()[0];

        var formatter = new TestUrlFormatter(propertyInfo, typeof(string));
        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => _mockHandler,
            UrlParameterFormatter = formatter
        };
        var service = RestService.For<IBasicApi>("https://foo", settings);

        await service.GetPropertyParam(new("propVal"));
        await formatter.AssertNoOutstandingAssertions();
    }

    /// <summary>Verifies a generic path parameter is formatted with the expected metadata.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task GenericParameterShouldBeExpectedReflection()
    {
        _ = _mockHandler
            .Expect(HttpMethod.Get, "https://foo/genericVal")
            .Respond("application/json", nameof(IBasicApi.GetGenericParam));

        var methodInfo = typeof(IBasicApi).GetMethod(nameof(IBasicApi.GetGenericParam))!;
        var stringMethod = methodInfo.MakeGenericMethod(typeof(string));
        var parameterInfo = stringMethod.GetParameters()[0];

        var formatter = new TestUrlFormatter(parameterInfo, typeof(string));
        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => _mockHandler,
            UrlParameterFormatter = formatter
        };
        var service = RestService.For<IBasicApi>("https://foo", settings);

        await service.GetGenericParam("genericVal");
        await formatter.AssertNoOutstandingAssertions();
    }

    /// <summary>Verifies a simple string query parameter is formatted with the expected metadata.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task QueryParameterShouldBeExpectedReflection()
    {
        _ = _mockHandler
            .Expect(HttpMethod.Get, "https://foo/")
            .WithExactQueryString(
                [new("queryKey", "queryValue")])
            .Respond("application/json", nameof(IBasicApi.GetQuery));

        var methodInfo = typeof(IBasicApi).GetMethod(nameof(IBasicApi.GetQuery))!;
        var parameterInfo = methodInfo.GetParameters()[0];

        var formatter = new TestUrlFormatter(parameterInfo, typeof(string));
        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => _mockHandler,
            UrlParameterFormatter = formatter
        };
        var service = RestService.For<IBasicApi>("https://foo", settings);

        await service.GetQuery("queryValue");
        await formatter.AssertNoOutstandingAssertions();
    }

    /// <summary>Verifies a record property query parameter is formatted with the expected metadata.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task QueryPropertyParameterShouldBeExpectedReflection()
    {
        _ = _mockHandler
            .Expect(HttpMethod.Get, "https://foo/")
            .WithExactQueryString([new("Value", "queryVal")])
            .Respond("application/json", nameof(IBasicApi.GetPropertyQuery));

        var methodInfo = typeof(IBasicApi).GetMethod(nameof(IBasicApi.GetPropertyQuery))!;
        var parameterInfo = methodInfo.GetParameters()[0];

        var formatter = new TestUrlFormatter(parameterInfo, typeof(BaseRecord));
        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => _mockHandler,
            UrlParameterFormatter = formatter
        };
        var service = RestService.For<IBasicApi>("https://foo", settings);

        await service.GetPropertyQuery(new("queryVal"));
        await formatter.AssertNoOutstandingAssertions();
    }

    /// <summary>Verifies a derived record's properties are formatted as query parameters with the expected metadata.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task DerivedQueryPropertyParameterShouldBeExpectedReflection()
    {
        _ = _mockHandler
            .Expect(HttpMethod.Get, "https://foo/")
            .WithExactQueryString(
                [
                    new("Name", "queryName"),
                    new("Value", "value"),
                ])
            .Respond("application/json", nameof(IBasicApi.GetPropertyQuery));

        var methodInfo = typeof(IBasicApi).GetMethod(nameof(IBasicApi.GetPropertyQuery))!;
        var parameterInfo = methodInfo.GetParameters()[0];

        var formatter = new TestUrlFormatter(
            [parameterInfo, parameterInfo],
            [typeof(BaseRecord), typeof(BaseRecord)]);
        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => _mockHandler,
            UrlParameterFormatter = formatter
        };
        var service = RestService.For<IBasicApi>("https://foo", settings);

        await service.GetPropertyQuery(new DerivedRecordWithProperty("queryName"));
        await formatter.AssertNoOutstandingAssertions();
    }

    /// <summary>Verifies a generic query parameter is formatted with the expected metadata.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task GenericQueryParameterShouldBeExpectedReflection()
    {
        _ = _mockHandler
            .Expect(HttpMethod.Get, "https://foo/")
            .WithExactQueryString(
                [new("queryKey", "queryValue")])
            .Respond("application/json", nameof(IBasicApi.GetGenericQuery));

        var methodInfo = typeof(IBasicApi).GetMethod(nameof(IBasicApi.GetGenericQuery))!;
        var stringMethod = methodInfo.MakeGenericMethod(typeof(string));
        var parameterInfo = stringMethod.GetParameters()[0];

        var formatter = new TestUrlFormatter(parameterInfo, typeof(string));
        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => _mockHandler,
            UrlParameterFormatter = formatter
        };
        var service = RestService.For<IBasicApi>("https://foo", settings);

        await service.GetGenericQuery("queryValue");
        await formatter.AssertNoOutstandingAssertions();
    }

    /// <summary>Verifies an enumerable query parameter is formatted with the expected metadata.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task EnumerableQueryParameterShouldBeExpectedReflection()
    {
        _ = _mockHandler
            .Expect(HttpMethod.Get, "https://foo/")
            .WithExactQueryString([new("enums", "k0,k1")])
            .Respond("application/json", nameof(IBasicApi.GetEnumerableQuery));

        var methodInfo = typeof(IBasicApi).GetMethod(nameof(IBasicApi.GetEnumerableQuery))!;
        var parameterInfo = methodInfo.GetParameters()[0];

        var formatter = new TestUrlFormatter(
            [parameterInfo, parameterInfo],
            [typeof(IEnumerable<string>), typeof(IEnumerable<string>)]);
        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => _mockHandler,
            UrlParameterFormatter = formatter
        };
        var service = RestService.For<IBasicApi>("https://foo", settings);

        await service.GetEnumerableQuery(["k0", "k1"]);
        await formatter.AssertNoOutstandingAssertions();
    }

    /// <summary>Verifies a record's enumerable property is formatted as a query parameter with the expected metadata.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task EnumerablePropertyQueryParameterShouldBeExpectedReflection()
    {
        _ = _mockHandler
            .Expect(HttpMethod.Get, "https://foo/")
            .WithExactQueryString([new("Enumerable", "0,1")])
            .Respond("application/json", nameof(IBasicApi.GetEnumerablePropertyQuery));

        var methodInfo = typeof(IBasicApi).GetMethod(nameof(IBasicApi.GetEnumerablePropertyQuery))!;
        var parameterInfo = methodInfo.GetParameters()[0];
        var propertyInfo = typeof(MyEnumerableParams).GetProperties()[0];

        var formatter = new TestUrlFormatter(
            [propertyInfo, propertyInfo, parameterInfo],
            [typeof(int[]), typeof(int[]), typeof(MyEnumerableParams)]);
        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => _mockHandler,
            UrlParameterFormatter = formatter
        };
        var service = RestService.For<IBasicApi>("https://foo", settings);

        await service.GetEnumerablePropertyQuery(new([0, 1]));
        await formatter.AssertNoOutstandingAssertions();
    }

    /// <summary>Verifies a dictionary query parameter is formatted with the expected metadata.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task QueryDictionaryParameterShouldBeExpectedReflection()
    {
        _ = _mockHandler
            .Expect(HttpMethod.Get, "https://foo/")
            .WithExactQueryString(
                [
                    new("key0", "1"),
                    new("key1", "2"),
                ])
            .Respond("application/json", nameof(IBasicApi.GetDictionaryQuery));

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
        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => _mockHandler,
            UrlParameterFormatter = formatter
        };
        var service = RestService.For<IBasicApi>("https://foo", settings);

        var dict = new Dictionary<string, object> { { "key0", 1 }, { "key1", 2 } };
        await service.GetDictionaryQuery(dict);
        await formatter.AssertNoOutstandingAssertions();
    }

    /// <summary>Disposes the mock HTTP handler owned by the test.</summary>
    public void Dispose() => _mockHandler?.Dispose();
}
