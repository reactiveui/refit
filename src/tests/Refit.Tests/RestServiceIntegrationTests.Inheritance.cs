// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RichardSzalay.MockHttp;

namespace Refit.Tests;

/// <summary>Integration tests that exercise <see cref="RestService"/> end to end against a mock HTTP handler.</summary>
public partial class RestServiceIntegrationTests
{
    /// <summary>Verifies a generic method can return multiple response shapes.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GenericMethodTest()
    {
        var mockHttp = new MockHttpMessageHandler();

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        const string response = "4";
        _ = mockHttp
            .Expect(HttpMethod.Get, "https://httpbin.org/get")
            .Respond("application/json", response);

        var myParams = new Dictionary<string, object>
        {
            ["FirstName"] = "John",
            ["LastName"] = "Rambo",
            ["Address"] = (Zip: 9999, Street: "HomeStreet 99")
        };

        var fixture = RestService.For<IHttpBinApi<HttpBinGet, Dictionary<string, object>, int>>(
            "https://httpbin.org",
            settings);

        // Use the generic to get it as an ApiResponse of string
        var resp = await fixture.GetQuery1<ApiResponse<string>>(myParams);
        await Assert.That(resp.Content).IsEqualTo(response);

        mockHttp.VerifyNoOutstandingExpectation();

        _ = mockHttp
            .Expect(HttpMethod.Get, "https://httpbin.org/get")
            .Respond("application/json", response);

        // Get as string
        var resp1 = await fixture.GetQuery1<string>(myParams);

        await Assert.That(resp1).IsEqualTo(response);

        mockHttp.VerifyNoOutstandingExpectation();

        _ = mockHttp
            .Expect(HttpMethod.Get, "https://httpbin.org/get")
            .Respond("application/json", response);

        var resp2 = await fixture.GetQuery1<int>(myParams);
        await Assert.That(resp2).IsEqualTo(4);

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies methods inherited from base interfaces are routed correctly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task InheritedMethodTest()
    {
        var mockHttp = new MockHttpMessageHandler();

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        var fixture = RestService.For<IAmInterfaceC>("https://httpbin.org", settings);

        _ = mockHttp
            .Expect(HttpMethod.Get, "https://httpbin.org/get")
            .Respond("application/json", nameof(IAmInterfaceA.Ping));
        var resp = await fixture.Ping();
        await Assert.That(resp).IsEqualTo(nameof(IAmInterfaceA.Ping));
        mockHttp.VerifyNoOutstandingExpectation();

        _ = mockHttp
            .Expect(HttpMethod.Get, "https://httpbin.org/get")
            .Respond("application/json", nameof(IAmInterfaceB.Pong));
        resp = await fixture.Pong();
        await Assert.That(resp).IsEqualTo(nameof(IAmInterfaceB.Pong));
        mockHttp.VerifyNoOutstandingExpectation();

        _ = mockHttp
            .Expect(HttpMethod.Get, "https://httpbin.org/get")
            .Respond("application/json", nameof(IAmInterfaceC.Pang));
        resp = await fixture.Pang();
        await Assert.That(resp).IsEqualTo(nameof(IAmInterfaceC.Pang));
        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies an interface inheriting only base methods is routed correctly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task InheritedInterfaceWithOnlyBaseMethodsTest()
    {
        var mockHttp = new MockHttpMessageHandler();

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        var fixture = RestService.For<IContainAandB>("https://httpbin.org", settings);

        _ = mockHttp
            .Expect(HttpMethod.Get, "https://httpbin.org/get")
            .Respond("application/json", nameof(IAmInterfaceA.Ping));
        var resp = await fixture.Ping();
        await Assert.That(resp).IsEqualTo(nameof(IAmInterfaceA.Ping));
        mockHttp.VerifyNoOutstandingExpectation();

        _ = mockHttp
            .Expect(HttpMethod.Get, "https://httpbin.org/get")
            .Respond("application/json", nameof(IAmInterfaceB.Pong));
        resp = await fixture.Pong();
        await Assert.That(resp).IsEqualTo(nameof(IAmInterfaceB.Pong));
        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies non-Refit base methods throw while Refit methods are routed.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task InheritedInterfaceWithoutRefitInBaseMethodsTest()
    {
        var mockHttp = new MockHttpMessageHandler();

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        var fixture = RestService.For<IImplementTheInterfaceAndUseRefit>(
            "https://httpbin.org",
            settings);

        _ = mockHttp
            .Expect(HttpMethod.Get, "https://httpbin.org/doSomething")
            .WithQueryString("parameter", "4")
            .Respond("application/json", nameof(IImplementTheInterfaceAndUseRefit.DoSomething));

        await fixture.DoSomething(4);
        mockHttp.VerifyNoOutstandingExpectation();

        _ = mockHttp
            .Expect(HttpMethod.Get, "https://httpbin.org/DoSomethingElse")
            .Respond("application/json", nameof(IImplementTheInterfaceAndUseRefit.DoSomethingElse));
        await fixture.DoSomethingElse();
        mockHttp.VerifyNoOutstandingExpectation();

        // base non refit method should throw NotImplementedException
        await Assert.That(
            () => ((IAmInterfaceEWithNoRefit<int>)fixture).DoSomethingElse()).ThrowsExactly<NotImplementedException>();
        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies non-Refit methods overriding the base throw while base Refit methods respond.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task InheritedInterfaceWithoutRefitMethodsOverrideBaseTest()
    {
        var mockHttp = new MockHttpMessageHandler();

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        var fixture = RestService.For<IImplementTheInterfaceAndDontUseRefit>(
            "https://httpbin.org",
            settings);

        // inherited non refit method should throw NotImplementedException
        await Assert.That(
            () => (Task)fixture.Test()).ThrowsExactly<NotImplementedException>();
        mockHttp.VerifyNoOutstandingExpectation();

        // base Refit method should respond
        _ = mockHttp
            .Expect(HttpMethod.Get, "https://httpbin.org/get")
            .WithQueryString("result", "Test")
            .Respond("application/json", nameof(IAmInterfaceD.Test));

        await ((IAmInterfaceD)fixture).Test();
        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies dictionary dynamic query parameters are echoed correctly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DictionaryDynamicQueryParametersTest()
    {
        var mockHttp = new MockHttpMessageHandler();

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        _ = mockHttp
            .Expect(HttpMethod.Get, "https://httpbin.org/get")
            .Respond(
                "application/json",
                """
                {"url": "https://httpbin.org/get?hardcoded=true&FirstName=John&LastName=Rambo&Address_Zip=9999&Address_Street=HomeStreet 99",
                 "args": {"Address_Street": "HomeStreet 99","Address_Zip": "9999","FirstName": "John","LastName": "Rambo","hardcoded": "true"}}
                """);

        var myParams = new Dictionary<string, object>
        {
            ["FirstName"] = "John",
            ["LastName"] = "Rambo",
            ["Address"] = (Zip: 9999, Street: "HomeStreet 99")
        };

        var fixture = RestService.For<IHttpBinApi<HttpBinGet, Dictionary<string, object>, int>>(
            "https://httpbin.org",
            settings);

        var resp = await fixture.GetQuery(myParams);

        await Assert.That(resp.Args!["FirstName"]).IsEqualTo("John");
        await Assert.That(resp.Args["LastName"]).IsEqualTo("Rambo");
        await Assert.That(resp.Args["Address_Zip"]).IsEqualTo("9999");
    }

    /// <summary>Verifies complex dynamic query parameters work with an included parameter name.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ComplexDynamicQueryparametersTestWithIncludeParameterName()
    {
        var mockHttp = new MockHttpMessageHandler();

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        _ = mockHttp
            .Expect(HttpMethod.Get, "https://httpbin.org/get")
            .Respond(
                "application/json",
                """
                {"url": "https://httpbin.org/get?search.FirstName=John&search.LastName=Rambo&search.Addr.Zip=9999&search.Addr.Street=HomeStreet 99",
                 "args": {"search.Addr.Street": "HomeStreet 99","search.Addr.Zip": "9999","search.FirstName": "John","search.LastName": "Rambo"}}
                """);

        var myParams = new MyComplexQueryParams
        {
            FirstName = "John",
            LastName = "Rambo",
            Address = new() { Postcode = 9999, Street = "HomeStreet 99" },
        };

        var fixture = RestService.For<IHttpBinApi<HttpBinGet, MyComplexQueryParams, int>>(
            "https://httpbin.org/get",
            settings);

        var resp = await fixture.GetQueryWithIncludeParameterName(myParams);

        await Assert.That(resp.Args!["search.FirstName"]).IsEqualTo("John");
        await Assert.That(resp.Args["search.LastName"]).IsEqualTo("Rambo");
        await Assert.That(resp.Args["search.Addr.Zip"]).IsEqualTo("9999");
    }

    /// <summary>Verifies a GET request works for a service declared outside a namespace.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ServiceOutsideNamespaceGetRequest()
    {
        var mockHttp = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        _ = mockHttp
            .Expect(HttpMethod.Get, "http://foo/")

            // We can't add HttpContent to a GET request,
            // because HttpClient doesn't allow it and it will
            // blow up at runtime
            .With(r => r.Content is null)
            .Respond("application/json", "Ok");

        var fixture = RestService.For<IServiceWithoutNamespace>("http://foo", settings);

        await fixture.GetRoot();

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a POST request works for a service declared outside a namespace.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ServiceOutsideNamespacePostRequest()
    {
        var mockHttp = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        _ = mockHttp.Expect(HttpMethod.Post, "http://foo/").Respond("application/json", "Ok");

        var fixture = RestService.For<IServiceWithoutNamespace>("http://foo", settings);

        await fixture.PostRoot();

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies content can be serialized as XML.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CanSerializeContentAsXml()
    {
        var mockHttp = new MockHttpMessageHandler();
        var contentSerializer = new XmlContentSerializer();
        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => mockHttp,
            ContentSerializer = contentSerializer
        };

        _ = mockHttp
            .Expect(HttpMethod.Post, "/users")
            .WithHeaders("Content-Type:application/xml; charset=utf-8")
            .Respond(
                req =>
                    new(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            "<User><Name>Created</Name></User>",
                            Encoding.UTF8,
                            "application/xml")
                    });

        var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);

        var result = await fixture.CreateUser(new()).ConfigureAwait(false);

        await Assert.That(result.Name).IsEqualTo("Created");

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a trailing forward slash is trimmed from the base URL.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ShouldTrimTrailingForwardSlashFromBaseUrl()
    {
        const string expectedBaseAddress = "http://example.com/api";
        const string inputBaseAddress = "http://example.com/api/";

        var fixture = RestService.For<ITrimTrailingForwardSlashApi>(inputBaseAddress);

        await Assert.That(expectedBaseAddress).IsEqualTo(fixture.Client.BaseAddress!.AbsoluteUri);
    }

    /// <summary>Verifies a null host URL throws an <see cref="ArgumentException"/>.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ShouldThrowArgumentExceptionIfHostUrlIsNull()
    {
        try
        {
            _ = RestService.For<IValidApi>(hostUrl: null!);
        }
        catch (ArgumentException ex)
        {
            await Assert.That(ex.ParamName).IsEqualTo("hostUrl");
            return;
        }

        Assert.Fail("Exception not thrown.");
    }

    /// <summary>Verifies an empty host URL throws an <see cref="ArgumentException"/>.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ShouldThrowArgumentExceptionIfHostUrlIsEmpty()
    {
        try
        {
            _ = RestService.For<IValidApi>(hostUrl: string.Empty);
        }
        catch (ArgumentException ex)
        {
            await Assert.That(ex.ParamName).IsEqualTo("hostUrl");
            return;
        }

        Assert.Fail("Exception not thrown.");
    }

    /// <summary>Verifies a whitespace host URL throws an <see cref="ArgumentException"/>.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ShouldThrowArgumentExceptionIfHostUrlIsWhitespace()
    {
        try
        {
            _ = RestService.For<IValidApi>(hostUrl: " ");
        }
        catch (ArgumentException ex)
        {
            await Assert.That(ex.ParamName).IsEqualTo("hostUrl");
            return;
        }

        Assert.Fail("Exception not thrown.");
    }

    /// <summary>Verifies the non-generic create overload returns a usable instance.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [SuppressMessage("Usage", "CA2263:Prefer generic overload", Justification = "Test intentionally exercises the non-generic RestService.For(Type, string) overload.")]
    public async Task NonGenericCreate()
    {
        const string expectedBaseAddress = "http://example.com/api";
        const string inputBaseAddress = "http://example.com/api/";

        var fixture = (ITrimTrailingForwardSlashApi)RestService.For(
            typeof(ITrimTrailingForwardSlashApi),
            inputBaseAddress);

        await Assert.That(expectedBaseAddress).IsEqualTo(fixture.Client.BaseAddress!.AbsoluteUri);
    }

    /// <summary>Verifies an empty query string produces an empty query.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EmptyQueryShouldBeEmpty()
    {
        var mockHttp = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp, };

        _ = mockHttp
            .Expect(HttpMethod.Get, "https://github.com/foo?")
            .WithExactQueryString(string.Empty)
            .Respond(HttpStatusCode.OK);

        var fixture = RestService.For<IQueryApi>("https://github.com", settings);

        await fixture.EmptyQuery();

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a whitespace-only query string produces an empty query.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task WhiteSpaceQueryShouldBeEmpty()
    {
        var mockHttp = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp, };

        _ = mockHttp
            .Expect(HttpMethod.Get, "https://github.com/foo?")
            .WithExactQueryString(string.Empty)
            .Respond(HttpStatusCode.OK);

        var fixture = RestService.For<IQueryApi>("https://github.com", settings);

        await fixture.WhiteSpaceQuery();

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a query string with an empty key produces an empty query.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EmptyQueryKeyShouldBeEmpty()
    {
        var mockHttp = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp, };

        _ = mockHttp
            .Expect(HttpMethod.Get, "https://github.com/foo?")
            .WithExactQueryString(string.Empty)
            .Respond(HttpStatusCode.OK);

        var fixture = RestService.For<IQueryApi>("https://github.com", settings);

        await fixture.EmptyQueryKey();

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a query string with an empty value is preserved.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EmptyQueryValueShouldNotBeEmpty()
    {
        var mockHttp = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp, };

        _ = mockHttp
            .Expect(HttpMethod.Get, "https://github.com/foo?key=")
            .WithExactQueryString("key=")
            .Respond(HttpStatusCode.OK);

        var fixture = RestService.For<IQueryApi>("https://github.com", settings);

        await fixture.EmptyQueryValue();

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a query string with empty key and value produces an empty query.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EmptyQueryKeyAndValueShouldBeEmpty()
    {
        var mockHttp = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp, };

        _ = mockHttp
            .Expect(HttpMethod.Get, "https://github.com/foo?")
            .WithExactQueryString(string.Empty)
            .Respond(HttpStatusCode.OK);

        var fixture = RestService.For<IQueryApi>("https://github.com", settings);

        await fixture.EmptyQueryKeyAndValue();

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies unescaped query characters are escaped.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UnescapedQueryShouldBeEscaped()
    {
        var mockHttp = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp, };

        _ = mockHttp
            .Expect(HttpMethod.Get, "https://github.com/foo")
            .WithExactQueryString("key%2C=value%2C&key1%28=value1%28")
            .Respond(HttpStatusCode.OK);

        var fixture = RestService.For<IQueryApi>("https://github.com", settings);

        await fixture.UnescapedQuery();

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies already-escaped query characters stay escaped.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EscapedQueryShouldStillBeEscaped()
    {
        var mockHttp = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp, };

        _ = mockHttp
            .Expect(HttpMethod.Get, "https://github.com/foo")
            .WithExactQueryString("key%2C=value%2C&key1%28=value1%28")
            .Respond(HttpStatusCode.OK);

        var fixture = RestService.For<IQueryApi>("https://github.com", settings);

        await fixture.EscapedQuery();

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a parameter-mapped query produces the expected query string.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ParameterMappedQueryShouldWork()
    {
        var mockHttp = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp, };

        _ = mockHttp
            .Expect(HttpMethod.Get, "https://github.com/foo")
            .WithExactQueryString("key1=value1")
            .Respond(HttpStatusCode.OK);

        var fixture = RestService.For<IQueryApi>("https://github.com", settings);

        await fixture.ParameterMappedQuery("key1", "value1");

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a parameter-mapped query escapes its values.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ParameterMappedQueryShouldEscape()
    {
        var mockHttp = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp, };

        _ = mockHttp
            .Expect(HttpMethod.Get, "https://github.com/foo")
            .WithExactQueryString("key1%2C=value1%2C")
            .Respond(HttpStatusCode.OK);

        var fixture = RestService.For<IQueryApi>("https://github.com", settings);

        await fixture.ParameterMappedQuery("key1,", "value1,");

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a nullable integer collection query produces the expected query string.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task NullableIntCollectionQuery()
    {
        var mockHttp = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp, };

        _ = mockHttp
            .Expect(HttpMethod.Get, "https://github.com/foo")
            .WithExactQueryString("values=3%2C4%2C")
            .Respond(HttpStatusCode.OK);

        var fixture = RestService.For<IQueryApi>("https://github.com", settings);

        await fixture.NullableIntCollectionQuery([3, 4, null]);

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a URL fragment is stripped before sending.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ShouldStripFragment()
    {
        var mockHttp = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp, };

        _ = mockHttp
            .Expect(HttpMethod.Get, "https://github.com/foo")
            .Respond(HttpStatusCode.OK);

        var fixture = RestService.For<IFragmentApi>("https://github.com", settings);

        await fixture.Fragment();

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies an empty URL fragment is stripped before sending.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ShouldStripEmptyFragment()
    {
        var mockHttp = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp, };

        _ = mockHttp
            .Expect(HttpMethod.Get, "https://github.com/foo")
            .Respond(HttpStatusCode.OK);

        var fixture = RestService.For<IFragmentApi>("https://github.com", settings);

        await fixture.EmptyFragment();

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies multiple URL fragments are stripped before sending.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ShouldStripManyFragments()
    {
        var mockHttp = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp, };

        _ = mockHttp
            .Expect(HttpMethod.Get, "https://github.com/foo")
            .Respond(HttpStatusCode.OK);

        var fixture = RestService.For<IFragmentApi>("https://github.com", settings);

        await fixture.ManyFragments();

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a parameter-based URL fragment is stripped before sending.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ShouldStripParameterFragment()
    {
        var mockHttp = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp, };

        _ = mockHttp
            .Expect(HttpMethod.Get, "https://github.com/foo")
            .Respond(HttpStatusCode.OK);

        var fixture = RestService.For<IFragmentApi>("https://github.com", settings);

        await fixture.ParameterFragment("ignore");

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a fragment after a query string is stripped while the query is kept.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ShouldStripFragmentAfterQuery()
    {
        var mockHttp = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp, };

        _ = mockHttp
            .Expect(HttpMethod.Get, "https://github.com/foo")
            .WithExactQueryString("key=value")
            .Respond(HttpStatusCode.OK);

        var fixture = RestService.For<IFragmentApi>("https://github.com", settings);

        await fixture.FragmentAfterQuery();

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a query string after a fragment marker is stripped.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ShouldStripQueryAfterFragment()
    {
        var mockHttp = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp, };

        _ = mockHttp
            .Expect(HttpMethod.Get, "https://github.com/foo")
            .Respond(HttpStatusCode.OK);

        var fixture = RestService.For<IFragmentApi>("https://github.com", settings);

        await fixture.QueryAfterFragment();

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a task is canceled when cancellation is requested.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task TaskShouldCancelWhenRequested()
    {
        using var tokenSource = new CancellationTokenSource();
        var token = tokenSource.Token;

        var fixture = RestService.For<ICancellableApi>("https://github.com");

        await tokenSource.CancelAsync();
        var task = fixture.GetWithCancellation(token);
        await Assert.That(async () => await task).ThrowsExactly<TaskCanceledException>();
    }

    /// <summary>Verifies a task with a result is canceled when cancellation is requested.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task TaskResultShouldCancelWhenRequested()
    {
        using var tokenSource = new CancellationTokenSource();
        var token = tokenSource.Token;

        var fixture = RestService.For<ICancellableApi>("https://github.com");

        await tokenSource.CancelAsync();
        var task = fixture.GetWithCancellationAndReturn(token);
        var exception = await Assert.That(() => (Task)task).ThrowsExactly<ApiRequestException>();
        await Assert.That(exception!.InnerException).IsTypeOf<TaskCanceledException>();
    }

    /// <summary>Verifies a null cancellation token is ignored.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task NullableCancellationTokenShouldBeIgnored()
    {
        var mockHttp = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp, };

        _ = mockHttp
            .Expect(HttpMethod.Get, "https://github.com/foo")
            .Respond(HttpStatusCode.OK);

        var fixture = RestService.For<ICancellableApi>("https://github.com", settings);

        await fixture.GetWithNullableCancellation(null);

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies types with the same simple name in different namespaces are routed correctly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task TypeCollisionTest()
    {
        var mockHttp = new MockHttpMessageHandler();

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp, };

        const string url = "https://httpbin.org/get";

        _ = mockHttp.Expect(HttpMethod.Get, url).Respond("application/json", "{ }");

        var fixtureA = RestService.For<ITypeCollisionApiA>(url, settings);

        var respA = await fixtureA.SomeARequest();

        _ = mockHttp.Expect(HttpMethod.Get, url).Respond("application/json", "{ }");

        var fixtureB = RestService.For<ITypeCollisionApiB>(url, settings);

        var respB = await fixtureB.SomeBRequest();

        await Assert.That(respA).IsTypeOf<CollisionA.SomeType>();
        await Assert.That(respB).IsTypeOf<CollisionB.SomeType>();
    }

    /// <summary>Verifies the same type name across multiple namespaces is routed correctly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SameTypeNameInMultipleNamespacesTest()
    {
        var mockHttp = new MockHttpMessageHandler();

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp, };

        const string url = "https://httpbin.org/get";

        _ = mockHttp.Expect(HttpMethod.Get, url + "/").Respond("application/json", "{ }");

        var fixtureA = RestService.For<INamespaceCollisionApi>(url, settings);

        var respA = await fixtureA.SomeRequest();

        _ = mockHttp.Expect(HttpMethod.Get, url + "/").Respond("application/json", "{ }");

        var fixtureB = RestService.For<CollisionA.INamespaceCollisionApi>(url, settings);

        var respB = await fixtureB.SomeRequest();

        _ = mockHttp.Expect(HttpMethod.Get, url + "/").Respond("application/json", "{ }");

        var fixtureC = RestService.For<CollisionB.INamespaceCollisionApi>(url, settings);

        var respC = await fixtureC.SomeRequest();

        await Assert.That(respA).IsTypeOf<CollisionA.SomeType>();
        await Assert.That(respB).IsTypeOf<CollisionA.SomeType>();
        await Assert.That(respC).IsTypeOf<CollisionB.SomeType>();
    }
}
