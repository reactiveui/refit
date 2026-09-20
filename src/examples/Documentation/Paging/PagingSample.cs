// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json;
using Refit.Testing;

namespace Refit.Documentation.Paging;

/// <summary>Pages generated clients against local stand-ins for common cloud listing APIs.</summary>
internal static class PagingSample
{
    /// <summary>The page size every listing scenario requests.</summary>
    private const int PageSize = 3;

    /// <summary>The number of pages seven items span in pages of three.</summary>
    private const int PagesForSevenItems = 3;

    /// <summary>The number of pages a bounded enumeration is allowed to fetch.</summary>
    private const int BoundedPages = 2;

    /// <summary>The bucket the S3 scenarios list.</summary>
    private const string Bucket = "photos";

    /// <summary>The prefix the S3 scenarios list.</summary>
    private const string Prefix = "2026/";

    /// <summary>The name of the query parameter S3 uses to continue a listing.</summary>
    private const string ContinuationParameter = "continuation-token=";

    /// <summary>The JSON options that use the generated metadata.</summary>
    private static readonly JsonSerializerOptions JsonOptions = new(PagingJsonContext.Default.Options) { TypeInfoResolver = PagingJsonContext.Default };

    /// <summary>Runs every scenario.</summary>
    /// <returns>Completion of all checked examples.</returns>
    internal static async Task RunAsync()
    {
        await CheckS3Async();
        await CheckAzureBlobAsync();
        await CheckCosmosAsync();
        await CheckGraphAsync();
        await CheckGitHubAsync();
        await CheckGcsAsync();
        await CheckJiraAsync();
        await CheckDynamoDbAsync();
        await CheckBehaviorAsync();
    }

    /// <summary>Checks an S3 listing that pages with a continuation token in an XML body.</summary>
    /// <returns>Completion of the check.</returns>
    private static async Task CheckS3Async()
    {
        using StubHttp http = new() { { Route.Fallback(), Reply.From(S3Mock.Serve) } };
        IS3Api api = http.CreateGeneratedClient<IS3Api>(S3Mock.Origin, new(new XmlContentSerializer()));

        List<string> keys = [];
        await foreach (S3Object item in api.ListObjects(Bucket, Prefix, PageSize))
        {
            keys.Add(item.Key);
        }

        Console.WriteLine(string.Join(", ", keys));
        SampleCheck.Equal(S3Mock.KeyCount, keys.Count);
        SampleCheck.Equal($"{Prefix}photo-001.jpg", keys[0]);
        SampleCheck.Equal(PagesForSevenItems, http.Requests.Count);
        SampleCheck.Equal("?list-type=2&prefix=2026%2F&max-keys=3", http.Requests[0].RequestUri!.Query);
        SampleCheck.Equal(true, http.Requests[1].RequestUri!.Query.Contains(ContinuationParameter, StringComparison.Ordinal));

        int pages = 0;
        await foreach (ListBucketResult page in api.ListObjects(Bucket, Prefix, PageSize).AsPages())
        {
            pages++;
            SampleCheck.Equal(page.KeyCount < PageSize, !page.IsTruncated);
        }

        SampleCheck.Equal(PagesForSevenItems, pages);
    }

    /// <summary>Checks an Azure Blob Storage listing whose last page carries an empty marker.</summary>
    /// <returns>Completion of the check.</returns>
    private static async Task CheckAzureBlobAsync()
    {
        using StubHttp http = new() { { Route.Fallback(), Reply.From(AzureBlobMock.Serve) } };
        IAzureBlobApi api = http.CreateGeneratedClient<IAzureBlobApi>(AzureBlobMock.Origin, new(new XmlContentSerializer()));

        List<string> names = [];
        await foreach (Blob blob in api.ListBlobs("logs", "app-", PageSize))
        {
            names.Add(blob.Name);
        }

        Console.WriteLine(string.Join(", ", names));
        SampleCheck.Equal(AzureBlobMock.BlobCount, names.Count);
        SampleCheck.Equal("app-blob-001.log", names[0]);
        SampleCheck.Equal(PagesForSevenItems, http.Requests.Count);
    }

