// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Refit.Tests;

/// <summary>Tests for <see cref="RequestBuilderImplementation{T}"/> request construction.</summary>
public partial class RequestBuilderTests
{
    /// <summary>The integer array {1, 2, 3} used as query test data.</summary>
    private static readonly int[] _intArray123 = [1, 2, 3];

    /// <summary>The integer array {5, 7} used as query test data.</summary>
    private static readonly int[] _intArray57 = [5, 7];

    /// <summary>The string array {"A", "B"} used as query test data.</summary>
    private static readonly string[] _stringArrayAb = ["A", "B"];

    /// <summary>Rejects non-interface request-builder targets.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ConstructorRejectsNonInterfaceTargets() =>
        await Assert.That(() => new RequestBuilderImplementation(typeof(string)))
            .ThrowsExactly<ArgumentException>();

    /// <summary>Verifies the public request-builder factory entry points create usable builders.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [SuppressMessage("Usage", "CA2263:Prefer generic overloads", Justification = "This test intentionally covers the Type-based overloads.")]
    public async Task RequestBuilderFactoryEntryPointsCreateBuilders()
    {
        var genericBuilder = RequestBuilder.ForType<IDummyHttpApi>();
        var genericBuilderWithSettings = RequestBuilder.ForType<IDummyHttpApi>(new());
        var typeBuilder = RequestBuilder.ForType(typeof(IDummyHttpApi));
        var typeBuilderWithSettings = RequestBuilder.ForType(typeof(IDummyHttpApi), new());
        var factory = new RequestBuilderFactory();
        var factoryGenericBuilder = factory.Create<IDummyHttpApi>(new());
        var factoryTypeBuilder = factory.Create(typeof(IDummyHttpApi), new());

        await Assert.That(genericBuilder).IsNotNull();
        await Assert.That(genericBuilderWithSettings).IsNotNull();
        await Assert.That(typeBuilder).IsNotNull();
        await Assert.That(typeBuilderWithSettings).IsNotNull();
        await Assert.That(factoryGenericBuilder).IsNotNull();
        await Assert.That(factoryTypeBuilder).IsNotNull();
    }

    /// <summary>Rejects methods that are missing or ambiguous without parameter metadata.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task BuildRestResultFuncRejectsMissingAndAmbiguousMethods()
    {
        var fixture = new RequestBuilderImplementation<IOverloadedApi>();

        await Assert.That(() => fixture.BuildRestResultFuncForMethod("Missing"))
            .ThrowsExactly<ArgumentException>();
        await Assert.That(() => fixture.BuildRestResultFuncForMethod(nameof(IOverloadedApi.Overloaded)))
            .ThrowsExactly<ArgumentException>();
    }

    /// <summary>Builds a request when no cancellation token is supplied.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task MethodsShouldBeCancellableDefault()
    {
        var fixture = new RequestBuilderImplementation<ICancellableMethods>();
        var factory = fixture.RunRequest("GetWithCancellation");
        var output = factory([]);

        var uri = new Uri(new("http://api"), output.RequestMessage!.RequestUri!);
        await Assert.That(uri.PathAndQuery).IsEqualTo("/foo");
        await Assert.That(output.CancellationToken.IsCancellationRequested).IsFalse();
    }

    /// <summary>Builds a request when a nullable cancellation token is supplied.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task MethodsWithNullableCancellationTokenShouldBuildRequest()
    {
        var fixture = new RequestBuilderImplementation<ICancellableMethods>();
        var factory = fixture.RunRequest("GetWithNullableCancellation");

        using var cts = new CancellationTokenSource();
        var output = factory([42, cts.Token]);

        var uri = new Uri(new("http://api"), output.RequestMessage!.RequestUri!);
        await Assert.That(uri.PathAndQuery).IsEqualTo("/foo/42");
        await Assert.That(output.CancellationToken.IsCancellationRequested).IsFalse();
    }

