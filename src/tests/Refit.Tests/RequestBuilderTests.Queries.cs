// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Tests for <see cref="RequestBuilderImplementation{T}"/> query and header formatting.</summary>
public partial class RequestBuilderTests
{
    /// <summary>A sample value routed through a property named 'Password' to exercise query-key prefixing.</summary>
    private const string SensitiveSampleValue = "secret";

    /// <summary>The User-Agent header name asserted by the header tests.</summary>
    private const string UserAgentHeaderName = "User-Agent";

    /// <summary>The reason message for the User-Agent header presence assertions.</summary>
    private const string UserAgentHeaderReason = "Headers include User-Agent header";

    /// <summary>The expected User-Agent header value.</summary>
    private const string RefitTestClientUserAgent = "RefitTestClient";

    /// <summary>The Api-Version header name asserted by the header tests.</summary>
    private const string ApiVersionHeaderName = "Api-Version";

    /// <summary>The reason message for the Api-Version header presence assertions.</summary>
    private const string ApiVersionHeaderReason = "Headers include Api-Version header";

    /// <summary>The Accept header name asserted by the header tests.</summary>
    private const string AcceptHeaderName = "Accept";

    /// <summary>The Authorization header name asserted by the header tests.</summary>
    private const string AuthorizationHeaderName = "Authorization";

    /// <summary>The reason message for the Authorization header presence assertions.</summary>
    private const string AuthorizationHeaderReason = "Headers include Authorization header";

    /// <summary>The custom emoji header name asserted by the header tests.</summary>
    private const string EmojiHeaderName = "X-Emoji";

    /// <summary>The dynamic request property key asserted by the property tests.</summary>
    private const string SomePropertyKey = "SomeProperty";

    /// <summary>The emoji query value asserted by the header-encoding tests.</summary>
    private const string JoyCatEmojiValue = ":joy_cat:";

    /// <summary>Assertion reason describing the presence of the X-Emoji header.</summary>
    private const string EmojiHeaderReason = "Headers include X-Emoji header";

    /// <summary>Assertion reason describing the presence of the Accept header.</summary>
    private const string AcceptHeaderReason = "Headers include Accept header";

    /// <summary>The JSON content type asserted by the Accept-header tests.</summary>
    private const string JsonContentType = "application/json";

    /// <summary>The numeric value assigned to the <c>Bar</c> field of the URL-encoded body samples.</summary>
    private const int SampleBarValue = 100;

    /// <summary>The numeric value assigned to the nested <c>Age</c> field of the nested URL-encoded body sample.</summary>
    private const int SampleNestedAge = 42;

    /// <summary>The value assigned to the <c>Foo</c> field of the URL-encoded body samples.</summary>
    private const string SampleFooValue = "Something";

    /// <summary>Reads string content with metadata.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ReadStringContentWithMetadata()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRestResultFuncForMethod("FetchSomeStringWithMetadata");
        var testHttpMessageHandler = new TestHttpMessageHandler();

        var task =
            (Task<ApiResponse<string>>)
                factory(
                    new(testHttpMessageHandler)
                    {
                        BaseAddress = new(ApiBaseUrlWithSlash)
                    },
                    [AlternateSampleId])!;
        var result = await task;

        await Assert.That(result.Headers).IsNotNull();
        await Assert.That(result.IsSuccessStatusCode).IsTrue();
        await Assert.That(result.ReasonPhrase).IsNotNull();
        await Assert.That(result.StatusCode == default).IsFalse();
        await Assert.That(result.Version).IsNotNull();