    /// <summary>Checks an Azure Cosmos DB query that carries its continuation in a header both ways.</summary>
    /// <returns>Completion of the check.</returns>
    private static async Task CheckCosmosAsync()
    {
        using StubHttp http = new() { { Route.Fallback(), Reply.From(CosmosMock.Serve) } };
        ICosmosApi api = http.CreateGeneratedClient<ICosmosApi>(CosmosMock.Origin, CreateJsonSettings());

        List<string> ids = [];
        await foreach (CosmosDocument document in api.Query("shop", "orders", new() { Query = "SELECT * FROM c" }, PageSize))
        {
            ids.Add(document.Id);
        }

        Console.WriteLine(string.Join(", ", ids));
        SampleCheck.Equal(CosmosMock.DocumentCount, ids.Count);
        SampleCheck.Equal(false, http.Requests[0].Headers.Contains(CosmosMock.ContinuationHeader));
        SampleCheck.Equal(true, http.Requests[1].Headers.Contains(CosmosMock.ContinuationHeader));
        SampleCheck.Equal(PagesForSevenItems, http.Requests.Count);
    }

    /// <summary>Checks Microsoft Graph, which names each next page with an absolute link the client restricts to its own origin.</summary>
    /// <returns>Completion of the check.</returns>
    private static async Task CheckGraphAsync()
    {
        using StubHttp http = new() { { Route.Fallback(), Reply.From(GraphMock.Serve) } };
        IGraphApi api = http.CreateGeneratedClient<IGraphApi>(GraphMock.Origin, CreateJsonSettings());

        List<string> names = [];
        await foreach (GraphUser user in api.ListUsers(PageSize))
        {
            names.Add(user.DisplayName);
        }

        Console.WriteLine(string.Join(", ", names));
        SampleCheck.Equal(GraphMock.UserCount, names.Count);
        SampleCheck.Equal(PagesForSevenItems, http.Requests.Count);

        // A link to another origin is refused without being requested, so the client's credentials are never forwarded to it.
        using StubHttp hostile = new()
        {
            { Route.Fallback(), Reply.From(static _ => PagingReplies.Json(new GraphUserPage { NextLink = "https://evil.example/v1.0/users?$skiptoken=3" }, PagingJsonContext.Default.GraphUserPage)) },
        };
        IGraphApi guarded = hostile.CreateGeneratedClient<IGraphApi>(GraphMock.Origin, CreateJsonSettings());
        bool refused = false;
        try
        {
            await foreach (GraphUser user in guarded.ListUsers(PageSize))
            {
                Console.WriteLine(user.Id);
            }
        }
        catch (InvalidOperationException)
        {
            refused = true;
        }

        SampleCheck.Equal(true, refused);
        SampleCheck.Equal(1, hostile.Requests.Count);
    }

    /// <summary>Checks GitHub, which links pages with a <c>Link</c> header on a bare JSON array.</summary>
    /// <returns>Completion of the check.</returns>
    private static async Task CheckGitHubAsync()
    {
        using StubHttp http = new() { { Route.Fallback(), Reply.From(GitHubMock.Serve) } };
        IGitHubApi api = http.CreateGeneratedClient<IGitHubApi>(GitHubMock.Origin, CreateJsonSettings());

        List<string> names = [];
        await foreach (GitHubRepo repository in api.ListRepositories("contoso", PageSize))
        {
            names.Add(repository.FullName);
        }

        Console.WriteLine(string.Join(", ", names));
        SampleCheck.Equal(GitHubMock.RepositoryCount, names.Count);
        SampleCheck.Equal(PagesForSevenItems, http.Requests.Count);
    }

    /// <summary>Checks Google Cloud Storage, which continues with a page token in a JSON body.</summary>
    /// <returns>Completion of the check.</returns>
    private static async Task CheckGcsAsync()
    {
        using StubHttp http = new() { { Route.Fallback(), Reply.From(GcsMock.Serve) } };
        IGcsApi api = http.CreateGeneratedClient<IGcsApi>(GcsMock.Origin, CreateJsonSettings());

        List<string> names = [];
        await foreach (GcsObject item in api.ListObjects("reports", "2026/", PageSize))
        {
            names.Add(item.Name);
        }

        Console.WriteLine(string.Join(", ", names));
        SampleCheck.Equal(GcsMock.ObjectCount, names.Count);
        SampleCheck.Equal(PagesForSevenItems, http.Requests.Count);
    }