    /// <summary>Cancels the request when a nullable cancellation token is cancelled.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task MethodsWithNullableCancellationTokenShouldCancelWhenRequested()
    {
        var fixture = new RequestBuilderImplementation<ICancellableMethods>();
        var factory = fixture.RunRequest("GetWithNullableCancellation");

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var output = factory([42, cts.Token]);

        await Assert.That(output.CancellationToken.IsCancellationRequested).IsTrue();
    }

    /// <summary>Builds a request when a cancellation token is supplied.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task MethodsShouldBeCancellableWithToken()
    {
        var fixture = new RequestBuilderImplementation<ICancellableMethods>();
        var factory = fixture.RunRequest("GetWithCancellation");

        using var cts = new CancellationTokenSource();

        var output = factory([cts.Token]);

        var uri = new Uri(new("http://api"), output.RequestMessage!.RequestUri!);
        await Assert.That(uri.PathAndQuery).IsEqualTo("/foo");
        await Assert.That(output.CancellationToken.IsCancellationRequested).IsFalse();
    }

    /// <summary>Cancels the request when the supplied token is cancelled.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task MethodsShouldBeCancellableWithTokenDoesCancel()
    {
        var fixture = new RequestBuilderImplementation<ICancellableMethods>();
        var factory = fixture.RunRequest("GetWithCancellation");

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var output = factory([cts.Token]);
        await Assert.That(output.CancellationToken.IsCancellationRequested).IsTrue();
    }

    /// <summary>The authorization header value getter receives the method cancellation token.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AuthorizationHeaderValueGetterReceivesMethodCancellationToken()
    {
        var observedCancellationToken = CancellationToken.None;
        var settings = new RefitSettings
        {
            AuthorizationHeaderValueGetter = (_, cancellationToken) =>
            {
                observedCancellationToken = cancellationToken;
                return Task.FromResult("tokenValue");
            }
        };

        var fixture = new RequestBuilderImplementation<IAuthenticatedCancellableMethods>(settings);
        var factory = fixture.RunRequest("GetWithAuthorizationAndCancellation");
        using var cts = new CancellationTokenSource();

        var output = factory([cts.Token]);

        await Assert.That(observedCancellationToken).IsEqualTo(cts.Token);
        await Assert.That(output.RequestMessage!.Headers.Authorization?.ToString()).IsEqualTo("Bearer tokenValue");
    }

    /// <summary>An HttpContent response is wrapped in an ApiResponse.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task HttpContentAsApiResponseTest()
    {
        var fixture = new RequestBuilderImplementation<IHttpContentApi>();
        var factory = fixture.BuildRestResultFuncForMethod("PostFileUploadWithMetadata");
        var testHttpMessageHandler = new TestHttpMessageHandler();
        var retContent = new StreamContent(new MemoryStream());
        testHttpMessageHandler.Content = retContent;

        var mpc = new MultipartContent("foosubtype");

        var task =
            (Task<ApiResponse<HttpContent>>)
                factory(
                    new(testHttpMessageHandler)
                    {
                        BaseAddress = new("http://api/")
                    },
                    [mpc])!;
        var result = await task;

        await Assert.That(result.Headers).IsNotNull();
        await Assert.That(result.IsSuccessStatusCode).IsTrue();
        await Assert.That(result.ReasonPhrase).IsNotNull();
        await Assert.That(result.StatusCode == default).IsFalse();
        await Assert.That(result.Version).IsNotNull();

        await Assert.That(testHttpMessageHandler.RequestMessage!.Content).IsEqualTo(mpc);
        await Assert.That(result.Content).IsEqualTo(retContent);
    }

