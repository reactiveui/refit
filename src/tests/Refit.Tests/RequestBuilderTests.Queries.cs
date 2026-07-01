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

    /// <summary>Hardcoded headers appear in the request headers.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task HardcodedHeadersShouldBeInHeaders()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();

        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IDummyHttpApi.FetchSomeStuffWithHardcodedHeaders));
        var output = await factory([6]);

        await Assert.That(output.Headers.Contains(UserAgentHeaderName)).IsTrue().Because(UserAgentHeaderReason);
        await Assert.That(output.Headers.UserAgent.ToString()).IsEqualTo(RefitTestClientUserAgent);
        await Assert.That(output.Headers.Contains(ApiVersionHeaderName)).IsTrue().Because(ApiVersionHeaderReason);
        await Assert.That(output.Headers.GetValues(ApiVersionHeaderName).Single()).IsEqualTo("2");
        await Assert.That(output.Headers.Contains(AcceptHeaderName)).IsTrue().Because("Headers include Accept header");
        await Assert.That(output.Headers.Accept.ToString()).IsEqualTo("application/json");
    }

    /// <summary>Empty hardcoded headers appear in the request headers.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task EmptyHardcodedHeadersShouldBeInHeaders()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            "FetchSomeStuffWithEmptyHardcodedHeader");
        var output = await factory([6]);

        await Assert.That(output.Headers.Contains(UserAgentHeaderName)).IsTrue().Because(UserAgentHeaderReason);
        await Assert.That(output.Headers.UserAgent.ToString()).IsEqualTo(RefitTestClientUserAgent);
        await Assert.That(output.Headers.Contains(ApiVersionHeaderName)).IsTrue().Because(ApiVersionHeaderReason);
        await Assert.That(output.Headers.GetValues(ApiVersionHeaderName).Single()).IsEqualTo(string.Empty);
    }

    /// <summary>Null hardcoded headers are not present in the request headers.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task NullHardcodedHeadersShouldNotBeInHeaders()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            "FetchSomeStuffWithNullHardcodedHeader");
        var output = await factory([6]);

        await Assert.That(output.Headers.Contains(UserAgentHeaderName)).IsTrue().Because(UserAgentHeaderReason);
        await Assert.That(output.Headers.UserAgent.ToString()).IsEqualTo(RefitTestClientUserAgent);
        await Assert.That(output.Headers.Contains(ApiVersionHeaderName)).IsFalse().Because(ApiVersionHeaderReason);
    }

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
                    [42])!;
        var result = await task;

        await Assert.That(result.Headers).IsNotNull();
        await Assert.That(result.IsSuccessStatusCode).IsTrue();
        await Assert.That(result.ReasonPhrase).IsNotNull();
        await Assert.That(result.StatusCode == default).IsFalse();
        await Assert.That(result.Version).IsNotNull();

        await Assert.That(result.Content).IsEqualTo("test");
    }

    /// <summary>Content headers can be hardcoded.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ContentHeadersCanBeHardcoded()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            "PostSomeStuffWithHardCodedContentTypeHeader");
        var output = await factory([6, "stuff"]);

        await Assert.That(output.Content!.Headers.Contains("Content-Type")).IsTrue().Because("Content headers include Content-Type header");
        await Assert.That(output.Content!.Headers.ContentType!.ToString()).IsEqualTo("literally/anything");
    }

    /// <summary>Non-canonical content type header casing is not duplicated.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task NonCanonicalContentTypeHeaderCasingIsNotDuplicated()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            "PostSomeStuffWithNonCanonicalContentTypeHeader");
        var output = await factory([6, "stuff"]);

        await Assert.That(output.Content!.Headers.ContentType!.ToString()).IsEqualTo("application/soap+xml");
    }

    /// <summary>A parameter with property and query attributes is added to the query.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ParameterWithPropertyAndQueryAttributesIsAddedToQuery()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            "FetchSomeStuffWithPropertyAndQuery");
        var output = await factory([6, "value1"]);

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

    /// <summary>A dynamic header appears in the request headers.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DynamicHeaderShouldBeInHeaders()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithDynamicHeader");
        var output = await factory([6, "Basic RnVjayB5ZWFoOmhlYWRlcnMh"]);

        await Assert.That(output.Headers.Authorization).IsNotNull();
        await Assert.That(output.Headers.Authorization!.Parameter).IsEqualTo("RnVjayB5ZWFoOmhlYWRlcnMh");
    }

    /// <summary>A custom dynamic header appears in the request headers.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task CustomDynamicHeaderShouldBeInHeaders()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithCustomHeader");
        var output = await factory([6, ":joy_cat:"]);

        await Assert.That(output.Headers.Contains(EmojiHeaderName)).IsTrue().Because("Headers include X-Emoji header");
        await Assert.That(output.Headers.GetValues(EmojiHeaderName).First()).IsEqualTo(":joy_cat:");
    }

    /// <summary>An empty dynamic header appears in the request headers.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task EmptyDynamicHeaderShouldBeInHeaders()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithCustomHeader");
        var output = await factory([6, string.Empty]);

        await Assert.That(output.Headers.Contains(EmojiHeaderName)).IsTrue().Because("Headers include X-Emoji header");
        await Assert.That(output.Headers.GetValues(EmojiHeaderName).First()).IsEqualTo(string.Empty);
    }

    /// <summary>A null dynamic header is not present in the request headers.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task NullDynamicHeaderShouldNotBeInHeaders()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithDynamicHeader");
        var output = await factory([6, null!]);

        await Assert.That(output.Headers.Authorization).IsNull();
    }

    /// <summary>A path member used as a custom dynamic header appears in the request headers.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task PathMemberAsCustomDynamicHeaderShouldBeInHeaders()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            "FetchSomeStuffWithPathMemberInCustomHeader");
        var output = await factory([6, ":joy_cat:"]);

        await Assert.That(output.Headers.Contains("X-PathMember")).IsTrue().Because("Headers include X-PathMember header");
        await Assert.That(output.Headers.GetValues("X-PathMember").First()).IsEqualTo("6");
    }

    /// <summary>Custom headers are added to the request headers only.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AddCustomHeadersToRequestHeadersOnly()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod("PostSomeStuffWithCustomHeader");
        var output = await factory([6, new { Foo = "bar" }, ":smile_cat:"]);

        await Assert.That(output.Headers.Contains(ApiVersionHeaderName)).IsTrue().Because(ApiVersionHeaderReason);
        await Assert.That(output.Headers.Contains(EmojiHeaderName)).IsTrue().Because("Headers include X-Emoji header");
        await Assert.That(output.Content!.Headers.Contains(ApiVersionHeaderName)).IsFalse().Because("Content headers include Api-Version header");
        await Assert.That(output.Content!.Headers.Contains(EmojiHeaderName)).IsFalse().Because("Content headers include X-Emoji header");
    }

    /// <summary>A header collection appears in the request headers.</summary>
    /// <param name="interfaceMethodName">The name of the interface method to build.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [Arguments(nameof(IDummyHttpApi.FetchSomeStuffWithDynamicHeaderCollection))]
    [Arguments(nameof(IDummyHttpApi.DeleteSomeStuffWithDynamicHeaderCollection))]
    [Arguments(nameof(IDummyHttpApi.PutSomeStuffWithDynamicHeaderCollection))]
    [Arguments(nameof(IDummyHttpApi.PostSomeStuffWithDynamicHeaderCollection))]
    [Arguments(nameof(IDummyHttpApi.PatchSomeStuffWithDynamicHeaderCollection))]
    public async Task HeaderCollectionShouldBeInHeaders(string interfaceMethodName)
    {
        var headerCollection = new Dictionary<string, string>
        {
            { "key1", "val1" },
            { "key2", "val2" }
        };

        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(interfaceMethodName);
        var output = await factory([6, headerCollection]);

        await Assert.That(output.Headers.Contains(UserAgentHeaderName)).IsTrue().Because(UserAgentHeaderReason);
        await Assert.That(output.Headers.GetValues(UserAgentHeaderName).First()).IsEqualTo(RefitTestClientUserAgent);
        await Assert.That(output.Headers.Contains(ApiVersionHeaderName)).IsTrue().Because(ApiVersionHeaderReason);
        await Assert.That(output.Headers.GetValues(ApiVersionHeaderName).First()).IsEqualTo("1");

        await Assert.That(output.Headers.Contains(AuthorizationHeaderName)).IsTrue().Because(AuthorizationHeaderReason);
        await Assert.That(output.Headers.GetValues(AuthorizationHeaderName).First()).IsEqualTo("SRSLY aHR0cDovL2kuaW1ndXIuY29tL0NGRzJaLmdpZg==");
        await Assert.That(output.Headers.Contains(AcceptHeaderName)).IsTrue().Because("Headers include Accept header");
        await Assert.That(output.Headers.GetValues(AcceptHeaderName).First()).IsEqualTo("application/json");

        await Assert.That(output.Headers.Contains("key1")).IsTrue().Because("Headers include key1 header");
        await Assert.That(output.Headers.GetValues("key1").First()).IsEqualTo("val1");
        await Assert.That(output.Headers.Contains("key2")).IsTrue().Because("Headers include key2 header");
        await Assert.That(output.Headers.GetValues("key2").First()).IsEqualTo("val2");
    }

    /// <summary>The last write wins for a header collection combined with a dynamic header.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task LastWriteWinsWhenHeaderCollectionAndDynamicHeader()
    {
        const string authHeader = "LetMeIn";
        const int id = 6;
        var headerCollection = new Dictionary<string, string>
        {
            { AuthorizationHeaderName, "OpenSesame" }
        };

        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IDummyHttpApi.FetchSomeStuffWithDynamicHeaderCollectionAndDynamicHeader));
        var output = await factory([id, authHeader, headerCollection]);

        await Assert.That(output.Headers.Contains(AuthorizationHeaderName)).IsTrue().Because(AuthorizationHeaderReason);
        await Assert.That(output.Headers.GetValues(AuthorizationHeaderName).First()).IsEqualTo("OpenSesame");

        fixture = new();
        factory = fixture.BuildRequestFactoryForMethod(
            nameof(
                IDummyHttpApi.FetchSomeStuffWithDynamicHeaderCollectionAndDynamicHeaderOrderFlipped));
        output = await factory([id, headerCollection, authHeader]);

        await Assert.That(output.Headers.Contains(AuthorizationHeaderName)).IsTrue().Because(AuthorizationHeaderReason);
        await Assert.That(output.Headers.GetValues(AuthorizationHeaderName).First()).IsEqualTo(authHeader);
    }

    /// <summary>A null header collection does not blow up.</summary>
    /// <param name="interfaceMethodName">The name of the interface method to build.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [Arguments(nameof(IDummyHttpApi.FetchSomeStuffWithDynamicHeaderCollection))]
    [Arguments(nameof(IDummyHttpApi.DeleteSomeStuffWithDynamicHeaderCollection))]
    [Arguments(nameof(IDummyHttpApi.PutSomeStuffWithDynamicHeaderCollection))]
    [Arguments(nameof(IDummyHttpApi.PostSomeStuffWithDynamicHeaderCollection))]
    [Arguments(nameof(IDummyHttpApi.PatchSomeStuffWithDynamicHeaderCollection))]
    public async Task NullHeaderCollectionDoesntBlowUp(string interfaceMethodName)
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(interfaceMethodName);
        var output = await factory([6, null!]);

        await Assert.That(output.Headers.Contains(UserAgentHeaderName)).IsTrue().Because(UserAgentHeaderReason);
        await Assert.That(output.Headers.GetValues(UserAgentHeaderName).First()).IsEqualTo(RefitTestClientUserAgent);
        await Assert.That(output.Headers.Contains(ApiVersionHeaderName)).IsTrue().Because(ApiVersionHeaderReason);
        await Assert.That(output.Headers.GetValues(ApiVersionHeaderName).First()).IsEqualTo("1");

        await Assert.That(output.Headers.Contains(AuthorizationHeaderName)).IsTrue().Because(AuthorizationHeaderReason);
        await Assert.That(output.Headers.GetValues(AuthorizationHeaderName).First()).IsEqualTo("SRSLY aHR0cDovL2kuaW1ndXIuY29tL0NGRzJaLmdpZg==");
        await Assert.That(output.Headers.Contains(AcceptHeaderName)).IsTrue().Because("Headers include Accept header");
        await Assert.That(output.Headers.GetValues(AcceptHeaderName).First()).IsEqualTo("application/json");
    }

    /// <summary>A header collection can unset headers.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task HeaderCollectionCanUnsetHeaders()
    {
        var headerCollection = new Dictionary<string, string>
        {
            { AuthorizationHeaderName, string.Empty },
            { ApiVersionHeaderName, null! }
        };

        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IDummyHttpApi.FetchSomeStuffWithDynamicHeaderCollection));
        var output = await factory([6, headerCollection]);

        await Assert.That(!output.Headers.Contains(ApiVersionHeaderName)).IsTrue().Because("Headers does not include Api-Version header");

        await Assert.That(output.Headers.Contains(AuthorizationHeaderName)).IsTrue().Because(AuthorizationHeaderReason);
        await Assert.That(output.Headers.GetValues(AuthorizationHeaderName).First()).IsEqualTo(string.Empty);
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
        var output = await factory([6, someProperty]);

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
        var output = await factory([6, someProperty, someOtherProperty]);

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
        var output = await factory([6, someProperty, someOtherProperty]);

        const int expectedPropertyCount = 3;

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
            [42])!;
        await task;

        await Assert.That(testHttpMessageHandler.RequestMessage!.RequestUri!.ToString()).IsEqualTo("http://api/foo/bar/42");
    }

    /// <summary>A dynamic authorization header and content do not blow up.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DontBlowUpWithDynamicAuthorizationHeaderAndContent()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod("PutSomeContentWithAuthorization");
        var output = await factory(
            [7, new { Octocat = "Dunetocat" }, "Basic RnVjayB5ZWFoOmhlYWRlcnMh"]);

        await Assert.That(output.Headers.Authorization).IsNotNull();
        await Assert.That(output.Headers.Authorization!.Parameter).IsEqualTo("RnVjayB5ZWFoOmhlYWRlcnMh");
    }

    /// <summary>A dynamic content type is honoured.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SuchFlexibleContentTypeWow()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            "PutSomeStuffWithDynamicContentType");
        var output = await factory(
            [7, "such \"refit\" is \"amaze\" wow", "text/dson"]);

        await Assert.That(output.Content).IsNotNull();
        await Assert.That(output.Content!.Headers.ContentType).IsNotNull();
        await Assert.That(output.Content!.Headers.ContentType!.MediaType).IsEqualTo("text/dson");
    }

    /// <summary>Body content gets URL encoded.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task BodyContentGetsUrlEncoded()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.RunRequest("PostSomeUrlEncodedStuff");
        var output = await factory(
            [
                6,

                // Baz is intentionally blank to verify empty values are preserved rather than stripped.
                new UrlEncodedBody(Foo: "Something", Bar: 100, Baz: string.Empty)
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
        var factory = fixture.RunRequest("PostSomeUrlEncodedStuff");
        var output = await factory(
            [
                6,

                // Baz is intentionally blank to verify empty values are preserved rather than stripped.
                new UrlEncodedBodyWithCollection(Foo: "Something", Bar: 100, FooBar: _intArray57, Baz: string.Empty)
            ]);

        await Assert.That(output.SendContent).IsEqualTo("Foo=Something&Bar=100&FooBar=5%2C7&Baz=");
    }

    /// <summary>A form field gets aliased.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task FormFieldGetsAliased()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.RunRequest("PostSomeAliasedUrlEncodedStuff");
        var output = await factory(
            [
                6,
                new SomeRequestData { ReadablePropertyName = 99 }
            ]);

        await Assert.That(output.SendContent).IsEqualTo("rpn=99");
    }

    /// <summary>A custom parameter formatter is applied.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task CustomParmeterFormatter()
    {
        var settings = new RefitSettings
        {
            UrlParameterFormatter = new TestUrlParameterFormatter("custom-parameter")
        };
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>(settings);

        var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuff");
        var output = await factory([5]);

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
}