        await Assert.That(result.Content).IsEqualTo("test");
    }

    /// <summary>A parameter with property and query attributes is added to the query.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ParameterWithPropertyAndQueryAttributesIsAddedToQuery()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            "FetchSomeStuffWithPropertyAndQuery");
        var output = await factory([SampleId, "value1"]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);
        await Assert.That(uri.PathAndQuery).IsEqualTo("/foo/bar/6?someValue=value1");
    }

    /// <summary>A property-level query prefix and delimiter customize the query key.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task PropertyQueryPrefixAndDelimiterAreUsedForQueryKey()
    {
        var fixture = new RequestBuilderImplementation<IQueryApi>();
        var factory = fixture.BuildRequestFactoryForMethod(nameof(IQueryApi.PrefixedQuery));
        var output = await factory([new PrefixedQueryObject { Password = SensitiveSampleValue, User = "bob" }]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
        await Assert.That(uri.AbsolutePath).IsEqualTo("/foo");
        await Assert.That(query[$"dontlog-{nameof(PrefixedQueryObject.Password)}"].ToString()).IsEqualTo(SensitiveSampleValue);
        await Assert.That(query["User"].ToString()).IsEqualTo("bob");
    }

    /// <summary>An empty query format serializes a complex value via ToString under the parameter name.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task EmptyQueryFormatSerializesComplexValueViaToString()
    {
        var fixture = new RequestBuilderImplementation<IQueryApi>();
        var factory = fixture.BuildRequestFactoryForMethod(nameof(IQueryApi.EmptyFormatComplexQuery));
        var output = await factory([new EnumerationQueryValue("medium")]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);
        await Assert.That(uri.PathAndQuery).IsEqualTo("/info?size=medium");
    }

    /// <summary>Dynamic request properties appear in the request properties.</summary>
    /// <param name="interfaceMethodName">The name of the interface method to build.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [Arguments(nameof(IDummyHttpApi.FetchSomeStuffWithDynamicRequestProperty))]
    [Arguments(nameof(IDummyHttpApi.DeleteSomeStuffWithDynamicRequestProperty))]
    [Arguments(nameof(IDummyHttpApi.PutSomeStuffWithDynamicRequestProperty))]
    [Arguments(nameof(IDummyHttpApi.PostSomeStuffWithDynamicRequestProperty))]
    [Arguments(nameof(IDummyHttpApi.PatchSomeStuffWithDynamicRequestProperty))]
    public async Task DynamicRequestPropertiesShouldBeInProperties(string interfaceMethodName)
    {
        var someProperty = new object();
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(interfaceMethodName);
        var output = await factory([SampleId, someProperty]);

#if NET6_0_OR_GREATER
        await Assert.That(output.Options).IsNotEmpty();
        await Assert.That(((IDictionary<string, object?>)output.Options)[SomePropertyKey]).IsEqualTo(someProperty);
#endif

#pragma warning disable CS0618 // Type or member is obsolete
        await Assert.That(output.Properties).IsNotEmpty();
        await Assert.That(output.Properties[SomePropertyKey]).IsEqualTo(someProperty);
#pragma warning restore CS0618 // Type or member is obsolete
    }

    /// <summary>Options from settings appear in the request properties.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task OptionsFromSettingsShouldBeInProperties()
    {
        const string nameProp1 = "UnitTest.Property1";
        const string valueProp1 = "TestValue";
        const string nameProp2 = "UnitTest.Property2";
        object valueProp2 = new List<string> { "123", "345" };
        var fixture = new RequestBuilderImplementation<IContainAandB>(
            new()
            {
                HttpRequestMessageOptions = new()
                {
                    [nameProp1] = valueProp1,
                    [nameProp2] = valueProp2,
                },
            });
        var factory = fixture.BuildRequestFactoryForMethod(nameof(IContainAandB.Ping));
        var output = await factory([]);

#if NET6_0_OR_GREATER
        await Assert.That(output.Options).IsNotEmpty();
        await Assert.That(
            output.Options.TryGetValue(
                new HttpRequestOptionsKey<string>(nameProp1),
                out var resultValueProp1)).IsTrue();
        await Assert.That(resultValueProp1).IsEqualTo(valueProp1);

        await Assert.That(
            output.Options.TryGetValue(
                new HttpRequestOptionsKey<List<string>>(nameProp2),
                out var resultValueProp2)).IsTrue();
        await Assert.That(resultValueProp2).IsCollectionEqualTo((List<string>)valueProp2);
#else
        await Assert.That(output.Properties).IsNotEmpty();
        await Assert.That(output.Properties.TryGetValue(nameProp1, out var resultValueProp1)).IsTrue();
        await Assert.That(resultValueProp1).IsTypeOf<string>();
        await Assert.That((string)resultValueProp1).IsEqualTo(valueProp1);

        await Assert.That(output.Properties.TryGetValue(nameProp2, out var resultValueProp2)).IsTrue();
        await Assert.That(resultValueProp2).IsTypeOf<List<string>>();
        await Assert.That((List<string>)resultValueProp2).IsEqualTo(valueProp2);
#endif
    }

    /// <summary>The interface type appears in the request properties.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task InterfaceTypeShouldBeInProperties()
    {
        var fixture = new RequestBuilderImplementation<IContainAandB>();
        var factory = fixture.BuildRequestFactoryForMethod(nameof(IContainAandB.Ping));
        var output = await factory([]);

#pragma warning disable CS0618 // Type or member is obsolete
        await Assert.That(output.Properties).IsNotEmpty();
        await Assert.That(output.Properties[HttpRequestMessageOptions.InterfaceType]).IsEqualTo(typeof(IContainAandB));
#pragma warning restore CS0618 // Type or member is obsolete
    }

    /// <summary>The rest method info appears in the request properties.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task RestMethodInfoShouldBeInProperties()
    {
        var fixture = new RequestBuilderImplementation<IContainAandB>();
        var factory = fixture.BuildRequestFactoryForMethod(nameof(IContainAandB.Ping));
        var output = await factory([]);

#if NET6_0_OR_GREATER
        await Assert.That(output.Options).IsNotEmpty();
        await Assert.That(
            output.Options.TryGetValue(
                new HttpRequestOptionsKey<RestMethodInfo>(
                    HttpRequestMessageOptions.RestMethodInfo),
                out var restMethodInfo)).IsTrue();
#else
        await Assert.That(output.Properties).IsNotEmpty();
        await Assert.That(
            output.Properties.TryGetValue(
                HttpRequestMessageOptions.RestMethodInfo,
                out var restMethodInfoObj)).IsTrue();
        await Assert.That(restMethodInfoObj).IsTypeOf<RestMethodInfo>();
        var restMethodInfo = restMethodInfoObj as RestMethodInfo;
#endif
        await Assert.That(restMethodInfo!.Name).IsEqualTo(nameof(IContainAandB.Ping));
    }

    /// <summary>The method name and the raw relative-path template appear in the reflection-built request properties,
    /// with the template keeping its <c>{placeholder}</c> rather than the filled URL.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task MethodNameAndRelativePathTemplateShouldBeInProperties()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(nameof(IDummyHttpApi.FetchSomeStuff));
        var output = await factory([SampleId]);

#if NET6_0_OR_GREATER
        await Assert.That(
            output.Options.TryGetValue(
                new HttpRequestOptionsKey<string>(HttpRequestMessageOptions.MethodName),
                out var methodName)).IsTrue();
        await Assert.That(
            output.Options.TryGetValue(
                new HttpRequestOptionsKey<string>(HttpRequestMessageOptions.RelativePathTemplate),
                out var relativePathTemplate)).IsTrue();
#else
        await Assert.That(
            output.Properties.TryGetValue(HttpRequestMessageOptions.MethodName, out var methodNameObj)).IsTrue();
        await Assert.That(
            output.Properties.TryGetValue(HttpRequestMessageOptions.RelativePathTemplate, out var templateObj)).IsTrue();
        var methodName = methodNameObj as string;
        var relativePathTemplate = templateObj as string;
#endif
        await Assert.That(methodName).IsEqualTo(nameof(IDummyHttpApi.FetchSomeStuff));
        await Assert.That(relativePathTemplate).IsEqualTo("/foo/bar/{id}");

        // The template is the low-cardinality label; the filled request URI resolves the placeholder to the argument.
        var uri = new Uri(new("http://api/"), output.RequestUri!);
        await Assert.That(uri.PathAndQuery).IsEqualTo($"/foo/bar/{SampleId}");
    }

    /// <summary>The declared call arguments, including the cancellation token, appear in order when capture is enabled.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task MethodArgumentsInPropertiesWhenEnabled()
    {
        const string orgName = "dotnet";
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var fixture = new RequestBuilderImplementation<IGitHubApi>(new() { CaptureMethodArguments = true });
        var runRequest = fixture.RunRequest(nameof(IGitHubApi.GetOrgMembers), "[]");
        var handler = await runRequest([orgName, token]);

        await MethodArgumentCaptureAssertions.AssertCapturedAsync(handler.RequestMessage!, orgName, token);
    }

    /// <summary>The method-arguments option is absent when capture is left at its default (off).</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task MethodArgumentsAbsentByDefault()
    {
        const string orgName = "dotnet";
        var fixture = new RequestBuilderImplementation<IGitHubApi>();
        var runRequest = fixture.RunRequest(nameof(IGitHubApi.GetOrgMembers), "[]");
        var handler = await runRequest([orgName, CancellationToken.None]);

        await MethodArgumentCaptureAssertions.AssertAbsentAsync(handler.RequestMessage!);
    }

    /// <summary>Dynamic request properties with default keys appear in the request properties.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DynamicRequestPropertiesWithDefaultKeysShouldBeInProperties()
    {
        var someProperty = new object();
        var someOtherProperty = new object();
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IDummyHttpApi.FetchSomeStuffWithDynamicRequestPropertyWithoutKey));
        var output = await factory([SampleId, someProperty, someOtherProperty]);

