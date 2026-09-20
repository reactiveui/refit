// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Verifies the runtime helpers that source-generated <see cref="PagedAttribute"/> methods call.</summary>
public class GeneratedPagingTests
{
    /// <summary>The offset of the page under test.</summary>
    private const int Offset = 3;

    /// <summary>The number of items on a full page under test.</summary>
    private const int PageSize = 3;

    /// <summary>The total number of items the API reports.</summary>
    private const long Total = 7;

    /// <summary>The name of a header the tests read.</summary>
    private const string TokenHeader = "x-token";

    /// <summary>The name of a header that holds a whole link.</summary>
    private const string LinkValueHeader = "x-next";

    /// <summary>A link the tests read.</summary>
    private const string ExampleLink = "https://a.example/x";

    /// <summary>Verifies the next offset is the offset plus the page's items while the total has not been reached.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task NextOffset_BelowTheTotal_AdvancesByTheItemCount()
    {
        var next = GeneratedPaging.NextOffset(Offset, Items(PageSize), Total);

        using (Assert.Multiple())
        {
            await Assert.That(next.HasNext).IsTrue();
            await Assert.That(next.Token).IsEqualTo(Offset + PageSize);
        }
    }

    /// <summary>Verifies the sequence ends when the next offset reaches the total.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task NextOffset_ReachingTheTotal_Ends()
    {
        var next = GeneratedPaging.NextOffset(Offset + 1, Items(PageSize), Total);

        await Assert.That(next.HasNext).IsFalse();
    }

    /// <summary>Verifies the sequence ends when the total is unknown.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task NextOffset_UnknownTotal_Ends()
    {
        var next = GeneratedPaging.NextOffset(0, Items(1), null);

        await Assert.That(next.HasNext).IsFalse();
    }

    /// <summary>Verifies the sequence ends on an empty page, which would otherwise request the same offset again.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task NextOffset_EmptyPage_Ends()
    {
        using (Assert.Multiple())
        {
            await Assert.That(GeneratedPaging.NextOffset(0, Items(0), Total).HasNext).IsFalse();
            await Assert.That(GeneratedPaging.NextOffset<int>(0, null, Total).HasNext).IsFalse();
        }
    }

    /// <summary>Verifies items that are not a collection are counted by enumeration.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task NextOffset_LazySequence_IsCountedByEnumeration()
    {
        var next = GeneratedPaging.NextOffset(0, Enumerable.Range(1, PageSize).Select(static value => value), Total);

        await Assert.That(next.Token).IsEqualTo(PageSize);
    }

    /// <summary>Verifies a nullable value continues with its value and ends without one.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task NextValue_ContinuesOnlyWhenTheValueIsPresent()
    {
        var present = GeneratedPaging.NextValue<int>(PageSize);
        var absent = GeneratedPaging.NextValue<int>(null);

        using (Assert.Multiple())
        {
            await Assert.That(present.HasNext).IsTrue();
            await Assert.That(present.Token).IsEqualTo(PageSize);
            await Assert.That(absent.HasNext).IsFalse();
        }
    }

    /// <summary>Verifies the first value of a response header is read, and an absent header reads as <see langword="null"/>.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task HeaderValue_ReadsTheFirstValueOfTheHeader()
    {
        using var response = LinkHeaderParserTests.CreateResponse(TokenHeader, "first");
        _ = response.Headers!.TryAddWithoutValidation(TokenHeader, "second");

        using (Assert.Multiple())
        {
            await Assert.That(GeneratedPaging.HeaderValue(response, TokenHeader)).IsEqualTo("first");
            await Assert.That(GeneratedPaging.HeaderValue(response, "x-absent")).IsNull();
        }
    }

    /// <summary>Verifies a response without headers reads every header as <see langword="null"/>.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task HeaderValue_ResponseWithoutHeaders_ReturnsNull()
    {
        using HttpRequestMessage request = new(HttpMethod.Get, ExampleLink);
        using var response = new ApiResponse<string>(request, null, null, new());

        await Assert.That(GeneratedPaging.HeaderValue(response, TokenHeader)).IsNull();
    }

    /// <summary>Verifies the <c>Link</c> header is read for its next relation, and any other header as the whole link.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task NextLink_ReadsTheLinkHeaderOrTheNamedHeader()
    {
        using var linked = LinkHeaderParserTests.CreateResponse("Link", "<https://a.example/x?page=2>; rel=\"next\"");
        using var named = LinkHeaderParserTests.CreateResponse(LinkValueHeader, "https://a.example/y");
        using var none = LinkHeaderParserTests.CreateResponse("x-other", "value");

        using (Assert.Multiple())
        {
            await Assert.That(GeneratedPaging.NextLink(linked, "link")?.AbsoluteUri).IsEqualTo("https://a.example/x?page=2");
            await Assert.That(GeneratedPaging.NextLink(named, LinkValueHeader)?.AbsoluteUri).IsEqualTo("https://a.example/y");
            await Assert.That(GeneratedPaging.NextLink(none, LinkValueHeader)).IsNull();
        }
    }

