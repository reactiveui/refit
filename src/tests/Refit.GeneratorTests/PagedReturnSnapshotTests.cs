// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.GeneratorTests;

/// <summary>Snapshot tests for the source emitted for methods that return <c>PagedEnumerable</c>.</summary>
public class PagedReturnSnapshotTests
{
    /// <summary>Verifies a cursor read from the body and sent as a query parameter.</summary>
    /// <returns>A task representing the asynchronous verification.</returns>
    [Test]
    public Task CursorFromBody() =>
        Fixture.VerifyForBody(
            """
            public sealed class ObjectPage
            {
                public List<StoredObject>? Contents { get; set; }

                public string? NextToken { get; set; }
            }

            public sealed class StoredObject
            {
                public string? Key { get; set; }
            }

            [Get("/{bucket}")]
            [Paged(Next = nameof(ObjectPage.NextToken))]
            PagedEnumerable<ObjectPage, StoredObject> List(string bucket, string? prefix, [PageToken, AliasAs("continuation-token")] string? token = null);
            """);

    /// <summary>Verifies an offset that ends when it reaches the total the page reports.</summary>
    /// <returns>A task representing the asynchronous verification.</returns>
    [Test]
    public Task OffsetFromTotal() =>
        Fixture.VerifyForBody(
            """
            public sealed class IssuePage
            {
                public List<Issue>? Issues { get; set; }

                public int Total { get; set; }
            }

            public sealed class Issue
            {
                public string? Key { get; set; }
            }

            [Get("/search")]
            [Paged(Total = nameof(IssuePage.Total))]
            PagedEnumerable<IssuePage, Issue> Search(string jql, [PageToken, AliasAs("startAt")] int startAt = 0);
            """);

    /// <summary>Verifies a continuation read from a response header and sent as a request header.</summary>
    /// <returns>A task representing the asynchronous verification.</returns>
    [Test]
    public Task CursorFromHeader() =>
        Fixture.VerifyForBody(
            """
            public sealed class Feed
            {
                public List<Document>? Documents { get; set; }
            }

            public sealed class Document
            {
                public string? Id { get; set; }
            }

            [Get("/docs")]
            [Paged(Items = "Content.Documents", NextHeader = "x-ms-continuation")]
            PagedEnumerable<ApiResponse<Feed>, Document> List([PageToken, Header("x-ms-continuation")] string? continuation = null);
            """);

    /// <summary>Verifies links read from the Link header and restricted to one origin.</summary>
    /// <returns>A task representing the asynchronous verification.</returns>
    [Test]
    public Task LinksFromLinkHeader() =>
        Fixture.VerifyForBody(
            """
            public sealed class Repo
            {
                public string? Name { get; set; }
            }

            [Get("/orgs/{org}/repos")]
            [Paged(NextHeader = "Link", Origins = new[] { "https://api.github.com" })]
            PagedEnumerable<ApiResponse<List<Repo>>, Repo> Repos(string org);
            """);

    /// <summary>Verifies links read from the body and restricted to the client's origin.</summary>
    /// <returns>A task representing the asynchronous verification.</returns>
    [Test]
    public Task LinksFromBody() =>
        Fixture.VerifyForBody(
            """
            public sealed class UserPage
            {
                public List<User>? Value { get; set; }

                public Uri? NextLink { get; set; }
            }

            public sealed class User
            {
                public string? Id { get; set; }
            }

            [Get("/users")]
            [Paged(Next = nameof(UserPage.NextLink), SameOrigin = true)]
            PagedEnumerable<UserPage, User> Users(int top);
            """);
}
