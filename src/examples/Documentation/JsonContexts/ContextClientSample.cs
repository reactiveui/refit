// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Refit.Testing;

namespace Refit.Documentation.JsonContexts;

/// <summary>Creates a generated client straight from a JSON context and calls every kind of member of the orders API.</summary>
internal static class ContextClientSample
{
    /// <summary>The number of lines on the order the shop returns.</summary>
    private const int LineCount = 2;

    /// <summary>The price of the keyboard on the order.</summary>
    private const decimal KeyboardPrice = 49.5M;

    /// <summary>Reads an order, a list, a polymorphic reply and a rejected order.</summary>
    /// <returns>A task that completes after every assertion.</returns>
    internal static async Task RunAsync()
    {
        using StubHttp http = OrdersServer.Create();
        using HttpClient client = OrdersServer.CreateClient(http);

        IOrdersApi api = RestService.ForGenerated<IOrdersApi>(client, OrdersJsonContext.Default);

        Order order = await api.GetOrderAsync(OrdersServer.OrderId, CancellationToken.None);
        Console.WriteLine($"{order.Customer} {order.Status} {order.Lines[0].UnitPrice}"); // Ada Shipped 49.5
        SampleCheck.Equal("Ada", order.Customer);
        SampleCheck.Equal(OrderStatus.Shipped, order.Status);
        SampleCheck.Equal(LineCount, order.Lines.Count);
        SampleCheck.Equal(KeyboardPrice, order.Lines[0].UnitPrice);

        List<Order> orders = await api.ListOrdersAsync(CancellationToken.None);
        SampleCheck.Equal(1, orders.Count);

        Shipment shipment = await api.GetShipmentAsync(OrdersServer.OrderId, CancellationToken.None);
        Console.WriteLine(shipment.GetType().Name); // CourierShipment
        SampleCheck.Equal(new CourierShipment("S-77", "TN-5501"), shipment);

        Order placed = await api.PlaceOrderAsync(new("Ada", [new("KB-1", 1, KeyboardPrice)]), CancellationToken.None);
        SampleCheck.Equal(OrdersServer.OrderId, placed.Id);

        await ShowRejectedOrderAsync(api);
    }

    /// <summary>Reads the problem document of a 400 reply. Refit parses it itself, so the context needs no entry for it.</summary>
    /// <param name="api">The client that sends the order.</param>
    /// <returns>A task that completes after the problem document is checked.</returns>
    /// <exception cref="InvalidOperationException">The shop unexpectedly accepts the order.</exception>
    private static async Task ShowRejectedOrderAsync(IOrdersApi api)
    {
        try
        {
            _ = await api.PlaceOrderAsync(new("Ada", []), CancellationToken.None);
            throw new InvalidOperationException("The shop should reject an order with no lines.");
        }
        catch (ApiException error)
        {
            ProblemDetails? problem = ValidationApiException.Create(error).Content;
            Console.WriteLine(problem?.Errors["lines"][0]); // An order needs at least one line.
            SampleCheck.Equal("An order needs at least one line.", problem?.Errors["lines"][0]);
        }
    }
}
