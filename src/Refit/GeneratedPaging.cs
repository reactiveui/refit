// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Runtime helpers used by source-generated <see cref="PagedAttribute"/> methods.</summary>
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public static class GeneratedPaging
{
    /// <summary>The header that carries links, named by its registered relation types.</summary>
    private const string LinkHeaderName = "Link";

    /// <summary>The relation type of the link to the following page.</summary>
    private const string NextRelation = "next";

    /// <summary>Computes the offset of the page after one that began at <paramref name="offset"/>, from the total item count.</summary>
    /// <typeparam name="TItem">The item type carried by a page.</typeparam>
    /// <param name="offset">The offset that fetched the page.</param>
    /// <param name="items">The items of the page; <see langword="null"/> counts as none.</param>
    /// <param name="total">The total number of items the API reports, or <see langword="null"/> when it does not.</param>
    /// <returns>The following offset, or the end of the sequence when the total is unknown, the page was empty, or the offset reaches the total.</returns>
    public static PageContinuation<int> NextOffset<TItem>(int offset, IEnumerable<TItem>? items, long? total)
    {
        var count = Count(items);
        var next = offset + count;
        return total is { } known && count > 0 && next < known ? PageContinuation.To(next) : default;
    }

    /// <summary>Creates the continuation for a token that a page holds as a nullable value.</summary>
    /// <typeparam name="TToken">The token type.</typeparam>
    /// <param name="next">The token read from the page.</param>
    /// <returns>A continuation to <paramref name="next"/>, or the end of the sequence when it has no value.</returns>
    public static PageContinuation<TToken> NextValue<TToken>(TToken? next)
        where TToken : struct =>
        next.HasValue ? PageContinuation.To(next.Value) : default;

    /// <summary>Reads the first value of a response header.</summary>
    /// <param name="response">The response to read.</param>
    /// <param name="name">The header name.</param>
    /// <returns>The first value, or <see langword="null"/> when the header is absent.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public static string? HeaderValue(IApiResponse response, string name)
    {
        ArgumentExceptionHelper.ThrowIfNull(response);
        ArgumentExceptionHelper.ThrowIfNull(name);

        if (response.Headers is not { } headers || !headers.TryGetValues(name, out var values))
        {
            return null;
        }

        // TryGetValues only succeeds for a header that has at least one value.
        using var enumerator = values.GetEnumerator();
        _ = enumerator.MoveNext();
        return enumerator.Current;
    }

    /// <summary>Reads the link to the following page from a response header.</summary>
    /// <param name="response">The response to read.</param>
    /// <param name="headerName">The header name; <c>Link</c> is read for its <c>rel="next"</c> relation and any other header as the whole link.</param>
    /// <returns>The link, or <see langword="null"/> when the response names none.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The header value is not a URI.</exception>
    public static Uri? NextLink(IApiResponse response, string headerName)
    {
        ArgumentExceptionHelper.ThrowIfNull(response);
        ArgumentExceptionHelper.ThrowIfNull(headerName);

        return string.Equals(headerName, LinkHeaderName, StringComparison.OrdinalIgnoreCase)
            ? response.GetLink(NextRelation)
            : ToLink(HeaderValue(response, headerName));
    }

    /// <summary>Converts the text of a next-page link to a URI.</summary>
    /// <param name="link">The link text.</param>
    /// <returns>The URI, which may be relative, or <see langword="null"/> when the text is <see langword="null"/> or empty.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="link"/> is not a URI.</exception>
    public static Uri? ToLink(string? link)
    {
        if (string.IsNullOrEmpty(link))
        {
            return null;
        }

        return Uri.TryCreate(link, UriKind.RelativeOrAbsolute, out var uri)
            ? uri
            : throw new InvalidOperationException("The next-page link is not a valid URI.");
    }

    /// <summary>Creates a policy that follows links only to the origin of the client's base address.</summary>
    /// <param name="client">The client the links are requested through.</param>
    /// <returns>A policy restricted to the client's origin.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The client has no base address.</exception>
    public static NextLinkOriginPolicy SameOrigin(HttpClient client)
    {
        ArgumentExceptionHelper.ThrowIfNull(client);
        return NextLinkOriginPolicy.Allow(
            client.BaseAddress ?? throw new InvalidOperationException("The client has no base address, so the origin of a next link cannot be compared with it."));
    }

    /// <summary>Counts the items of a page without enumerating a collection.</summary>
    /// <typeparam name="TItem">The item type.</typeparam>
    /// <param name="items">The items; <see langword="null"/> counts as none.</param>
    /// <returns>The number of items.</returns>
    private static int Count<TItem>(IEnumerable<TItem>? items)
    {
        if (items is null)
        {
            return 0;
        }

        if (items is ICollection<TItem> collection)
        {
            return collection.Count;
        }

        var count = 0;
        foreach (var item in items)
        {
            _ = item;
            count++;
        }

        return count;
    }
}
