// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Verifies the generated paged methods that follow links, and the origin policy that restricts them.</summary>
public partial class GeneratedPagedMethodTests
{
    /// <summary>A link that names another origin.</summary>
    private const string ForeignLink = "https://other.example/link/items?page=1";

    /// <summary>Verifies a same-origin method follows each link the server returns until none remains.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Links_FollowEachNextLinkTheServerReturns()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);

        var items = await CollectAsync(server.CreateClientFor<IGeneratedPagingApi>().LinkItems());

        await Assert.That(Ids(items)).IsEqualTo(AllIds);
        await Assert.That(RequestedQueries(server)).IsEqualTo("|?page=1|?page=2");
    }

    /// <summary>Verifies a relative next link resolves against the URI of the request that fetched the page.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Links_RelativeLinkResolvesAgainstTheRequestUri()
    {
        var server = new PagedApiHandler(TotalItems, PageSize) { NextLinkForPage = static page => $"/link/items?page={page + 1}" };

        var items = await CollectAsync(server.CreateClientFor<IGeneratedPagingApi>().LinkItems());

        await Assert.That(Ids(items)).IsEqualTo(AllIds);
        await Assert.That(server.Requests.Count).IsEqualTo(PageCount);
    }

    /// <summary>Verifies a link to another origin is refused by a same-origin method and is never requested.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Links_SameOriginRefusesAForeignLinkWithoutRequestingIt()
    {
        var server = new PagedApiHandler(TotalItems, PageSize) { NextLinkForPage = static _ => ForeignLink };

        List<PagedItem> seen = [];
        await Assert.That(async () =>
        {
            await foreach (var item in server.CreateClientFor<IGeneratedPagingApi>().LinkItems())
            {
                seen.Add(item);
            }
        }).Throws<InvalidOperationException>();

        await Assert.That(Ids(seen)).IsEqualTo("1,2,3");
        await Assert.That(server.Requests.Count).IsEqualTo(1);
    }

    /// <summary>Verifies a method that lists its origins refuses a link to any other origin.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Links_ListedOriginRefusesAForeignLink()
    {
        var server = new PagedApiHandler(TotalItems, PageSize) { NextLinkForPage = static _ => ForeignLink };

        await Assert.That(async () => await CollectAsync(server.CreateClientFor<IGeneratedPagingApi>().ListedOriginLinkItems())).Throws<InvalidOperationException>();

        await Assert.That(server.Requests.Count).IsEqualTo(1);
    }

    /// <summary>Verifies a method that lists its origins follows links to a listed origin.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Links_ListedOriginFollowsLinksToThatOrigin()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);

        var items = await CollectAsync(server.CreateClientFor<IGeneratedPagingApi>().ListedOriginLinkItems());

        await Assert.That(Ids(items)).IsEqualTo(AllIds);
    }

    /// <summary>Verifies an unrestricted method follows a link to any origin.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Links_AnyOriginFollowsAForeignLink()
    {
        var server = new PagedApiHandler(TotalItems, PageSize) { NextLinkForPage = static page => page == 0 ? ForeignLink : null };

        var items = await CollectAsync(server.CreateClientFor<IGeneratedPagingApi>().AnyOriginLinkItems());

        await Assert.That(Ids(items)).IsEqualTo(AllIds);
        await Assert.That(string.Join("|", server.Requests.Select(static request => request.Uri.Host))).IsEqualTo("api.example.com|other.example|api.example.com");
    }

    /// <summary>Verifies the <c>rel="next"</c> entry of a <c>Link</c> header is followed and the other entries are ignored.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task LinkHeader_FollowsTheNextRelation()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);

        var items = await CollectAsync(server.CreateClientFor<IGeneratedPagingApi>().LinkHeaderItems());

        await Assert.That(Ids(items)).IsEqualTo(AllIds);
        await Assert.That(RequestedQueries(server)).IsEqualTo("|?page=1|?page=2");
    }

    /// <summary>Verifies a link with credentials in its authority is refused, so credentials are never forwarded to it.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Links_AuthorityWithUserInformationIsRefused()
    {
        var server = new PagedApiHandler(TotalItems, PageSize) { NextLinkForPage = static _ => "https://user:secret@api.example.com/link/items?page=1" };

        await Assert.That(async () => await CollectAsync(server.CreateClientFor<IGeneratedPagingApi>().LinkItems())).Throws<InvalidOperationException>();

        await Assert.That(server.Requests.Count).IsEqualTo(1);
    }
}
