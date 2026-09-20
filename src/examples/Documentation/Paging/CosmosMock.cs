// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Refit.Documentation.Paging;

/// <summary>A stand-in for Azure Cosmos DB queries that serves seven documents and continues with a header.</summary>
internal static class CosmosMock
{
    /// <summary>The origin of the account's document endpoint.</summary>
    internal const string Origin = "https://contoso.documents.azure.com";

    /// <summary>The header that carries the continuation in both directions.</summary>
    internal const string ContinuationHeader = "x-ms-continuation";

    /// <summary>The number of documents the collection holds.</summary>
    internal const int DocumentCount = 7;

    /// <summary>The header that states the most documents on a page.</summary>
    private const string MaxItemCountHeader = "x-ms-max-item-count";

    /// <summary>The text a continuation starts with.</summary>
    private const string ContinuationPrefix = "+RID:~cosmos";

    /// <summary>The number of categories the documents cycle through.</summary>
    private const int CategoryCount = 2;

    /// <summary>Answers a document query.</summary>
    /// <param name="request">The request.</param>
    /// <returns>The JSON response, with the continuation in a header while documents remain.</returns>
    internal static HttpResponseMessage Serve(HttpRequestMessage request)
    {
        int maxItemCount = int.Parse(HeaderValue(request, MaxItemCountHeader)!, CultureInfo.InvariantCulture);
        string? continuation = HeaderValue(request, ContinuationHeader);
        int start = continuation is null ? 0 : Decode(continuation);
        int end = Math.Min(start + maxItemCount, DocumentCount);

        DocumentFeed feed = new() { Count = end - start };
        for (int index = start; index < end; index++)
        {
            feed.Documents.Add(new() { Id = $"order-{index + 1}", Category = index % CategoryCount == 0 ? "books" : "music" });
        }

        return end < DocumentCount
            ? PagingReplies.Json(feed, PagingJsonContext.Default.DocumentFeed, (ContinuationHeader, Encode(end)))
            : PagingReplies.Json(feed, PagingJsonContext.Default.DocumentFeed);
    }

    /// <summary>Reads a request header that carries a single value.</summary>
    /// <param name="request">The request.</param>
    /// <param name="name">The header name.</param>
    /// <returns>The value, or <see langword="null"/> when the request has no such header.</returns>
    private static string? HeaderValue(HttpRequestMessage request, string name) =>
        request.Headers.TryGetValues(name, out IEnumerable<string>? values) ? string.Concat(values) : null;

    /// <summary>Makes a position an opaque continuation.</summary>
    /// <param name="position">The index of the next document.</param>
    /// <returns>The continuation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string Encode(int position) => $"{ContinuationPrefix}{position.ToString(CultureInfo.InvariantCulture)}";

    /// <summary>Reads the position from a continuation.</summary>
    /// <param name="continuation">The continuation.</param>
    /// <returns>The index of the next document.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Decode(string continuation) => int.Parse(continuation[ContinuationPrefix.Length..], CultureInfo.InvariantCulture);
}
