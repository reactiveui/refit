// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json;
using Refit.Testing;

namespace Refit.Documentation.JsonContexts;

/// <summary>Builds the JSON options, serializer and settings by hand, and gives the context to the options as their metadata source.</summary>
internal static class OwnOptionsSample
{
    /// <summary>The price of the keyboard the sample orders.</summary>
    private const decimal KeyboardPrice = 49.5M;

    /// <summary>The options the app shares: snake_case names, prices written as text, and the orders context as the metadata source.</summary>
    private static readonly JsonSerializerOptions AppOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new PriceJsonConverter() },
        TypeInfoResolver = OrdersJsonContext.Default,
    };

    /// <summary>Creates a client from settings that wrap the app's own options.</summary>
    /// <returns>A task that completes after the request and reply are checked.</returns>
    internal static async Task RunAsync()
    {
        using StubHttp http = OrdersServer.CreateSnakeCase();
        using HttpClient client = OrdersServer.CreateClient(http);

        SystemTextJsonContentSerializer serializer = new(AppOptions);
        RefitSettings settings = new(serializer);

        IOrdersApi api = RestService.ForGenerated<IOrdersApi>(client, settings);

        Order order = await api.GetOrderAsync(OrdersServer.OrderId, CancellationToken.None);
        Console.WriteLine(order.Lines[0].UnitPrice); // 49.50
        SampleCheck.Equal(KeyboardPrice, order.Lines[0].UnitPrice);

        // The route only matches a body with snake_case names and a text price.
        Order placed = await api.PlaceOrderAsync(new("Ada", [new("KB-1", 1, KeyboardPrice)]), CancellationToken.None);
        SampleCheck.Equal(OrdersServer.OrderId, placed.Id);
    }
}
