// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Specialized;
using System.Globalization;
using System.Net;
using System.Web;

namespace Refit.Documentation.Paging;

/// <summary>A stand-in for the GitHub REST API that serves seven repositories and links pages with a <c>Link</c> header.</summary>
internal static class GitHubMock
{
    /// <summary>The origin of the GitHub REST API.</summary>
    internal const string Origin = "https://api.github.com";

    /// <summary>The number of repositories the organization holds.</summary>
    internal const int RepositoryCount = 7;

    /// <summary>Answers a repositories request, which GitHub rejects without a User-Agent header.</summary>
    /// <param name="request">The request.</param>
    /// <returns>The JSON response.</returns>
    internal static HttpResponseMessage Serve(HttpRequestMessage request)
    {
        if (request.Headers.UserAgent.Count == 0)
        {
            return new(HttpStatusCode.Forbidden);
        }

        NameValueCollection query = HttpUtility.ParseQueryString(request.RequestUri!.Query);
        int perPage = int.Parse(query["per_page"]!, CultureInfo.InvariantCulture);
        int page = string.IsNullOrEmpty(query["page"]) ? 1 : int.Parse(query["page"]!, CultureInfo.InvariantCulture);
        int lastPage = (RepositoryCount + perPage - 1) / perPage;
        int start = (page - 1) * perPage;
        int end = Math.Min(start + perPage, RepositoryCount);

        List<GitHubRepo> repositories = [];
        for (int index = start; index < end; index++)
        {
            repositories.Add(new() { Id = index + 1, FullName = $"contoso/repo-{index + 1}" });
        }

        string link(int target, string relation) => $"<{Origin}/organizations/1/repos?per_page={perPage}&page={target}>; rel=\"{relation}\"";
        return page < lastPage
            ? PagingReplies.Json(repositories, PagingJsonContext.Default.ListGitHubRepo, ("Link", $"{link(page + 1, "next")}, {link(lastPage, "last")}"))
            : PagingReplies.Json(repositories, PagingJsonContext.Default.ListGitHubRepo);
    }
}
