// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace Refit.Tests;

/// <summary>
/// A fake server for <see cref="IPagedItemsApi"/> holding items numbered from one, served by cursor, by offset or by link.
/// It records every request so tests can assert what was fetched.
/// </summary>
/// <param name="totalItems">The number of items the server holds.</param>
/// <param name="pageSize">The number of items on a cursor or link page.</param>
internal sealed class PagedApiHandler(int totalItems, int pageSize) : HttpMessageHandler
{
    /// <summary>The origin the fixture API is served from.</summary>
    internal const string Origin = "https://api.example.com";

    /// <summary>The header that carries a continuation in both directions.</summary>
    internal const string ContinuationHeader = "x-ms-continuation";

    /// <summary>The requests received, in arrival order.</summary>
    private readonly ConcurrentQueue<RecordedRequest> _requests = new();

    /// <summary>Gets the requests received, in arrival order.</summary>
    internal IReadOnlyCollection<RecordedRequest> Requests => _requests;

    /// <summary>Gets or sets a step that runs when a request arrives, before its response is produced.</summary>
    internal Func<HttpRequestMessage, CancellationToken, Task>? OnRequest { get; set; }

    /// <summary>Gets or sets the status returned for a zero-based page, or <see langword="null"/> for 200 throughout.</summary>
    internal Func<int, HttpStatusCode>? StatusForPage { get; set; }

    /// <summary>Gets or sets the link a link page carries to the page after the zero-based page, or <see langword="null"/> for the default absolute link.</summary>
    internal Func<int, string?>? NextLinkForPage { get; set; }

    /// <summary>Gets the number of pages a cursor or link listing spans.</summary>
    internal int PageCount => (totalItems + pageSize - 1) / pageSize;

    /// <summary>Creates a client for any Refit interface whose requests reach this server.</summary>
    /// <typeparam name="TApi">The Refit interface.</typeparam>
    /// <returns>The generated client.</returns>
    internal TApi CreateClientFor<TApi>() => RestService.For<TApi>(HttpClientTestFactory.Create(this, new(Origin)));

    /// <summary>Creates a client for <see cref="IPagedItemsApi"/> whose requests reach this server.</summary>
    /// <param name="bearerToken">A token sent as the Authorization header of every request the client makes, or <see langword="null"/> for none.</param>
    /// <returns>The generated client.</returns>
    internal IPagedItemsApi CreateClient(string? bearerToken = null)
    {
        var client = HttpClientTestFactory.Create(this, new(Origin));
        if (bearerToken is not null)
        {
            client.DefaultRequestHeaders.Authorization = new("Bearer", bearerToken);
        }

        return RestService.For<IPagedItemsApi>(client);
    }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var uri = request.RequestUri!;
        _requests.Enqueue(new(uri, request.Headers.Authorization?.ToString(), request.Headers.TryGetValues(ContinuationHeader, out var continuation) ? continuation.First() : null));

        if (OnRequest is { } step)
        {
            await step(request, cancellationToken).ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var (page, json, headers) = Render(request);
        var status = StatusForPage?.Invoke(page) ?? HttpStatusCode.OK;
        HttpResponseMessage response = new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json"), RequestMessage = request };
        foreach (var (name, value) in headers)
        {
            response.Headers.Add(name, value);
        }

        return response;
    }

