// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Specialized;
using System.Globalization;
using System.Web;

namespace Refit.Documentation.Paging;

/// <summary>A stand-in for Jira search that serves seven matching issues by offset.</summary>
internal static class JiraMock
{
    /// <summary>The origin of the Jira site.</summary>
    internal const string Origin = "https://contoso.atlassian.net";

    /// <summary>The number of issues the search matches.</summary>
    internal const int IssueCount = 7;

    /// <summary>Answers a search request.</summary>
    /// <param name="request">The request.</param>
    /// <returns>The JSON response.</returns>
    internal static HttpResponseMessage Serve(HttpRequestMessage request)
    {
        NameValueCollection query = HttpUtility.ParseQueryString(request.RequestUri!.Query);
        int startAt = int.Parse(query["startAt"]!, CultureInfo.InvariantCulture);
        int maxResults = int.Parse(query["maxResults"]!, CultureInfo.InvariantCulture);
        int end = Math.Min(startAt + maxResults, IssueCount);

        JiraSearchResult result = new() { StartAt = startAt, MaxResults = maxResults, Total = IssueCount };
        for (int index = startAt; index < end; index++)
        {
            result.Issues.Add(new() { Key = $"OPS-{index + 1}", Summary = $"Issue {index + 1}" });
        }

        return PagingReplies.Json(result, PagingJsonContext.Default.JiraSearchResult);
    }
}
