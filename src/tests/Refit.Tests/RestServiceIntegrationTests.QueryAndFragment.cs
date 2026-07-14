// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net;
using Refit.Testing;

namespace Refit.Tests;

/// <summary>Integration tests covering query-string edge cases and URL fragment stripping.</summary>
public partial class RestServiceIntegrationTests
{
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
}