    /// <summary>Renders the response for a request.</summary>
    /// <param name="request">The request.</param>
    /// <returns>The zero-based page the request addressed, its JSON body and its response headers.</returns>
    private (int Page, string Json, IReadOnlyList<(string Name, string Value)> Headers) Render(HttpRequestMessage request)
    {
        var uri = request.RequestUri!;
        var query = QueryHelpers.ParseQuery(uri.Query);
        if (uri.AbsolutePath == "/cursor/items")
        {
            var page = query.TryGetValue("cursor", out var cursor) ? int.Parse(cursor.ToString()[1..]) : 0;
            var next = page + 1 < PageCount ? $"\"c{page + 1}\"" : "null";
            return (page, $"{{\"items\":{Items(page * pageSize, pageSize)},\"nextCursor\":{next}}}", []);
        }

        if (uri.AbsolutePath == "/offset/items")
        {
            var offset = int.Parse(query["offset"].ToString());
            var limit = int.Parse(query["limit"].ToString());
            return (offset / limit, $"{{\"items\":{Items(offset, limit)},\"total\":{totalItems}}}", []);
        }

        if (uri.AbsolutePath == "/nextoffset/items")
        {
            var offset = int.Parse(query["offset"].ToString());
            var limit = int.Parse(query["limit"].ToString());
            var next = offset + limit < totalItems ? (offset + limit).ToString(CultureInfo.InvariantCulture) : "null";
            return (offset / limit, $"{{\"items\":{Items(offset, limit)},\"nextOffset\":{next}}}", []);
        }

        return uri.AbsolutePath switch
        {
            "/header/items" => RenderHeaderPage(request),
            "/linkheader/items" => RenderLinkHeaderPage(query),
            _ => RenderLinkPage(query),
        };
    }

    /// <summary>Renders a page whose continuation is carried by the continuation header in both directions.</summary>
    /// <param name="request">The request.</param>
    /// <returns>The zero-based page, its JSON body and its response headers.</returns>
    private (int Page, string Json, IReadOnlyList<(string Name, string Value)> Headers) RenderHeaderPage(HttpRequestMessage request)
    {
        var page = request.Headers.TryGetValues(ContinuationHeader, out var continuation) ? int.Parse(continuation.First()[1..]) : 0;
        IReadOnlyList<(string Name, string Value)> headers = page + 1 < PageCount ? [(ContinuationHeader, $"c{page + 1}")] : [];
        return (page, $"{{\"items\":{Items(page * pageSize, pageSize)}}}", headers);
    }

    /// <summary>Renders a bare JSON array page whose next page is named by a <c>Link</c> header.</summary>
    /// <param name="query">The parsed query string.</param>
    /// <returns>The zero-based page, its JSON body and its response headers.</returns>
    private (int Page, string Json, IReadOnlyList<(string Name, string Value)> Headers) RenderLinkHeaderPage(Dictionary<string, StringValues> query)
    {
        var page = query.TryGetValue("page", out var value) ? int.Parse(value.ToString()) : 0;
        IReadOnlyList<(string Name, string Value)> headers = page + 1 < PageCount
            ? [("Link", $"<{NextLinkForPage?.Invoke(page) ?? $"{Origin}/linkheader/items?page={page + 1}"}>; rel=\"next\", <{Origin}/linkheader/items?page={PageCount - 1}>; rel=\"last\"")]
            : [];
        return (page, Items(page * pageSize, pageSize), headers);
    }

    /// <summary>Renders a page whose next page is named by a link in the body.</summary>
    /// <param name="query">The parsed query string.</param>
    /// <returns>The zero-based page, its JSON body and its response headers.</returns>
    private (int Page, string Json, IReadOnlyList<(string Name, string Value)> Headers) RenderLinkPage(Dictionary<string, StringValues> query)
    {
        var linkPage = query.TryGetValue("page", out var value) ? int.Parse(value.ToString()) : 0;
        var link = linkPage + 1 < PageCount ? NextLinkForPage?.Invoke(linkPage) ?? $"{Origin}/link/items?page={linkPage + 1}" : null;
        var nextLink = link is null ? "null" : $"\"{link}\"";
        return (linkPage, $"{{\"items\":{Items(linkPage * pageSize, pageSize)},\"next\":{nextLink}}}", []);
    }

    /// <summary>Renders a JSON array of items.</summary>
    /// <param name="skip">The number of items before the first one rendered.</param>
    /// <param name="take">The most items rendered.</param>
    /// <returns>The JSON array.</returns>
    private string Items(int skip, int take)
    {
        var json = new StringBuilder("[");
        for (var id = skip + 1; id <= Math.Min(totalItems, skip + take); id++)
        {
            _ = json.Append(id == skip + 1 ? string.Empty : ",").Append($"{{\"id\":{id},\"name\":\"item-{id}\"}}");
        }

        return json.Append(']').ToString();
    }
}