    /// <summary>An HttpContent response is returned directly.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task HttpContentTest()
    {
        var fixture = new RequestBuilderImplementation<IHttpContentApi>();
        var factory = fixture.BuildRestResultFuncForMethod("PostFileUpload");
        var testHttpMessageHandler = new TestHttpMessageHandler();
        var retContent = new StreamContent(new MemoryStream());
        testHttpMessageHandler.Content = retContent;

        var mpc = new MultipartContent("foosubtype");

        var task =
            (Task<HttpContent>)
                factory(
                    new(testHttpMessageHandler)
                    {
                        BaseAddress = new("http://api/")
                    },
                    [mpc])!;
        var result = await task;

        await Assert.That(testHttpMessageHandler.RequestMessage!.Content).IsEqualTo(mpc);
        await Assert.That(result).IsEqualTo(retContent);
    }

    /// <summary>A stream body is wrapped directly in stream content.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task StreamRequestBodyUsesStreamContent()
    {
        var fixture = new RequestBuilderImplementation<IStreamApi>();
        var factory = fixture.BuildRequestFactoryForMethod(nameof(IStreamApi.PostStream));

        await using var stream = new MemoryStream([1, 2, 3]);
        var request = factory([stream]);

        await Assert.That(request.Content).IsTypeOf<StreamContent>();
    }

    /// <summary>A stream response is wrapped in an ApiResponse.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task StreamResponseAsApiResponseTest()
    {
        var fixture = new RequestBuilderImplementation<IStreamApi>();
        var factory = fixture.BuildRestResultFuncForMethod("GetRemoteFileWithMetadata");
        var testHttpMessageHandler = new TestHttpMessageHandler();
        var streamResponse = new MemoryStream();
        const string reponseContent = "A remote file";
        testHttpMessageHandler.Content = new StreamContent(streamResponse);

        var writer = new StreamWriter(streamResponse);
        await writer.WriteAsync(reponseContent);
        await writer.FlushAsync();
        streamResponse.Seek(0L, SeekOrigin.Begin);

        var task =
            (Task<ApiResponse<Stream>>)
                factory(
                    new(testHttpMessageHandler)
                    {
                        BaseAddress = new("http://api/")
                    },
                    ["test-file"])!;
        var result = await task;

        await Assert.That(result.Headers).IsNotNull();
        await Assert.That(result.IsSuccessStatusCode).IsTrue();
        await Assert.That(result.ReasonPhrase).IsNotNull();
        await Assert.That(result.StatusCode == default).IsFalse();
        await Assert.That(result.Version).IsNotNull();

        using var reader = new StreamReader(result.Content!);
        await Assert.That(await reader.ReadToEndAsync()).IsEqualTo(reponseContent);
    }

    /// <summary>The generated sync ApiResponse preserves the request message.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task GeneratedSyncApiResponseShouldPreserveRequestMessage()
    {
        var fixture = new RequestBuilderImplementation<ExplicitInterfaceRefitTests.ISyncPipelineApi>();
        var factory = fixture.BuildRestResultFuncForMethod("GetApiResponse");
        var testHttpMessageHandler = new TestHttpMessageHandler();

        var response = (IApiResponse<string>)
            factory(
                new(testHttpMessageHandler)
                {
                    BaseAddress = new("http://api/")
                },
                [])!;

        await Assert.That(response.RequestMessage).IsSameReferenceAs(testHttpMessageHandler.RequestMessage);
        await Assert.That(response.RequestMessage!.RequestUri).IsEqualTo(testHttpMessageHandler.RequestMessage!.RequestUri);
    }

    /// <summary>A stream response is returned directly.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task StreamResponseTest()
    {
        var fixture = new RequestBuilderImplementation<IStreamApi>();
        var factory = fixture.BuildRestResultFuncForMethod("GetRemoteFile");
        var testHttpMessageHandler = new TestHttpMessageHandler();
        var streamResponse = new MemoryStream();
        const string reponseContent = "A remote file";
        testHttpMessageHandler.Content = new StreamContent(streamResponse);

        var writer = new StreamWriter(streamResponse);
        await writer.WriteAsync(reponseContent);
        await writer.FlushAsync();
        streamResponse.Seek(0L, SeekOrigin.Begin);

        var task =
            (Task<Stream>)
                factory(
                    new(testHttpMessageHandler)
                    {
                        BaseAddress = new("http://api/")
                    },
                    ["test-file"])!;
        var result = await task;

        using var reader = new StreamReader(result);
        await Assert.That(await reader.ReadToEndAsync()).IsEqualTo(reponseContent);
    }

