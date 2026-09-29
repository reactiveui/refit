// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace Refit.Benchmarks;

/// <summary>
/// Compares a generated Refit client created with a JSON context against a hand-written <see cref="HttpClient"/>
/// client that uses the same metadata. Both send through the same in-memory handler, so the difference is the
/// client code: request building, serialization and reply handling.
/// </summary>
[System.Diagnostics.DebuggerDisplay("{ItemCount}")]
[MemoryDiagnoser]
[CategoriesColumn]
[GroupBenchmarksBy(BenchmarkDotNet.Configs.BenchmarkLogicalGroupRule.ByCategory)]
public class HandWrittenClientComparisonBenchmark
{
    /// <summary>The base address both clients use.</summary>
    private const string BaseAddress = "https://items.example";

    /// <summary>The number of items in the list reply.</summary>
    private const int ListItemCount = 25;

    /// <summary>The path of the item list.</summary>
    private const string ItemsPath = "/items";

    /// <summary>The identifier of the item the get benchmarks read and the create benchmarks send.</summary>
    private const int ItemId = 7;

    /// <summary>The item the create benchmarks send.</summary>
    private static readonly FastItem NewItem = new() { Id = ItemId, Name = "created" };

    /// <summary>The relative address of the item list.</summary>
    private static readonly Uri ItemsUri = new(ItemsPath, UriKind.Relative);

    /// <summary>The relative address of the single item.</summary>
    private static readonly Uri ItemUri = new($"{ItemsPath}/{ItemId}", UriKind.Relative);

    /// <summary>The client shared by both implementations.</summary>
    private HttpClient _client = null!;

    /// <summary>The generated Refit client.</summary>
    private IItemsService _refit = null!;

    /// <summary>Gets the number of items in the list reply.</summary>
    public static int ItemCount => ListItemCount;

    /// <summary>Creates the handler and both clients.</summary>
    [GlobalSetup]
    public void Setup()
    {
        StringBuilder list = new("[");
        for (var i = 0; i < ListItemCount; i++)
        {
            _ = list.Append(i == 0 ? string.Empty : ",")
                .Append(CultureInfo.InvariantCulture, $$"""{"id":{{i}},"name":"item {{i}}"}""");
        }

        _client = new(new ItemsHandler(list.Append(']').ToString())) { BaseAddress = new(BaseAddress) };
        _refit = RestService.ForGenerated<IItemsService>(_client, HandWrittenComparisonJsonContext.Default);
    }

    /// <summary>Disposes the shared client.</summary>
    [GlobalCleanup]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Cleanup() => _client.Dispose();

    /// <summary>Reads one item with the hand-written client.</summary>
    /// <returns>The item.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Get")]
    public Task<FastItem?> HandWrittenGetAsync() =>
        _client.GetFromJsonAsync(ItemUri, HandWrittenComparisonJsonContext.Default.FastItem);

    /// <summary>Reads one item with the generated Refit client.</summary>
    /// <returns>The item.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Benchmark]
    [BenchmarkCategory("Get")]
    public Task<FastItem> RefitGetAsync() => _refit.GetAsync(ItemId, CancellationToken.None);

    /// <summary>Reads the item list with the hand-written client.</summary>
    /// <returns>The items.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("List")]
    public Task<List<FastItem>?> HandWrittenListAsync() =>
        _client.GetFromJsonAsync(ItemsUri, HandWrittenComparisonJsonContext.Default.ListFastItem);

    /// <summary>Reads the item list with the generated Refit client.</summary>
    /// <returns>The items.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Benchmark]
    [BenchmarkCategory("List")]
    public Task<List<FastItem>> RefitListAsync() => _refit.ListAsync(CancellationToken.None);

    /// <summary>Creates an item with the hand-written client.</summary>
    /// <returns>The created item.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Create")]
    public async Task<FastItem?> HandWrittenCreateAsync()
    {
        using var response = await _client
            .PostAsJsonAsync(ItemsUri, NewItem, HandWrittenComparisonJsonContext.Default.FastItem)
            .ConfigureAwait(false);
        _ = response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync(HandWrittenComparisonJsonContext.Default.FastItem).ConfigureAwait(false);
    }

    /// <summary>Creates an item with the generated Refit client.</summary>
    /// <returns>The created item.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Benchmark]
    [BenchmarkCategory("Create")]
    public Task<FastItem> RefitCreateAsync() => _refit.CreateAsync(NewItem, CancellationToken.None);

    /// <summary>Answers the item routes from memory: the list for <c>GET /items</c>, one item otherwise.</summary>
    /// <param name="list">The JSON list reply.</param>
    private sealed class ItemsHandler(string list) : HttpMessageHandler
    {
        /// <summary>The JSON reply for a single item.</summary>
        private const string Item = """{"id":7,"name":"item 7"}""";

        /// <inheritdoc/>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Method == HttpMethod.Get && request.RequestUri!.AbsolutePath == ItemsPath ? list : Item;
            var status = request.Method == HttpMethod.Post ? HttpStatusCode.Created : HttpStatusCode.OK;
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            return Task.FromResult(new HttpResponseMessage(status) { RequestMessage = request, Content = content });
        }
    }
}