    /// <summary>Checks Jira, which pages by offset and reports the total number of matches.</summary>
    /// <returns>Completion of the check.</returns>
    private static async Task CheckJiraAsync()
    {
        using StubHttp http = new() { { Route.Fallback(), Reply.From(JiraMock.Serve) } };
        IJiraApi api = http.CreateGeneratedClient<IJiraApi>(JiraMock.Origin, CreateJsonSettings());

        List<string> keys = [];
        await foreach (JiraIssue issue in api.Search("project = OPS", PageSize))
        {
            keys.Add(issue.Key);
        }

        Console.WriteLine(string.Join(", ", keys));
        SampleCheck.Equal(JiraMock.IssueCount, keys.Count);
        SampleCheck.Equal(PagesForSevenItems, http.Requests.Count);
        SampleCheck.Equal("?jql=project%20%3D%20OPS&maxResults=3&startAt=0", http.Requests[0].RequestUri!.Query);
    }

    /// <summary>Checks DynamoDB, whose continuation lives inside the request body, so the ordinary call is wrapped by hand.</summary>
    /// <returns>Completion of the check.</returns>
    private static async Task CheckDynamoDbAsync()
    {
        using StubHttp http = new() { { Route.Fallback(), Reply.From(DynamoDbMock.ServeAsync) } };
        IDynamoDbApi api = http.CreateGeneratedClient<IDynamoDbApi>(DynamoDbMock.Origin, CreateJsonSettings());

        PagedEnumerable<DynamoScanResponse, Dictionary<string, DynamoAttributeValue>> orders = PagedEnumerable.Create(
            (Dictionary<string, DynamoAttributeValue>? startKey, CancellationToken cancellationToken) =>
                api.Scan(new() { TableName = "Orders", Limit = PageSize, ExclusiveStartKey = startKey }, cancellationToken),
            static page => page.Items,
            static (page, _) => PageContinuation.To(page.LastEvaluatedKey));

        int count = 0;
        await foreach (Dictionary<string, DynamoAttributeValue> item in orders)
        {
            count++;
            Console.WriteLine(item["OrderId"].S);
        }

        SampleCheck.Equal(DynamoDbMock.ItemCount, count);
        SampleCheck.Equal(PagesForSevenItems, http.Requests.Count);
    }

    /// <summary>Checks laziness, early termination, a page limit and cancellation on a generated listing.</summary>
    /// <returns>Completion of the check.</returns>
    private static async Task CheckBehaviorAsync()
    {
        using StubHttp http = new() { { Route.Fallback(), Reply.From(S3Mock.Serve) } };
        IS3Api api = http.CreateGeneratedClient<IS3Api>(S3Mock.Origin, new(new XmlContentSerializer()));

        PagedEnumerable<ListBucketResult, S3Object> listing = api.ListObjects(Bucket, null, PageSize);
        SampleCheck.Equal(0, http.Requests.Count);

        await using (IAsyncEnumerator<S3Object> first = listing.GetAsyncEnumerator())
        {
            SampleCheck.Equal(true, await first.MoveNextAsync());
        }

        SampleCheck.Equal(1, http.Requests.Count);

        int bounded = 0;
        await foreach (S3Object item in listing.WithMaxPages(BoundedPages))
        {
            bounded++;
            Console.WriteLine(item.Key);
        }

        SampleCheck.Equal(BoundedPages * PageSize, bounded);
        SampleCheck.Equal(1 + BoundedPages, http.Requests.Count);

        using CancellationTokenSource canceled = new();
        await canceled.CancelAsync();
        int before = http.Requests.Count;
        bool cancellationObserved = false;
        try
        {
            await foreach (S3Object item in listing.WithCancellation(canceled.Token))
            {
                Console.WriteLine(item.Key);
            }
        }
        catch (OperationCanceledException)
        {
            cancellationObserved = true;
        }

        SampleCheck.Equal(true, cancellationObserved);
        SampleCheck.Equal(before, http.Requests.Count);
    }

    /// <summary>Creates settings whose serializer uses the generated JSON metadata.</summary>
    /// <returns>New settings for a client.</returns>
    private static RefitSettings CreateJsonSettings() =>
        new(new SystemTextJsonContentSerializer(JsonOptions));
}
