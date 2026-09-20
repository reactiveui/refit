// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;

namespace Refit.Tests;

/// <summary>Verifies a next link is followed only where the explicit origin policy allows, so credentials go nowhere else.</summary>
public partial class PagedEnumerableTests
{
    /// <summary>The bearer token the client attaches to every request it makes.</summary>
    private const string Secret = "secret-token";

    /// <summary>A link that points at a host other than the API's.</summary>
    private const string ForeignLink = "https://evil.example.net/link/items?page=1";

    /// <summary>Verifies a link to another origin is refused after the current page's items, and never requested.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task OriginPolicy_LinkToAnotherOrigin_IsNeverRequested()
    {
        var server = new PagedApiHandler(TotalItems, PageSize) { NextLinkForPage = static _ => ForeignLink };

        var (seen, failure) = await CollectUntilFailureAsync(LinkItems(server.CreateClient(Secret), SameOrigin));

        await Assert.That(failure).IsTypeOf<InvalidOperationException>();
        await Assert.That(failure!.Message.Contains("evil.example.net", StringComparison.Ordinal)).IsTrue();
        await Assert.That(failure.Message.Contains("page=1", StringComparison.Ordinal)).IsFalse();
        await Assert.That(Ids(seen)).IsEqualTo("1,2,3");
        await Assert.That(server.Requests.Count).IsEqualTo(1);
        await Assert.That(server.Requests.All(static request => request.Uri.Host == "api.example.com")).IsTrue();
    }

    /// <summary>Verifies a scheme downgrade on the API's own host is refused and never requested.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task OriginPolicy_SchemeDowngrade_IsNeverRequested()
    {
        var server = new PagedApiHandler(TotalItems, PageSize) { NextLinkForPage = static _ => "http://api.example.com/link/items?page=1" };

        var (_, failure) = await CollectUntilFailureAsync(LinkItems(server.CreateClient(Secret), SameOrigin));

        await Assert.That(failure).IsTypeOf<InvalidOperationException>();
        await Assert.That(server.Requests.Count).IsEqualTo(1);
    }

    /// <summary>Verifies a link carrying user information is refused and never requested.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task OriginPolicy_LinkWithUserInfo_IsNeverRequested()
    {
        var server = new PagedApiHandler(TotalItems, PageSize) { NextLinkForPage = static _ => "https://user:pw@api.example.com/link/items?page=1" };

        var (_, failure) = await CollectUntilFailureAsync(LinkItems(server.CreateClient(Secret), SameOrigin));

        await Assert.That(failure).IsTypeOf<InvalidOperationException>();
        await Assert.That(server.Requests.Count).IsEqualTo(1);
    }

    /// <summary>Verifies links on the allowed origin are followed with the client's credentials.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task OriginPolicy_LinkOnTheAllowedOrigin_IsFollowedWithCredentials()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);

        var items = await CollectAsync(LinkItems(server.CreateClient(Secret), SameOrigin));

