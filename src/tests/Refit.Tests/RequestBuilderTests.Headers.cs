// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Tests for <see cref="RequestBuilderImplementation{T}"/> HTTP header population.</summary>
public partial class RequestBuilderTests
{
    /// <summary>Hardcoded headers appear in the request headers.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task HardcodedHeadersShouldBeInHeaders()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();

        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IDummyHttpApi.FetchSomeStuffWithHardcodedHeaders));
        var output = await factory([SampleId]);

        await Assert.That(output.Headers.Contains(UserAgentHeaderName)).IsTrue().Because(UserAgentHeaderReason);
        await Assert.That(output.Headers.UserAgent.ToString()).IsEqualTo(RefitTestClientUserAgent);
        await Assert.That(output.Headers.Contains(ApiVersionHeaderName)).IsTrue().Because(ApiVersionHeaderReason);
        await Assert.That(output.Headers.GetValues(ApiVersionHeaderName).Single()).IsEqualTo("2");
        await Assert.That(output.Headers.Contains(AcceptHeaderName)).IsTrue().Because(AcceptHeaderReason);
        await Assert.That(output.Headers.Accept.ToString()).IsEqualTo(JsonContentType);
    }

    /// <summary>Empty hardcoded headers appear in the request headers.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task EmptyHardcodedHeadersShouldBeInHeaders()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            "FetchSomeStuffWithEmptyHardcodedHeader");
        var output = await factory([SampleId]);

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
        var output = await factory([SampleId]);

        await Assert.That(output.Headers.Contains(UserAgentHeaderName)).IsTrue().Because(UserAgentHeaderReason);
        await Assert.That(output.Headers.UserAgent.ToString()).IsEqualTo(RefitTestClientUserAgent);
        await Assert.That(output.Headers.Contains(ApiVersionHeaderName)).IsFalse().Because(ApiVersionHeaderReason);
    }

    /// <summary>Content headers can be hardcoded.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ContentHeadersCanBeHardcoded()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            "PostSomeStuffWithHardCodedContentTypeHeader");
        var output = await factory([SampleId, "stuff"]);

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
        var output = await factory([SampleId, "stuff"]);

        await Assert.That(output.Content!.Headers.ContentType!.ToString()).IsEqualTo("application/soap+xml");
    }

    /// <summary>A dynamic header appears in the request headers.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DynamicHeaderShouldBeInHeaders()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithDynamicHeader");
        var output = await factory([SampleId, "Basic RnVjayB5ZWFoOmhlYWRlcnMh"]);

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
        var output = await factory([SampleId, JoyCatEmojiValue]);

        await Assert.That(output.Headers.Contains(EmojiHeaderName)).IsTrue().Because(EmojiHeaderReason);
        await Assert.That(output.Headers.GetValues(EmojiHeaderName).First()).IsEqualTo(JoyCatEmojiValue);
    }

    /// <summary>An empty dynamic header appears in the request headers.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task EmptyDynamicHeaderShouldBeInHeaders()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithCustomHeader");
        var output = await factory([SampleId, string.Empty]);

        await Assert.That(output.Headers.Contains(EmojiHeaderName)).IsTrue().Because(EmojiHeaderReason);
        await Assert.That(output.Headers.GetValues(EmojiHeaderName).First()).IsEqualTo(string.Empty);
    }

    /// <summary>A null dynamic header is not present in the request headers.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task NullDynamicHeaderShouldNotBeInHeaders()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithDynamicHeader");
        var output = await factory([SampleId, null!]);

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
        var output = await factory([SampleId, JoyCatEmojiValue]);

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
        var output = await factory([SampleId, new { Foo = "bar" }, ":smile_cat:"]);

        await Assert.That(output.Headers.Contains(ApiVersionHeaderName)).IsTrue().Because(ApiVersionHeaderReason);
        await Assert.That(output.Headers.Contains(EmojiHeaderName)).IsTrue().Because(EmojiHeaderReason);
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
        var output = await factory([SampleId, headerCollection]);

        await Assert.That(output.Headers.Contains(UserAgentHeaderName)).IsTrue().Because(UserAgentHeaderReason);
        await Assert.That(output.Headers.GetValues(UserAgentHeaderName).First()).IsEqualTo(RefitTestClientUserAgent);
        await Assert.That(output.Headers.Contains(ApiVersionHeaderName)).IsTrue().Because(ApiVersionHeaderReason);
        await Assert.That(output.Headers.GetValues(ApiVersionHeaderName).First()).IsEqualTo("1");

        await Assert.That(output.Headers.Contains(AuthorizationHeaderName)).IsTrue().Because(AuthorizationHeaderReason);
        await Assert.That(output.Headers.GetValues(AuthorizationHeaderName).First()).IsEqualTo("SRSLY aHR0cDovL2kuaW1ndXIuY29tL0NGRzJaLmdpZg==");
        await Assert.That(output.Headers.Contains(AcceptHeaderName)).IsTrue().Because(AcceptHeaderReason);
        await Assert.That(output.Headers.GetValues(AcceptHeaderName).First()).IsEqualTo(JsonContentType);

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
        var output = await factory([SampleId, null!]);

        await Assert.That(output.Headers.Contains(UserAgentHeaderName)).IsTrue().Because(UserAgentHeaderReason);
        await Assert.That(output.Headers.GetValues(UserAgentHeaderName).First()).IsEqualTo(RefitTestClientUserAgent);
        await Assert.That(output.Headers.Contains(ApiVersionHeaderName)).IsTrue().Because(ApiVersionHeaderReason);
        await Assert.That(output.Headers.GetValues(ApiVersionHeaderName).First()).IsEqualTo("1");

        await Assert.That(output.Headers.Contains(AuthorizationHeaderName)).IsTrue().Because(AuthorizationHeaderReason);
        await Assert.That(output.Headers.GetValues(AuthorizationHeaderName).First()).IsEqualTo("SRSLY aHR0cDovL2kuaW1ndXIuY29tL0NGRzJaLmdpZg==");
        await Assert.That(output.Headers.Contains(AcceptHeaderName)).IsTrue().Because(AcceptHeaderReason);
        await Assert.That(output.Headers.GetValues(AcceptHeaderName).First()).IsEqualTo(JsonContentType);
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
        var output = await factory([SampleId, headerCollection]);

        await Assert.That(!output.Headers.Contains(ApiVersionHeaderName)).IsTrue().Because("Headers does not include Api-Version header");

        await Assert.That(output.Headers.Contains(AuthorizationHeaderName)).IsTrue().Because(AuthorizationHeaderReason);
        await Assert.That(output.Headers.GetValues(AuthorizationHeaderName).First()).IsEqualTo(string.Empty);
    }

    /// <summary>A dynamic authorization header and content do not blow up.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DontBlowUpWithDynamicAuthorizationHeaderAndContent()
    {
        const int id = 7;
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod("PutSomeContentWithAuthorization");
        var output = await factory(
            [id, new { Octocat = "Dunetocat" }, "Basic RnVjayB5ZWFoOmhlYWRlcnMh"]);

        await Assert.That(output.Headers.Authorization).IsNotNull();
        await Assert.That(output.Headers.Authorization!.Parameter).IsEqualTo("RnVjayB5ZWFoOmhlYWRlcnMh");
    }

    /// <summary>A dynamic content type is honoured.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SuchFlexibleContentTypeWow()
    {
        const int id = 7;
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRequestFactoryForMethod(
            "PutSomeStuffWithDynamicContentType");
        var output = await factory(
            [id, "such \"refit\" is \"amaze\" wow", "text/dson"]);

        await Assert.That(output.Content).IsNotNull();
        await Assert.That(output.Content!.Headers.ContentType).IsNotNull();
        await Assert.That(output.Content!.Headers.ContentType!.MediaType).IsEqualTo("text/dson");
    }

    /// <summary>With the default settings the reflection builder sends a malformed header value verbatim.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ValidateHeadersDisabledSendsMalformedHeaderVerbatim()
    {
        var fixture = new RequestBuilderImplementation<IHeaderValidationApi>();
        var factory = fixture.BuildRequestFactoryForMethod(nameof(IHeaderValidationApi.Get));

        var output = await factory([MalformedHeaderValue]);

        await Assert.That(output.Headers.GetValues(ValidatedHeaderName)).IsEquivalentTo([MalformedHeaderValue]);
    }

    /// <summary>With <see cref="RefitSettings.ValidateHeaders"/> enabled the reflection builder rejects a malformed header value.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ValidateHeadersEnabledThrowsForMalformedHeader()
    {
        var fixture = new RequestBuilderImplementation<IHeaderValidationApi>(new RefitSettings { ValidateHeaders = true });
        var factory = fixture.BuildRequestFactoryForMethod(nameof(IHeaderValidationApi.Get));

        await Assert.That(() => (Task)factory([MalformedHeaderValue])).Throws<FormatException>();
    }
}
