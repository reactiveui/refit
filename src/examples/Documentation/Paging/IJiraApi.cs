// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation.Paging;

/// <summary>Searches Jira issues, which pages by offset and reports the total number of matches.</summary>
internal interface IJiraApi
{
    /// <summary>Searches for issues.</summary>
    /// <param name="jql">The Jira query.</param>
    /// <param name="maxResults">The most issues on a page.</param>
    /// <param name="startAt">The offset of the first issue to request.</param>
    /// <returns>A lazy sequence of issues that ends when the offset reaches the total the search reports.</returns>
    [Get("/rest/api/3/search")]
    [Paged(Items = nameof(JiraSearchResult.Issues), Total = nameof(JiraSearchResult.Total))]
    PagedEnumerable<JiraSearchResult, JiraIssue> Search(string jql, int maxResults, [PageToken] int startAt = 0);
}