        await Assert.That(Ids(items)).IsEqualTo(AllIds);
        await Assert.That(server.Requests.All(static request => request.Authorization == $"Bearer {Secret}")).IsTrue();
    }

    /// <summary>Verifies an allow-list with several origins follows a link to any of them.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task OriginPolicy_AnyAllowedOrigin_IsFollowed()
    {
        var server = new PagedApiHandler(TotalItems, PageSize) { NextLinkForPage = static page => $"https://cdn.example.com/link/items?page={page + 1}" };
        var policy = NextLinkOriginPolicy.Allow(new Uri(PagedApiHandler.Origin), new Uri("https://cdn.example.com"));

        var items = await CollectAsync(LinkItems(server.CreateClient(), policy));

        await Assert.That(Ids(items)).IsEqualTo(AllIds);
        await Assert.That(server.Requests.Count(static request => request.Uri.Host == "cdn.example.com")).IsEqualTo(PageCount - 1);
    }

    /// <summary>Verifies the unrestricted policy follows another origin and forwards the client's credentials there, as its name warns.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task OriginPolicy_Unrestricted_FollowsAnotherOriginWithTheClientCredentials()
    {
        var server = new PagedApiHandler(TotalItems, PageSize) { NextLinkForPage = static page => page == 0 ? ForeignLink : $"{PagedApiHandler.Origin}/link/items?page={page + 1}" };

        var items = await CollectAsync(LinkItems(server.CreateClient(Secret), NextLinkOriginPolicy.Unrestricted));

        await Assert.That(Ids(items)).IsEqualTo(AllIds);
        await Assert.That(server.Requests.Any(static request => request.Uri.Host == "evil.example.net" && request.Authorization == $"Bearer {Secret}")).IsTrue();
    }

    /// <summary>Verifies a relative link on a page that is not a response resolves against the first allowed origin.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task OriginPolicy_RelativeLinkOnAPlainDto_ResolvesAgainstTheFirstAllowedOrigin()
    {
        var server = new PagedApiHandler(TotalItems, PageSize) { NextLinkForPage = static page => $"/link/items?page={page + 1}" };
        var api = server.CreateClient();
        var items = PagedEnumerable.FromLinks(
            SameOrigin,
            async (url, cancellationToken) => (url is null ? await api.GetFirstLinkPageAsync(cancellationToken) : await api.GetLinkPageAsync(url, cancellationToken)).Content!,
            static page => page.Items,
            static page => page.Next);

        var collected = await CollectAsync(items);

        await Assert.That(Ids(collected)).IsEqualTo(AllIds);
        await Assert.That(server.Requests.Count).IsEqualTo(PageCount);
    }

    /// <summary>Verifies a response whose request URI is relative resolves a relative link against the first allowed origin.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task OriginPolicy_ResponseWithRelativeRequestUri_ResolvesAgainstTheFirstAllowedOrigin()
    {
        using HttpRequestMessage request = new(HttpMethod.Get, new Uri("/link/items", UriKind.Relative));
        Uri next = new("/link/items?page=1", UriKind.Relative);
        List<Uri?> requested = [];
        var items = PagedEnumerable.FromLinks(
            SameOrigin,
            (url, _) =>
            {
                requested.Add(url);
                LinkPage page = new() { Next = url is null ? next : null };
                return Task.FromResult(new ApiResponse<LinkPage>(new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage = request }, page, new()));
            },
            static response => response.Content?.Items,
            static response => response.Content?.Next);

        await CollectAsync(items);

        await Assert.That(requested[1]!.AbsoluteUri).IsEqualTo($"{PagedApiHandler.Origin}/link/items?page=1");
    }

    /// <summary>Verifies a response reporting no request message resolves a relative link against the first allowed origin.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task OriginPolicy_ResponseWithoutRequestMessage_ResolvesAgainstTheFirstAllowedOrigin()
    {
        List<Uri?> requested = [];
        var items = PagedEnumerable.FromLinks(
            SameOrigin,
            (url, _) =>
            {
                requested.Add(url);
                return Task.FromResult(new RequestlessLinkPage { Next = url is null ? new("/link/items?page=1", UriKind.Relative) : null });
            },
            static page => Array.Empty<PagedItem>(),
            static page => page.Next);

        await CollectAsync(items);

        await Assert.That(requested[1]!.AbsoluteUri).IsEqualTo($"{PagedApiHandler.Origin}/link/items?page=1");
    }

    /// <summary>Collects items until the sequence throws.</summary>
    /// <typeparam name="TPage">The page type.</typeparam>
    /// <param name="source">The sequence to enumerate.</param>
    /// <returns>The items received and the exception that ended the sequence, or <see langword="null"/>.</returns>
    private static async Task<(List<PagedItem> Items, Exception? Failure)> CollectUntilFailureAsync<TPage>(PagedEnumerable<TPage, PagedItem> source)
    {
        List<PagedItem> seen = [];
        try
        {
            await foreach (var item in source)
            {
                seen.Add(item);
            }
        }
        catch (Exception ex)
        {
            return (seen, ex);
        }

        return (seen, null);
    }
}
