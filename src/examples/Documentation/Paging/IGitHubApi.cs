// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation.Paging;

/// <summary>Lists GitHub repositories, which pages with the <c>Link</c> header of a bare JSON array.</summary>
internal interface IGitHubApi
{
    /// <summary>Lists the repositories of an organization.</summary>
    /// <param name="org">The organization login.</param>
    /// <param name="perPage">The most repositories on a page.</param>
    /// <returns>A lazy sequence of repositories; a link is followed only when it points at <c>api.github.com</c>.</returns>
    [Get("/orgs/{org}/repos")]
    [Headers("User-Agent: refit-paging-sample", "Accept: application/vnd.github+json")]
    [Paged(NextHeader = "Link", Origins = ["https://api.github.com"])]
    PagedEnumerable<ApiResponse<List<GitHubRepo>>, GitHubRepo> ListRepositories(string org, [AliasAs("per_page")] int perPage);
}
