// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using Refit.Testing;

namespace Refit.Tests;

/// <summary>Integration tests that exercise <see cref="RestService"/> end to end against a mock HTTP handler.</summary>
public partial class RestServiceIntegrationTests
{
    /// <summary>URL for the httpbin.org get endpoint over HTTPS.</summary>
    private const string HttpBinGetUrl = "https://httpbin.org/get";

    /// <summary>Base URL for httpbin.org over HTTPS.</summary>
    private const string HttpsHttpBinBaseUrl = "https://httpbin.org";

    /// <summary>Base URL for github.com.</summary>
    private const string GitHubBaseUrl = "https://github.com";

    /// <summary>URL for the github.com foo endpoint.</summary>
    private const string GitHubFooUrl = "https://github.com/foo";

    /// <summary>URL for the github.com foo endpoint with a trailing query marker.</summary>
    private const string GitHubFooQueryUrl = "https://github.com/foo?";

    /// <summary>Expected integer response value.</summary>
    private const int ExpectedIntResponse = 4;

    /// <summary>Sample query parameter value.</summary>
    private const int SampleParameterValue = 4;

    /// <summary>First sample nullable-collection query value.</summary>
    private const int CollectionValueThree = 3;

    /// <summary>Second sample nullable-collection query value.</summary>
    private const int CollectionValueFour = 4;

    /// <summary>Verifies a generic method can return multiple response shapes.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GenericMethodTest()
    {
        const string response = "4";

        var handler = new StubHttp
        {
            {
                Route.Get(HttpBinGetUrl),
                Reply.Json(response)
            },
            {
                Route.Get(HttpBinGetUrl),
                Reply.Json(response)
            },
            {
                Route.Get(HttpBinGetUrl),
                Reply.Json(response)
            },
        };

        var settings = handler.ToSettings();

        var myParams = new Dictionary<string, object>
        {
            ["FirstName"] = "John",
            ["LastName"] = RamboLastName,
            ["Address"] = (Zip: 9999, Street: "HomeStreet 99")
        };

        var fixture = RestService.For<IHttpBinApi<HttpBinGet, Dictionary<string, object>, int>>(
            HttpsHttpBinBaseUrl,
            settings);

        // Use the generic to get it as an ApiResponse of string
        var resp = await fixture.GetQuery1<ApiResponse<string>>(myParams);
        await Assert.That(resp.Content).IsEqualTo(response);

        // Get as string
        var resp1 = await fixture.GetQuery1<string>(myParams);

        await Assert.That(resp1).IsEqualTo(response);

        var resp2 = await fixture.GetQuery1<int>(myParams);
        await Assert.That(resp2).IsEqualTo(ExpectedIntResponse);

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies methods inherited from base interfaces are routed correctly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task InheritedMethodTest()
    {
        var handler = new StubHttp
        {
            {
                Route.Get(HttpBinGetUrl),
                Reply.Json(nameof(IAmInterfaceA.Ping))
            },
            {
                Route.Get(HttpBinGetUrl),
                Reply.Json(nameof(IAmInterfaceB.Pong))
            },
            {
                Route.Get(HttpBinGetUrl),
                Reply.Json(nameof(IAmInterfaceC.Pang))
            },
        };

        var fixture = handler.CreateClient<IAmInterfaceC>(HttpsHttpBinBaseUrl);

        var resp = await fixture.Ping();
        await Assert.That(resp).IsEqualTo(nameof(IAmInterfaceA.Ping));

        resp = await fixture.Pong();
        await Assert.That(resp).IsEqualTo(nameof(IAmInterfaceB.Pong));

        resp = await fixture.Pang();
        await Assert.That(resp).IsEqualTo(nameof(IAmInterfaceC.Pang));

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies an interface inheriting only base methods is routed correctly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task InheritedInterfaceWithOnlyBaseMethodsTest()
    {
        var handler = new StubHttp
        {
            {
                Route.Get(HttpBinGetUrl),
                Reply.Json(nameof(IAmInterfaceA.Ping))
            },
            {
                Route.Get(HttpBinGetUrl),
                Reply.Json(nameof(IAmInterfaceB.Pong))
            },
        };

        var fixture = handler.CreateClient<IContainAandB>(HttpsHttpBinBaseUrl);

        var resp = await fixture.Ping();
        await Assert.That(resp).IsEqualTo(nameof(IAmInterfaceA.Ping));

        resp = await fixture.Pong();
        await Assert.That(resp).IsEqualTo(nameof(IAmInterfaceB.Pong));

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies non-Refit base methods throw while Refit methods are routed.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task InheritedInterfaceWithoutRefitInBaseMethodsTest()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = "https://httpbin.org/doSomething", Query = [("parameter", "4")] },
                Reply.Json(nameof(IImplementTheInterfaceAndUseRefit.DoSomething))
            },
            {
                Route.Get("https://httpbin.org/DoSomethingElse"),
                Reply.Json(nameof(IImplementTheInterfaceAndUseRefit.DoSomethingElse))
            },
        };

