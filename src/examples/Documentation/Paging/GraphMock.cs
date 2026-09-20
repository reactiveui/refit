// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Specialized;
using System.Globalization;
using System.Web;

namespace Refit.Documentation.Paging;

/// <summary>A stand-in for Microsoft Graph that serves seven users and names each next page with an absolute link.</summary>
internal static class GraphMock
{
    /// <summary>The origin of Microsoft Graph.</summary>
    internal const string Origin = "https://graph.microsoft.com";

    /// <summary>The number of users the directory holds.</summary>
    internal const int UserCount = 7;

    /// <summary>The query parameter that carries the position of the next page.</summary>
    private const string SkipToken = "$skiptoken";

    /// <summary>Answers a users request.</summary>
    /// <param name="request">The request.</param>
    /// <returns>The JSON response.</returns>
    internal static HttpResponseMessage Serve(HttpRequestMessage request)
    {
        NameValueCollection query = HttpUtility.ParseQueryString(request.RequestUri!.Query);
        int top = int.Parse(query["$top"]!, CultureInfo.InvariantCulture);
        int start = string.IsNullOrEmpty(query[SkipToken]) ? 0 : int.Parse(query[SkipToken]!, CultureInfo.InvariantCulture);
        int end = Math.Min(start + top, UserCount);

        GraphUserPage page = new();
        for (int index = start; index < end; index++)
        {
            page.Value.Add(new() { Id = $"user-{index + 1}", DisplayName = $"User {index + 1}" });
        }

        if (end < UserCount)
        {
            page.NextLink = $"{Origin}/v1.0/users?$top={top}&{SkipToken}={end}";
        }

        return PagingReplies.Json(page, PagingJsonContext.Default.GraphUserPage);
    }
}