    /// <summary>ValueTask returning methods build and execute requests.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ValueTaskMethodsShouldWork()
    {
        var fixture = new RequestBuilderImplementation<IValueTaskApi>();
        var factory = fixture.BuildRestResultFuncForMethod("GetValue");
        var testHttpMessageHandler = new TestHttpMessageHandler();

        var valueTask = (ValueTask<string>)
            factory(
                new(testHttpMessageHandler)
                {
                    BaseAddress = new("http://api/")
                },
                ["value"])!;

        var result = await valueTask;

        await Assert.That(result).IsEqualTo("test");
        await Assert.That(testHttpMessageHandler.RequestMessage!.RequestUri!.ToString()).IsEqualTo("http://api/value");
    }

    /// <summary>ValueTask of ApiResponse returning methods build and execute requests.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ValueTaskApiResponseMethodsShouldWork()
    {
        var fixture = new RequestBuilderImplementation<IValueTaskApiResponseApi>();
        var factory = fixture.BuildRestResultFuncForMethod("GetValue");
        var testHttpMessageHandler = new TestHttpMessageHandler();

        var valueTask = (ValueTask<ApiResponse<string>>)
            factory(
                new(testHttpMessageHandler)
                {
                    BaseAddress = new("http://api/")
                },
                ["value"])!;

        using var response = await valueTask;

        await Assert.That(response.IsSuccessStatusCode).IsTrue();
        await Assert.That(response.Content).IsEqualTo("test");
        await Assert.That(response.RequestMessage).IsSameReferenceAs(testHttpMessageHandler.RequestMessage);
        await Assert.That(response.RequestMessage!.RequestUri!.ToString()).IsEqualTo("http://api/value");
    }

    /// <summary>Observable methods cancel when the supplied token is cancelled.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ObservableMethodsWithCancellationTokenShouldCancelWhenRequested()
    {
        var fixture = new RequestBuilderImplementation<IObservableCancellableMethods>();
        var factory = fixture.BuildRestResultFuncForMethod("GetWithCancellation");
        var testHttpMessageHandler = new TestHttpMessageHandler();

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var observable = (IObservable<string>)
            factory(
                new(testHttpMessageHandler)
                {
                    BaseAddress = new("http://api/")
                },
                ["value", cts.Token])!;

        await Assert.That(() => (Task)ObservableTestHelpers.Await(observable)).ThrowsExactly<TaskCanceledException>();
        await Assert.That(testHttpMessageHandler.RequestMessage!.RequestUri!.ToString()).IsEqualTo("http://api/value/value");
        await Assert.That(testHttpMessageHandler.CancellationToken.IsCancellationRequested).IsTrue();
    }

    /// <summary>Throws while analyzing an invalid public synchronous method.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ConstructorThrowsForInvalidPublicSyncMethod()
    {
        var exception = await Assert.That(
            () => new RequestBuilderImplementation<IInvalidReturnTypeIApiResponse>()).ThrowsExactly<ArgumentException>();

        await Assert.That(exception!.Message).Contains(
            "All REST Methods must return either Task<T> or ValueTask<T> or IObservable<T>");
    }

