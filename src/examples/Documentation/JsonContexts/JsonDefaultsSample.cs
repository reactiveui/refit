// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Refit.Testing;

namespace Refit.Documentation.JsonContexts;

/// <summary>Reads the same camelCase reply through a context without options and through a context with the web defaults.</summary>
internal static class JsonDefaultsSample
{
    /// <summary>Shows the empty object a context without options produces, then the fix.</summary>
    /// <returns>A task that completes after both reads are checked.</returns>
    internal static async Task RunAsync()
    {
        using StubHttp http = OrdersServer.Create();
        using HttpClient client = OrdersServer.CreateClient(http);

        IOrdersApi unconfigured = RestService.ForGenerated<IOrdersApi>(client, UnconfiguredOrdersJsonContext.Default);
        Order empty = await unconfigured.GetOrderAsync(OrdersServer.OrderId, CancellationToken.None);
        Console.WriteLine($"{empty.Id} {empty.Customer is null}"); // 0 True
        SampleCheck.Equal(0, empty.Id);
        SampleCheck.Equal(null, empty.Customer);

        IOrdersApi web = RestService.ForGenerated<IOrdersApi>(client, OrdersJsonContext.Default);
        Order order = await web.GetOrderAsync(OrdersServer.OrderId, CancellationToken.None);
        Console.WriteLine($"{order.Id} {order.Customer}"); // 5 Ada
        SampleCheck.Equal(OrdersServer.OrderId, order.Id);
        SampleCheck.Equal("Ada", order.Customer);
    }
}
