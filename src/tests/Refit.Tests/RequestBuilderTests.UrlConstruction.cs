// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Refit.Tests;

/// <summary>Tests for <see cref="RequestBuilderImplementation{T}"/> URL path and query-string construction.</summary>
public partial class RequestBuilderTests
{
    /// <summary>A hardcoded query parameter appears in the URL.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task HardcodedQueryParamShouldBeInUrl()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            "FetchSomeStuffWithHardcodedQueryParameter");
        var output = await factory([SampleId]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);
        await Assert.That(uri.PathAndQuery).IsEqualTo("/foo/bar/6?baz=bamf");
    }

    /// <summary>Parameterized query parameters appear in the URL.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ParameterizedQueryParamsShouldBeInUrl()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            "FetchSomeStuffWithHardcodedAndOtherQueryParameters");
        var output = await factory([SampleId, "foo"]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);
        await Assert.That(uri.PathAndQuery).IsEqualTo("/foo/bar/6?baz=bamf&search_for=foo");
    }

    /// <summary>A parameter that appears more than once is rendered each time.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ParameterizedValuesShouldBeInUrlMoreThanOnce()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IDummyHttpApi.SomeApiThatUsesParameterMoreThanOnceInTheUrl));
        var output = await factory([SampleId]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);
        await Assert.That(uri.PathAndQuery).IsEqualTo("/api/foo/6/file_6?query=6");
    }

    /// <summary>Round-tripping parameterized query parameters appear in the URL.</summary>
    /// <param name="path">The path segment value to round-trip.</param>
    /// <param name="expectedQuery">The expected resulting path and query.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [Arguments("aaa/bbb", "/foo/bar/aaa/bbb/1")]
    [Arguments("aaa/bbb/ccc", "/foo/bar/aaa/bbb/ccc/1")]
    [Arguments("aaa", "/foo/bar/aaa/1")]
    [Arguments("aa a/bb-b", "/foo/bar/aa%20a/bb-b/1")]
    public async Task RoundTrippingParameterizedQueryParamsShouldBeInUrl(
        string path,
        string expectedQuery)
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();

        var factory = fixture.BuildRequestFactoryForMethod(
            "FetchSomeStuffWithRoundTrippingParam");
        var output = await factory([path, 1]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);
        await Assert.That(uri.PathAndQuery).IsEqualTo(expectedQuery);
    }

    /// <summary>Null query parameters render as blank in the URL.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [UnconditionalSuppressMessage(
        "SingleFile",
        "IL3000:Avoid accessing Assembly file path when publishing as a single file",
        Justification = "Test reads the on-disk assembly path to build a FileInfo argument; never run as a single-file app.")]
    public async Task ParameterizedNullQueryParamsShouldBeBlankInUrl()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod("PostWithQueryStringParameters");
        var output = await factory(
            [new FileInfo(typeof(RequestBuilderTests).Assembly.Location), null!]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);
        await Assert.That(uri.PathAndQuery).IsEqualTo("/foo?name=");
    }

    /// <summary>Parameters are put as an explicit query string.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ParametersShouldBePutAsExplicitQueryString()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IDummyHttpApi.QueryWithExplicitParameters));
        var output = await factory(["value1", "value2"]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);

        await Assert.That(uri.PathAndQuery).IsEqualTo("/query?q1=value1&q2=value2");
    }

    /// <summary>A query parameter is formatted using its format attribute.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task QueryParamShouldFormat()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithQueryFormat");
        var output = await factory([SampleId]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);
        await Assert.That(uri.PathAndQuery).IsEqualTo("/foo/bar/6.0");
    }

    /// <summary>Parameterized query parameters appear in the URL with values encoded.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ParameterizedQueryParamsShouldBeInUrlAndValuesEncoded()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            "FetchSomeStuffWithHardcodedAndOtherQueryParameters");
        var output = await factory([SampleId, "push!=pull&push"]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);

        await Assert.That(uri.PathAndQuery).IsEqualTo("/foo/bar/6?baz=bamf&search_for=push%21%3Dpull%26push");
    }

    /// <summary>Mixed replacement and query values are encoded in the URL.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ParameterizedQueryParamsShouldBeInUrlAndValuesEncodedWhenMixedReplacementAndQuery()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            FetchVoidQueryAliasMethodName);
        var output = await factory(["6 & 7/8", ExampleEmailValue, PushNotPullValue]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);

        await Assert.That(uri.PathAndQuery).IsEqualTo("/void/6%20%26%207%2F8/path?a=test%40example.com&b=push%21%3Dpull");
    }

    /// <summary>Query parameters with a path delimiter are encoded.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task QueryParamWithPathDelimiterShouldBeEncoded()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            FetchVoidQueryAliasMethodName);
        var output = await factory(["6/6", ExampleEmailValue, PushNotPullValue]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);

        await Assert.That(uri.PathAndQuery).IsEqualTo("/void/6%2F6/path?a=test%40example.com&b=push%21%3Dpull");
    }

    /// <summary>Query parameters ending in double quotes are not truncated.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task QueryParamWhichEndsInDoubleQuotesShouldNotBeTruncated()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            "FetchSomeStuffWithDoubleQuotesInUrl");
        var output = await factory([AlternateSampleId]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);

        await Assert.That(uri.PathAndQuery).IsEqualTo("/foo?q=app_metadata.id%3A%2242%22");
    }

    /// <summary>Captures the last character when a route ends with a constant.</summary>
    /// <param name="methodToTest">The name of the interface method to build.</param>
    /// <param name="constantChar">The trailing constant character expected in the URL.</param>
    /// <param name="contains">The substring expected to appear in the URL.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [Arguments("GetWithTrainingParenthesis", ")", "/foo/bar/(1)")]
    [Arguments("GetWithTrailingSlash", "/", "/foo/bar/1/")]
    public async Task ShouldCaptureLastCharacterWhenRouteEndsWithConstant(string methodToTest, string constantChar, string contains)
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            methodToTest);
        var output = await factory(["1"]);

        var uri = new Uri(new(ApiBaseUrlWithSlash), output.RequestUri!);

        await Assert.That(uri.PathAndQuery).EndsWith(constantChar);
        await Assert.That(uri.PathAndQuery).Contains(contains);
    }

    /// <summary>Mixed replacement and query values are encoded for a bad id.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ParameterizedQueryParamsShouldBeInUrlAndValuesEncodedWhenMixedReplacementAndQueryBadId()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            FetchVoidQueryAliasMethodName);
        var output = await factory(["6", ExampleEmailValue, PushNotPullValue]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);

        await Assert.That(uri.PathAndQuery).IsEqualTo("/void/6/path?a=test%40example.com&b=push%21%3Dpull");
    }

    /// <summary>Non-formattable query parameters are included in the URL.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task NonFormattableQueryParamsShouldBeIncluded()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            "FetchSomeStuffWithNonFormattableQueryParams");
        var output = await factory([true, 'x']);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);

        await Assert.That(uri.PathAndQuery).IsEqualTo("/foo?b=True&c=x");
    }

    /// <summary>Multiple parameters in the same segment are generated correctly.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task MultipleParametersInTheSameSegmentAreGeneratedProperly()
    {
        const int segmentWidth = 1024;
        const int segmentHeight = 768;
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            "FetchSomethingWithMultipleParametersPerSegment");
        var output = await factory([SampleId, segmentWidth, segmentHeight]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);
        await Assert.That(uri.PathAndQuery).IsEqualTo("/6/1024x768/foo");
    }

    /// <summary>Verifies a simple string path parameter is formatted with the expected metadata.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task UrlParameterShouldWorkWithGeneratedCode()
    {
        var fixture = new RequestBuilderImplementation<IBasicApi>();
        var factory = fixture.BuildRequestFactoryForMethod(nameof(IBasicApi.GetParam));
        var output = await factory([nameof(IBasicApi.GetParam)]);

        var uri = new Uri(new("http://api"), output.RequestUri!);
        await Assert.That(uri.PathAndQuery).IsEqualTo($"/{nameof(IBasicApi.GetParam)}");
    }
}