#if NET6_0_OR_GREATER
        await Assert.That(output.Options).IsNotEmpty();
        await Assert.That(((IDictionary<string, object?>)output.Options)["someValue"]).IsEqualTo(someProperty);
        await Assert.That(((IDictionary<string, object?>)output.Options)["someOtherValue"]).IsEqualTo(someOtherProperty);
#endif

#pragma warning disable CS0618 // Type or member is obsolete
        await Assert.That(output.Properties).IsNotEmpty();
        await Assert.That(output.Properties["someValue"]).IsEqualTo(someProperty);
        await Assert.That(output.Properties["someOtherValue"]).IsEqualTo(someOtherProperty);
#pragma warning restore CS0618 // Type or member is obsolete
    }

    /// <summary>A duplicate key overwrites the previous request property.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DynamicRequestPropertiesWithDuplicateKeyShouldOverwritePreviousProperty()
    {
        var someProperty = new object();
        var someOtherProperty = new object();
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IDummyHttpApi.FetchSomeStuffWithDynamicRequestPropertyWithDuplicateKey));
        var output = await factory([SampleId, someProperty, someOtherProperty]);

        // The reflection builder stores the interface type, the RestMethodInfo, the method name, the raw route template,
        // and the single (deduplicated) dynamic property.
        const int expectedPropertyCount = 5;