    /// <summary>Verifies link text converts to a URI, and blank text ends the sequence.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ToLink_ConvertsTextAndTreatsBlankAsNoLink()
    {
        using (Assert.Multiple())
        {
            await Assert.That(GeneratedPaging.ToLink(ExampleLink)?.AbsoluteUri).IsEqualTo(ExampleLink);
            await Assert.That(GeneratedPaging.ToLink("/x?page=2")?.OriginalString).IsEqualTo("/x?page=2");
            await Assert.That(GeneratedPaging.ToLink(null)).IsNull();
            await Assert.That(GeneratedPaging.ToLink(string.Empty)).IsNull();
        }
    }

    /// <summary>Verifies text that is not a URI is reported instead of silently ending the listing.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ToLink_InvalidText_Throws() =>
        await Assert.That(static () => GeneratedPaging.ToLink("http://")).Throws<InvalidOperationException>();

    /// <summary>Verifies a same-origin policy allows the client's own origin and refuses another.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SameOrigin_AllowsOnlyTheOriginOfTheClientBaseAddress()
    {
        using var client = HttpClientTestFactory.Create(new Uri("https://a.example/v1/"));

        var policy = GeneratedPaging.SameOrigin(client);

        using (Assert.Multiple())
        {
            await Assert.That(policy.IsAllowed(new("https://a.example/v1/items?page=2"))).IsTrue();
            await Assert.That(policy.IsAllowed(new("https://b.example/v1/items"))).IsFalse();
        }
    }

    /// <summary>Verifies a client with no base address cannot define a same-origin policy.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SameOrigin_ClientWithoutBaseAddress_Throws()
    {
        using var client = HttpClientTestFactory.Create();

        using (Assert.Multiple())
        {
            await Assert.That(() => GeneratedPaging.SameOrigin(client)).Throws<InvalidOperationException>();
            await Assert.That(static () => GeneratedPaging.SameOrigin(null!)).Throws<ArgumentNullException>();
        }
    }

    /// <summary>Verifies the helpers reject a missing argument.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Helpers_NullArguments_Throw()
    {
        using var response = LinkHeaderParserTests.CreateResponse(TokenHeader, "value");

        using (Assert.Multiple())
        {
            await Assert.That(static () => GeneratedPaging.HeaderValue(null!, TokenHeader)).Throws<ArgumentNullException>();
            await Assert.That(() => GeneratedPaging.HeaderValue(response, null!)).Throws<ArgumentNullException>();
            await Assert.That(static () => GeneratedPaging.NextLink(null!, "Link")).Throws<ArgumentNullException>();
            await Assert.That(() => GeneratedPaging.NextLink(response, null!)).Throws<ArgumentNullException>();
        }
    }

    /// <summary>Verifies an empty string ends a cursor sequence, so a server that returns <c>""</c> for the last page does not loop.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task PageContinuation_To_EmptyStringEndsTheSequence()
    {
        var empty = PageContinuation.To(string.Empty);
        var value = PageContinuation.To("c1");

        using (Assert.Multiple())
        {
            await Assert.That(empty.HasNext).IsFalse();
            await Assert.That(value.HasNext).IsTrue();
        }
    }

    /// <summary>Verifies a cursor sequence stops at an empty cursor instead of requesting the first page again.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task FromCursor_EmptyCursor_EndsTheSequence()
    {
        var calls = 0;
        var items = PagedEnumerable.FromCursor(
            (_, _) =>
            {
                calls++;
                return Task.FromResult(new CursorPage { Items = [new() { Id = 1 }], NextCursor = string.Empty });
            },
            static page => page.Items,
            static page => page.NextCursor);

        List<PagedItem> collected = [];
        await foreach (var item in items)
        {
            collected.Add(item);
        }

        using (Assert.Multiple())
        {
            await Assert.That(collected.Count).IsEqualTo(1);
            await Assert.That(calls).IsEqualTo(1);
        }
    }

    /// <summary>Builds a list of consecutive items.</summary>
    /// <param name="count">The number of items.</param>
    /// <returns>The items.</returns>
    private static List<int> Items(int count) => [.. Enumerable.Range(1, count)];
}
