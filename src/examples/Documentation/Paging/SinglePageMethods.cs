// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Refit.Testing;

namespace Refit.Documentation.Paging;

/// <summary>Builds a listing from ordinary methods that each return one page.</summary>
internal static class SinglePageMethods
{
    /// <summary>Follows cursors, offsets and Link headers in three local services.</summary>
    /// <param name="token">Cancels page requests and subscriptions.</param>
    /// <returns>Completion after each listing has reached its final page.</returns>
    internal static async Task RunAsync(CancellationToken token)
    {
        RefitSettings settings = RefitSettings.ForJsonContext(PagingJsonContext.Default);
        using StubHttp storage = new() { { Route.Fallback(), Reply.From(GcsMock.Serve) } };
        IGcsApi blobs = storage.CreateGeneratedClient<IGcsApi>(GcsMock.Origin, settings);
        PagedEnumerable<GcsObjectList, GcsObject> byCursor = PagedEnumerable.FromCursor(blobs.ReadPageAsync, static p => p.Items, static p => p.NextPageToken);
        SampleCheck.Equal(GcsMock.ObjectCount, await PageCountObserver<GcsObject>.CountAsync(byCursor.WithPrefetch().ToObservable(), token));
        SampleCheck.Equal(3, await PageCountObserver<GcsObjectList>.CountAsync(byCursor.ToPageObservable(), token));

        using StubHttp search = new() { { Route.Fallback(), Reply.From(JiraMock.Serve) } };
        IJiraApi issues = search.CreateGeneratedClient<IJiraApi>(JiraMock.Origin, settings);
        PagedEnumerable<JiraSearchResult, JiraIssue> byOffset = PagedEnumerable.FromOffset(issues.ReadPageAsync, static p => p.Issues, static (page, offset) =>
        {
            int next = offset + page.Issues.Count;
            return next < page.Total ? next : null;
        });
        SampleCheck.Equal(JiraMock.IssueCount, await PageCountObserver<JiraIssue>.CountAsync(byOffset.ToObservable(), token));
        await ResumeSearchAsync(issues, token);

        using StubHttp hosting = new() { { Route.Fallback(), Reply.From(GitHubMock.Serve) } };
        IGitHubApi repos = hosting.CreateGeneratedClient<IGitHubApi>(GitHubMock.Origin, settings);
        Uri first = new($"{GitHubMock.Origin}/orgs/contoso/repos?per_page=3");
        NextLinkOriginPolicy origins = NextLinkOriginPolicy.Allow(first);
        PagedEnumerable<ApiResponse<List<GitHubRepo>>, GitHubRepo> byLink = PagedEnumerable.FromLinks(
            origins,
            (url, ct) => repos.ReadPageAsync(url ?? first, ct),
            static p => p.Content,
            static p => p.GetLink("next"));
        SampleCheck.Equal(true, origins.IsAllowed(first));
        SampleCheck.Equal(false, origins.IsAllowed(new Uri(GcsMock.Origin)));
        SampleCheck.Equal(GitHubMock.RepositoryCount, await PageCountObserver<GitHubRepo>.CountAsync(byLink.ToObservable(), token));
        await ReadPublicMirrorsAsync(settings, token);
    }

    /// <summary>Follows an anonymous directory from its index to a second host.</summary>
    /// <param name="settings">The serializer for both pages.</param>
    /// <param name="token">Cancels enumeration.</param>
    /// <returns>Completion after verifying the host of the second request.</returns>
    private static async Task ReadPublicMirrorsAsync(RefitSettings settings, CancellationToken token)
    {
        const string Mirror = "https://mirror.example/catalog";
        using StubHttp anonymous = new();
        anonymous.Add(
            Route.Get("/mirrors"),
            Reply.From(static _ => PagingReplies.Json<List<GitHubRepo>>(
                [new() { FullName = "public/first" }],
                PagingJsonContext.Default.ListGitHubRepo,
                ("Link", $"<{Mirror}>; rel=\"next\""))));
        anonymous.Add(Route.Get("/catalog"), Reply.With<List<GitHubRepo>>([new() { FullName = "public/second" }], PagingJsonContext.Default.ListGitHubRepo));
        IGitHubApi directory = anonymous.CreateGeneratedClient<IGitHubApi>("https://directory.example", settings);
        SampleCheck.Equal(2, await PageCountObserver<GitHubRepo>.CountAsync(directory.ListPublicMirrors().ToObservable(), token));
        SampleCheck.Equal(new Uri(Mirror), anonymous.Requests[1].RequestUri);
        SampleCheck.Equal(true, NextLinkOriginPolicy.Unrestricted.IsAllowed(new Uri(Mirror)));
        await anonymous.VerifyAllCalledAsync();
    }

    /// <summary>Resumes an issue search using the offset retained after the first page.</summary>
    /// <param name="issues">The service that supplies one page at a time.</param>
    /// <param name="token">Cancels page reads.</param>
    /// <returns>Completion after the remaining issues have been counted.</returns>
    private static async Task ResumeSearchAsync(IJiraApi issues, CancellationToken token)
    {
        JiraSearchResult first = await issues.ReadPageAsync(0, token);
        PageContinuation<int> bookmark = GeneratedPaging.NextOffset(0, first.Issues, first.Total);
        if (!bookmark.HasNext)
        {
            return;
        }

        PagedEnumerable<JiraSearchResult, JiraIssue> remaining = PagedEnumerable.Create(
            bookmark.Token,
            issues.ReadPageAsync,
            static page => page.Issues,
            static (page, start) => start + page.Issues.Count < page.Total
                ? GeneratedPaging.NextValue<int>(start + page.Issues.Count)
                : PageContinuation<int>.End);
        SampleCheck.Equal(first.Total - first.Issues.Count, await PageCountObserver<JiraIssue>.CountAsync(remaining.ToObservable(), token));
    }
}