#if NET6_0_OR_GREATER
        await Assert.That(output.Options.Count()).IsEqualTo(expectedPropertyCount);
        await Assert.That(((IDictionary<string, object?>)output.Options)[SomePropertyKey]).IsEqualTo(someOtherProperty);
#endif

#pragma warning disable CS0618 // Type or member is obsolete
        await Assert.That(output.Properties.Count).IsEqualTo(expectedPropertyCount);
        await Assert.That(output.Properties[SomePropertyKey]).IsEqualTo(someOtherProperty);
#pragma warning restore CS0618 // Type or member is obsolete
    }

    /// <summary>The HttpClient prefixes the absolute path to the request URI.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task HttpClientShouldPrefixedAbsolutePathToTheRequestUri()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRestResultFuncForMethod("FetchSomeStuffWithoutFullPath");
        var testHttpMessageHandler = new TestHttpMessageHandler();

        var task = (Task)factory(
            new(testHttpMessageHandler)
            {
                BaseAddress = new("http://api/foo/bar")
            },
            [])!;
        await task;

        await Assert.That(testHttpMessageHandler.RequestMessage!.RequestUri!.ToString()).IsEqualTo("http://api/foo/bar/string");
    }

    /// <summary>The HttpClient prefixes the absolute path for a void method.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task HttpClientForVoidMethodShouldPrefixedAbsolutePathToTheRequestUri()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRestResultFuncForMethod("FetchSomeStuffWithVoid");
        var testHttpMessageHandler = new TestHttpMessageHandler();

        var task = (Task)factory(
            new(testHttpMessageHandler)
            {
                BaseAddress = new("http://api/foo/bar")
            },
            [])!;
        await task;

        await Assert.That(testHttpMessageHandler.RequestMessage!.RequestUri!.ToString()).IsEqualTo("http://api/foo/bar/void");
    }

    /// <summary>The HttpClient does not prefix an empty absolute path to the request URI.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task HttpClientShouldNotPrefixEmptyAbsolutePathToTheRequestUri()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRestResultFuncForMethod("FetchSomeStuff");
        var testHttpMessageHandler = new TestHttpMessageHandler();

        var task = (Task)factory(
            new(testHttpMessageHandler) { BaseAddress = new(ApiBaseUrlWithSlash) },
            [AlternateSampleId])!;
        await task;

        await Assert.That(testHttpMessageHandler.RequestMessage!.RequestUri!.ToString()).IsEqualTo("http://api/foo/bar/42");
    }

    /// <summary>Body content gets URL encoded.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task BodyContentGetsUrlEncoded()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.RunRequest(nameof(IDummyHttpApi.PostSomeUrlEncodedStuff));
        var output = await factory(
            [
                SampleId,

                // Baz is intentionally blank to verify empty values are preserved rather than stripped.
                new UrlEncodedBody(Foo: SampleFooValue, Bar: SampleBarValue, Baz: string.Empty)
            ]);

        await Assert.That(output.SendContent).IsEqualTo("Foo=Something&Bar=100&Baz=");
    }

    /// <summary>Body content gets URL encoded with a collection format.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task BodyContentGetsUrlEncodedWithCollectionFormat()
    {
        var settings = new RefitSettings { CollectionFormat = CollectionFormat.Csv };
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>(settings);
        var factory = fixture.RunRequest(nameof(IDummyHttpApi.PostSomeUrlEncodedStuff));
        var output = await factory(
            [
                SampleId,

                // Baz is intentionally blank to verify empty values are preserved rather than stripped.
                new UrlEncodedBodyWithCollection(Foo: SampleFooValue, Bar: SampleBarValue, FooBar: _intArray57, Baz: string.Empty)
            ]);

        await Assert.That(output.SendContent).IsEqualTo("Foo=Something&Bar=100&FooBar=5%2C7&Baz=");
    }

    /// <summary>A nested object and dictionary in a URL-encoded body flatten to dotted field names.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task BodyContentGetsUrlEncodedWithNestedObjectFlattened()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.RunRequest(nameof(IDummyHttpApi.PostSomeUrlEncodedStuff));
        var output = await factory(
            [
                SampleId,
                new UrlEncodedBodyWithNestedObject(
                    Foo: SampleFooValue,
                    Detail: new(Email: "a@b.com", Age: SampleNestedAge),
                    Extra: new Dictionary<string, string> { { "k", "v" } })
            ]);

        await Assert.That(output.SendContent)
            .IsEqualTo("Foo=Something&Detail.Email=a%40b.com&Detail.Age=42&Extra.k=v");
    }

    /// <summary>A form field gets aliased.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task FormFieldGetsAliased()
    {
        const int readablePropertyValue = 99;
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.RunRequest("PostSomeAliasedUrlEncodedStuff");
        var output = await factory(
            [
                SampleId,
                new SomeRequestData { ReadablePropertyName = readablePropertyValue }
            ]);

        await Assert.That(output.SendContent).IsEqualTo("rpn=99");
    }

    /// <summary>A custom parameter formatter is applied.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task CustomParmeterFormatter()
    {
        const int id = 5;
        var settings = new RefitSettings
        {
            UrlParameterFormatter = new TestUrlParameterFormatter("custom-parameter")
        };
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>(settings);

        var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuff");
        var output = await factory([id]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);
        await Assert.That(uri.PathAndQuery).IsEqualTo("/foo/bar/custom-parameter");
    }

    /// <summary>A query string with enumerables can be formatted.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task QueryStringWithEnumerablesCanBeFormatted()
    {
        var settings = new RefitSettings
        {
            UrlParameterFormatter = new TestEnumerableUrlParameterFormatter()
        };
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>(settings);

        var factory = fixture.BuildRequestFactoryForMethod("QueryWithEnumerable");
        var output = await factory([_intArray123]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);
        await Assert.That(uri.PathAndQuery).IsEqualTo("/query?numbers=1%2C2%2C3");
    }

    /// <summary>A query string with an array can be formatted.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task QueryStringWithArrayCanBeFormatted()
    {
        var settings = new RefitSettings
        {
            UrlParameterFormatter = new TestEnumerableUrlParameterFormatter()
        };
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>(settings);

        var factory = fixture.BuildRequestFactoryForMethod("QueryWithArray");
        var output = await factory([_intArray123]);

        var uri = new Uri(new(ApiBaseUrl), output.RequestUri!);
        await Assert.That(uri.PathAndQuery).IsEqualTo("/query?numbers=1%2C2%2C3");
    }

    /// <summary>Body payload for the URL-encoded request body test.</summary>
    /// <param name="Foo">The first value.</param>
    /// <param name="Bar">The numeric value.</param>
    /// <param name="Baz">A blank value used to verify empty fields are preserved rather than stripped.</param>
    private sealed record UrlEncodedBody(string Foo, int Bar, string Baz);

    /// <summary>Body payload with a collection field for the URL-encoded collection-format test.</summary>
    /// <param name="Foo">The first value.</param>
    /// <param name="Bar">The numeric value.</param>
    /// <param name="FooBar">The collection value.</param>
    /// <param name="Baz">A blank value used to verify empty fields are preserved rather than stripped.</param>
    private sealed record UrlEncodedBodyWithCollection(string Foo, int Bar, int[] FooBar, string Baz);

    /// <summary>Body payload with a nested object and dictionary for the URL-encoded flattening test.</summary>
    /// <param name="Foo">The scalar value.</param>
    /// <param name="Detail">The nested object flattened under its property name.</param>
    /// <param name="Extra">The dictionary flattened under its property name.</param>
    private sealed record UrlEncodedBodyWithNestedObject(
        string Foo,
        NestedFormDetail Detail,
        Dictionary<string, string> Extra);

    /// <summary>Nested detail object for the URL-encoded flattening test.</summary>
    /// <param name="Email">The email value.</param>
    /// <param name="Age">The numeric age value.</param>
    private sealed record NestedFormDetail(string Email, int Age);
}
