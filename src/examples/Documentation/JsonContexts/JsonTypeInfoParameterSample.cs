// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Refit.Testing;

namespace Refit.Documentation.JsonContexts;

/// <summary>Passes JSON metadata to interface methods, so each call carries the metadata it needs.</summary>
internal static class JsonTypeInfoParameterSample
{
    /// <summary>The number of lines on the order the shop returns.</summary>
    private const int LineCount = 2;

    /// <summary>The price of the keyboard on the order.</summary>
    private const decimal KeyboardPrice = 49.5M;

    /// <summary>Runs every scenario.</summary>
    /// <returns>A task that completes after every assertion.</returns>
    internal static async Task RunAsync()
    {
        await PassMetadataToEachCallAsync();
        await SkipMetadataAsync();
        await PreferMetadataOverSettingsAsync();
    }

    /// <summary>Calls a client that has no JSON context. Every call brings its own metadata.</summary>
    /// <returns>A task that completes after every reply and request body is checked.</returns>
    private static async Task PassMetadataToEachCallAsync()
    {
        using StubHttp http = OrdersServer.Create();
        using HttpClient client = OrdersServer.CreateClient(http);

        IOrdersTypeInfoApi api = RestService.ForGenerated<IOrdersTypeInfoApi>(client);

        Order order = await api.GetOrderAsync(OrdersServer.OrderId, OrdersJsonContext.Default.Order, CancellationToken.None);
        Console.WriteLine($"{order.Customer} {order.Lines[0].UnitPrice}"); // Ada 49.5
        SampleCheck.Equal(LineCount, order.Lines.Count);
        SampleCheck.Equal(KeyboardPrice, order.Lines[0].UnitPrice);

        using ApiResponse<Order> response = await api.GetOrderResponseAsync(OrdersServer.OrderId, OrdersJsonContext.Default.Order, CancellationToken.None);
        Console.WriteLine($"{response.StatusCode} {response.Content?.Customer}"); // OK Ada
        SampleCheck.Equal("Ada", response.Content?.Customer);

        List<Order> orders = await api.ListOrdersAsync(OrdersJsonContext.Default.ListOrder, CancellationToken.None);
        SampleCheck.Equal(1, orders.Count);

        await foreach (Order streamed in api.StreamOrdersAsync(OrdersJsonContext.Default.Order, CancellationToken.None))
        {
            Console.WriteLine(streamed.Customer); // Ada
            SampleCheck.Equal(OrdersServer.OrderId, streamed.Id);
        }

        NewOrder newOrder = new("Ada", [new("KB-1", 1, KeyboardPrice)]);
        Order placed = await api.PlaceOrderAsync(newOrder, OrdersJsonContext.Default.NewOrder, OrdersJsonContext.Default.Order, CancellationToken.None);
        SampleCheck.Equal(OrdersServer.OrderId, placed.Id);
    }

    /// <summary>Leaves an optional metadata parameter empty, so the serializer looks the metadata up.</summary>
    /// <returns>A task that completes after both calls are checked.</returns>
    private static async Task SkipMetadataAsync()
    {
        using StubHttp http = OrdersServer.Create();
        using HttpClient client = OrdersServer.CreateClient(http);

        IOrdersTypeInfoApi api = RestService.ForGenerated<IOrdersTypeInfoApi>(client, OrdersJsonContext.Default);

        Order looked = await api.FindOrderAsync(OrdersServer.OrderId);
        Order passed = await api.FindOrderAsync(OrdersServer.OrderId, OrdersJsonContext.Default.Order);
        Console.WriteLine($"{looked.Customer} {passed.Customer}"); // Ada Ada
        SampleCheck.Equal("Ada", looked.Customer);
        SampleCheck.Equal("Ada", passed.Customer);
    }

    /// <summary>Reads a camelCase reply through snake_case settings. The metadata's own options apply to the call.</summary>
    /// <returns>A task that completes after the reply is checked.</returns>
    private static async Task PreferMetadataOverSettingsAsync()
    {
        using StubHttp http = OrdersServer.Create();
        using HttpClient client = OrdersServer.CreateClient(http);

        IOrdersTypeInfoApi api = RestService.ForGenerated<IOrdersTypeInfoApi>(client, RefitSettings.SnakeCase());

        Order order = await api.GetOrderAsync(OrdersServer.OrderId, OrdersJsonContext.Default.Order, CancellationToken.None);
        Console.WriteLine(order.Lines[0].UnitPrice); // 49.5
        SampleCheck.Equal(KeyboardPrice, order.Lines[0].UnitPrice);
    }
}