        var fixture = handler.CreateClient<IImplementTheInterfaceAndUseRefit>(HttpsHttpBinBaseUrl);

        await fixture.DoSomething(SampleParameterValue);

        await fixture.DoSomethingElse();

        // base non refit method should throw NotImplementedException
        await Assert.That(
            () => ((IAmInterfaceEWithNoRefit<int>)fixture).DoSomethingElse()).ThrowsExactly<NotImplementedException>();

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies non-Refit methods overriding the base throw while base Refit methods respond.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task InheritedInterfaceWithoutRefitMethodsOverrideBaseTest()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = HttpBinGetUrl, Query = [("result", "Test")] },
                Reply.Json(nameof(IAmInterfaceD.Test))
            },
        };

        var fixture = handler.CreateClient<IImplementTheInterfaceAndDontUseRefit>(HttpsHttpBinBaseUrl);

        // inherited non refit method should throw NotImplementedException
        await Assert.That(
            () => (Task)fixture.Test()).ThrowsExactly<NotImplementedException>();

        // base Refit method should respond
        await ((IAmInterfaceD)fixture).Test();

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies dictionary dynamic query parameters are echoed correctly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DictionaryDynamicQueryParametersTest()
    {
        var handler = new StubHttp
        {
            {
                Route.Get(HttpBinGetUrl),
                Reply.Json("""
                            {"url": "https://httpbin.org/get?hardcoded=true&FirstName=John&LastName=Rambo&Address_Zip=9999&Address_Street=HomeStreet 99",
                             "args": {"Address_Street": "HomeStreet 99","Address_Zip": "9999","FirstName": "John","LastName": "Rambo","hardcoded": "true"}}
                            """)
            },
        };

        var settings = handler.ToSettings();

        var myParams = new Dictionary<string, object>
        {
            ["FirstName"] = "John",
            ["LastName"] = RamboLastName,
            ["Address"] = (Zip: 9999, Street: "HomeStreet 99")
        };

        var fixture = RestService.For<IHttpBinApi<HttpBinGet, Dictionary<string, object>, int>>(
            HttpsHttpBinBaseUrl,
            settings);

        var resp = await fixture.GetQuery(myParams);

        await Assert.That(resp.Args!["FirstName"]).IsEqualTo("John");
        await Assert.That(resp.Args["LastName"]).IsEqualTo(RamboLastName);
        await Assert.That(resp.Args["Address_Zip"]).IsEqualTo("9999");
    }

    /// <summary>Verifies complex dynamic query parameters work with an included parameter name.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ComplexDynamicQueryparametersTestWithIncludeParameterName()
    {
        var handler = new StubHttp
        {
            {
                Route.Get(HttpBinGetUrl),
                Reply.Json("""
                            {"url": "https://httpbin.org/get?search.FirstName=John&search.LastName=Rambo&search.Addr.Zip=9999&search.Addr.Street=HomeStreet 99",
                             "args": {"search.Addr.Street": "HomeStreet 99","search.Addr.Zip": "9999","search.FirstName": "John","search.LastName": "Rambo"}}
                            """)
            },
        };

        var settings = handler.ToSettings();

        var myParams = new MyComplexQueryParams
        {
            FirstName = "John",
            LastName = RamboLastName,
            Address = new() { Postcode = 9999, Street = "HomeStreet 99" },
        };

        var fixture = RestService.For<IHttpBinApi<HttpBinGet, MyComplexQueryParams, int>>(
            HttpBinGetUrl,
            settings);

        var resp = await fixture.GetQueryWithIncludeParameterName(myParams);

        await Assert.That(resp.Args!["search.FirstName"]).IsEqualTo("John");
        await Assert.That(resp.Args["search.LastName"]).IsEqualTo(RamboLastName);
        await Assert.That(resp.Args["search.Addr.Zip"]).IsEqualTo("9999");
    }

    /// <summary>Verifies a GET request works for a service declared outside a namespace.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ServiceOutsideNamespaceGetRequest()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = "http://foo/", Where = static r => r.Content is null },
                Reply.Json("Ok")
            },
        };

        var fixture = handler.CreateClient<IServiceWithoutNamespace>("http://foo");

        await fixture.GetRoot();

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a POST request works for a service declared outside a namespace.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ServiceOutsideNamespacePostRequest()
    {
        var handler = new StubHttp
        {
            {
                Route.Post("http://foo/"),
                Reply.Json("Ok")
            },
        };

        var fixture = handler.CreateClient<IServiceWithoutNamespace>("http://foo");

        await fixture.PostRoot();

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies content can be serialized as XML.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CanSerializeContentAsXml()
    {
        var contentSerializer = new XmlContentSerializer();

        var handler = new StubHttp
        {
            {
                new RouteMatcher
                {
                    Method = HttpMethod.Post,
                    Template = "/users",
                    Headers = [("Content-Type", "application/xml; charset=utf-8")]
                },
                Reply.From(static req => new(HttpStatusCode.OK) { Content = new StringContent("<User><Name>Created</Name></User>", Encoding.UTF8, "application/xml") })
            },
        };

        var fixture = handler.CreateClient<IGitHubApi>("https://api.github.com", new RefitSettings { ContentSerializer = contentSerializer });

        var result = await fixture.CreateUser(new()).ConfigureAwait(false);

        await Assert.That(result.Name).IsEqualTo("Created");

        await handler.VerifyAllCalledAsync();
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
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = GitHubFooQueryUrl, ExactQuery = string.Empty },
                Reply.Status(HttpStatusCode.OK)
            },
        };

        var fixture = handler.CreateClient<IQueryApi>(GitHubBaseUrl);

        await fixture.EmptyQuery();

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a whitespace-only query string produces an empty query.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task WhiteSpaceQueryShouldBeEmpty()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = GitHubFooQueryUrl, ExactQuery = string.Empty },
                Reply.Status(HttpStatusCode.OK)
            },
        };

        var fixture = handler.CreateClient<IQueryApi>(GitHubBaseUrl);

        await fixture.WhiteSpaceQuery();

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a query string with an empty key produces an empty query.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EmptyQueryKeyShouldBeEmpty()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = GitHubFooQueryUrl, ExactQuery = string.Empty },
                Reply.Status(HttpStatusCode.OK)
            },
        };

        var fixture = handler.CreateClient<IQueryApi>(GitHubBaseUrl);

        await fixture.EmptyQueryKey();

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a query string with an empty value is preserved.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EmptyQueryValueShouldNotBeEmpty()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = "https://github.com/foo?key=", ExactQuery = "key=" },
                Reply.Status(HttpStatusCode.OK)
            },
        };

        var fixture = handler.CreateClient<IQueryApi>(GitHubBaseUrl);

        await fixture.EmptyQueryValue();

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a query string with empty key and value produces an empty query.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EmptyQueryKeyAndValueShouldBeEmpty()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = GitHubFooQueryUrl, ExactQuery = string.Empty },
                Reply.Status(HttpStatusCode.OK)
            },
        };

        var fixture = handler.CreateClient<IQueryApi>(GitHubBaseUrl);

        await fixture.EmptyQueryKeyAndValue();

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies unescaped query characters are escaped.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UnescapedQueryShouldBeEscaped()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = GitHubFooUrl, ExactQuery = "key%2C=value%2C&key1%28=value1%28" },
                Reply.Status(HttpStatusCode.OK)
            },
        };

        var fixture = handler.CreateClient<IQueryApi>(GitHubBaseUrl);

        await fixture.UnescapedQuery();

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies already-escaped query characters stay escaped.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EscapedQueryShouldStillBeEscaped()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = GitHubFooUrl, ExactQuery = "key%2C=value%2C&key1%28=value1%28" },
                Reply.Status(HttpStatusCode.OK)
            },
        };

        var fixture = handler.CreateClient<IQueryApi>(GitHubBaseUrl);

        await fixture.EscapedQuery();

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a parameter-mapped query produces the expected query string.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ParameterMappedQueryShouldWork()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = GitHubFooUrl, ExactQuery = "key1=value1" },
                Reply.Status(HttpStatusCode.OK)
            },
        };

        var fixture = handler.CreateClient<IQueryApi>(GitHubBaseUrl);

        await fixture.ParameterMappedQuery("key1", "value1");

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a parameter-mapped query escapes its values.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ParameterMappedQueryShouldEscape()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = GitHubFooUrl, ExactQuery = "key1%2C=value1%2C" },
                Reply.Status(HttpStatusCode.OK)
            },
        };

        var fixture = handler.CreateClient<IQueryApi>(GitHubBaseUrl);

        await fixture.ParameterMappedQuery("key1,", "value1,");

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a nullable integer collection query produces the expected query string.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task NullableIntCollectionQuery()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = GitHubFooUrl, ExactQuery = "values=3%2C4%2C" },
                Reply.Status(HttpStatusCode.OK)
            },
        };

        var fixture = handler.CreateClient<IQueryApi>(GitHubBaseUrl);

        await fixture.NullableIntCollectionQuery([CollectionValueThree, CollectionValueFour, null]);

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a URL fragment is stripped before sending.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ShouldStripFragment()
    {
        var handler = new StubHttp
        {
            {
                Route.Get(GitHubFooUrl),
                Reply.Status(HttpStatusCode.OK)
            },
        };

        var fixture = handler.CreateClient<IFragmentApi>(GitHubBaseUrl);

        await fixture.Fragment();

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies an empty URL fragment is stripped before sending.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ShouldStripEmptyFragment()
    {
        var handler = new StubHttp
        {
            {
                Route.Get(GitHubFooUrl),
                Reply.Status(HttpStatusCode.OK)
            },
        };

        var fixture = handler.CreateClient<IFragmentApi>(GitHubBaseUrl);

        await fixture.EmptyFragment();

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies multiple URL fragments are stripped before sending.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ShouldStripManyFragments()
    {
        var handler = new StubHttp
        {
            {
                Route.Get(GitHubFooUrl),
                Reply.Status(HttpStatusCode.OK)
            },
        };

        var fixture = handler.CreateClient<IFragmentApi>(GitHubBaseUrl);

        await fixture.ManyFragments();

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a parameter-based URL fragment is stripped before sending.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ShouldStripParameterFragment()
    {
        var handler = new StubHttp
        {
            {
                Route.Get(GitHubFooUrl),
                Reply.Status(HttpStatusCode.OK)
            },
        };

        var fixture = handler.CreateClient<IFragmentApi>(GitHubBaseUrl);

        await fixture.ParameterFragment("ignore");

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a fragment after a query string is stripped while the query is kept.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ShouldStripFragmentAfterQuery()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = GitHubFooUrl, ExactQuery = "key=value" },
                Reply.Status(HttpStatusCode.OK)
            },
        };

        var fixture = handler.CreateClient<IFragmentApi>(GitHubBaseUrl);

        await fixture.FragmentAfterQuery();

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a query string after a fragment marker is stripped.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ShouldStripQueryAfterFragment()
    {
        var handler = new StubHttp
        {
            {
                Route.Get(GitHubFooUrl),
                Reply.Status(HttpStatusCode.OK)
            },
        };

        var fixture = handler.CreateClient<IFragmentApi>(GitHubBaseUrl);

        await fixture.QueryAfterFragment();

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a task is canceled when cancellation is requested.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task TaskShouldCancelWhenRequested()
    {
        using var tokenSource = new CancellationTokenSource();
        var token = tokenSource.Token;

        var fixture = RestService.For<ICancellableApi>(GitHubBaseUrl);

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

        var fixture = RestService.For<ICancellableApi>(GitHubBaseUrl);

        await tokenSource.CancelAsync();
        var task = fixture.GetWithCancellationAndReturn(token);
        await Assert.That(() => (Task)task).ThrowsExactly<TaskCanceledException>();
    }

    /// <summary>Verifies a null cancellation token is ignored.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task NullableCancellationTokenShouldBeIgnored()
    {
        var handler = new StubHttp
        {
            {
                Route.Get(GitHubFooUrl),
                Reply.Status(HttpStatusCode.OK)
            },
        };

        var fixture = handler.CreateClient<ICancellableApi>(GitHubBaseUrl);

        await fixture.GetWithNullableCancellation(null);

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies types with the same simple name in different namespaces are routed correctly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task TypeCollisionTest()
    {
        const string url = HttpBinGetUrl;

        var handler = new StubHttp
        {
            {
                Route.Get(url),
                Reply.Json("{ }")
            },
            {
                Route.Get(url),
                Reply.Json("{ }")
            },
        };

        var settings = handler.ToSettings();

        var fixtureA = RestService.For<ITypeCollisionApiA>(url, settings);

        var respA = await fixtureA.SomeARequest();

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
        const string url = HttpBinGetUrl;

        var handler = new StubHttp
        {
            {
                Route.Get(url + "/"),
                Reply.Json("{ }")
            },
            {
                Route.Get(url + "/"),
                Reply.Json("{ }")
            },
            {
                Route.Get(url + "/"),
                Reply.Json("{ }")
            },
        };

        var settings = handler.ToSettings();

        var fixtureA = RestService.For<INamespaceCollisionApi>(url, settings);

        var respA = await fixtureA.SomeRequest();

        var fixtureB = RestService.For<CollisionA.INamespaceCollisionApi>(url, settings);

        var respB = await fixtureB.SomeRequest();

        var fixtureC = RestService.For<CollisionB.INamespaceCollisionApi>(url, settings);

        var respC = await fixtureC.SomeRequest();

        await Assert.That(respA).IsTypeOf<CollisionA.SomeType>();
        await Assert.That(respB).IsTypeOf<CollisionA.SomeType>();
        await Assert.That(respC).IsTypeOf<CollisionB.SomeType>();
    }
}