    /// <summary>Methods that do not specify an HTTP method fail to build.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task MethodsThatDontHaveAnHttpMethodShouldFail()
    {
        var failureMethods = new[] { "SomeOtherMethod", "weofjwoeijfwe", null };

        var successMethods = new[] { "FetchSomeStuff" };

        foreach (var v in failureMethods)
        {
            var shouldDie = true;

            try
            {
                var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
                fixture.BuildRequestFactoryForMethod(v!);
            }
            catch (Exception)
            {
                shouldDie = false;
            }

            await Assert.That(shouldDie).IsFalse();
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

            await Assert.That(shouldDie).IsFalse();
        }
    }

    /// <summary>A hardcoded query parameter appears in the URL.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task HardcodedQueryParamShouldBeInUrl()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            "FetchSomeStuffWithHardcodedQueryParameter");
        var output = factory([6]);

        var uri = new Uri(new("http://api"), output.RequestUri!);
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
        var output = factory([6, "foo"]);

        var uri = new Uri(new("http://api"), output.RequestUri!);
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
        var output = factory([6]);

        var uri = new Uri(new("http://api"), output.RequestUri!);
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
        var output = factory([path, 1]);

        var uri = new Uri(new("http://api"), output.RequestUri!);
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
        var output = factory(
            [new FileInfo(typeof(RequestBuilderTests).Assembly.Location), null!]);

        var uri = new Uri(new("http://api"), output.RequestUri!);
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
        var output = factory(["value1", "value2"]);

        var uri = new Uri(new("http://api"), output.RequestUri!);

        await Assert.That(uri.PathAndQuery).IsEqualTo("/query?q1=value1&q2=value2");
    }

    /// <summary>A query parameter is formatted using its format attribute.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task QueryParamShouldFormat()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithQueryFormat");
        var output = factory([6]);

        var uri = new Uri(new("http://api"), output.RequestUri!);
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
        var output = factory([6, "push!=pull&push"]);

        var uri = new Uri(new("http://api"), output.RequestUri!);

        await Assert.That(uri.PathAndQuery).IsEqualTo("/foo/bar/6?baz=bamf&search_for=push%21%3Dpull%26push");
    }

    /// <summary>Mixed replacement and query values are encoded in the URL.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ParameterizedQueryParamsShouldBeInUrlAndValuesEncodedWhenMixedReplacementAndQuery()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            "FetchSomeStuffWithVoidAndQueryAlias");
        var output = factory(["6 & 7/8", "test@example.com", "push!=pull"]);

        var uri = new Uri(new("http://api"), output.RequestUri!);

        await Assert.That(uri.PathAndQuery).IsEqualTo("/void/6%20%26%207%2F8/path?a=test%40example.com&b=push%21%3Dpull");
    }

    /// <summary>Query parameters with a path delimiter are encoded.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task QueryParamWithPathDelimiterShouldBeEncoded()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            "FetchSomeStuffWithVoidAndQueryAlias");
        var output = factory(["6/6", "test@example.com", "push!=pull"]);

        var uri = new Uri(new("http://api"), output.RequestUri!);

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
        var output = factory([42]);

        var uri = new Uri(new("http://api"), output.RequestUri!);

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
        var output = factory(["1"]);

        var uri = new Uri(new("http://api/"), output.RequestUri!);

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
            "FetchSomeStuffWithVoidAndQueryAlias");
        var output = factory(["6", "test@example.com", "push!=pull"]);

        var uri = new Uri(new("http://api"), output.RequestUri!);

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
        var output = factory([true, 'x']);

        var uri = new Uri(new("http://api"), output.RequestUri!);

        await Assert.That(uri.PathAndQuery).IsEqualTo("/foo?b=True&c=x");
    }

    /// <summary>Multiple parameters in the same segment are generated correctly.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task MultipleParametersInTheSameSegmentAreGeneratedProperly()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            "FetchSomethingWithMultipleParametersPerSegment");
        var output = factory([6, 1024, 768]);

        var uri = new Uri(new("http://api"), output.RequestUri!);
        await Assert.That(uri.PathAndQuery).IsEqualTo("/6/1024x768/foo");
    }
}
