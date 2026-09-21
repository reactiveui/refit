// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Refit.Testing;

namespace Refit.Documentation.JsonContexts;

/// <summary>Shows what happens when a call uses a type the context does not describe.</summary>
internal static class ReflectionFallbackSample
{
    /// <summary>The price of the keyboard in the order the sample places.</summary>
    private const decimal KeyboardPrice = 49.5M;

    /// <summary>Sends a type without metadata, first with reflection off and then with the reflection fallback.</summary>
    /// <returns>A task that completes after both calls are checked.</returns>
    /// <exception cref="InvalidOperationException">The call unexpectedly succeeds without metadata.</exception>
    internal static async Task RunAsync()
    {
        using StubHttp http = OrdersServer.Create();
        using HttpClient client = OrdersServer.CreateClient(http);
        NewOrder newOrder = new("Ada", [new("KB-1", 1, KeyboardPrice)]);

        IOrdersApi api = RestService.ForGenerated<IOrdersApi>(client, OrderReadsJsonContext.Default);
        try
        {
            _ = await api.PlaceOrderAsync(newOrder, CancellationToken.None);
            throw new InvalidOperationException("NewOrder has no metadata, so the call should throw.");
        }
        catch (NotSupportedException error)
        {
            Console.WriteLine(error.Message); // JsonTypeInfo metadata for type '...NewOrder' was not provided by TypeInfoResolver of type '...OrderReadsJsonContext'. ...
            SampleCheck.Equal(true, error.Message.Contains(nameof(NewOrder), StringComparison.Ordinal));
        }

#if !NATIVE_AOT
        // Reflection-based JSON is not trim or Native AOT safe.
        IOrdersApi fallback = RestService.ForGenerated<IOrdersApi>(client, OrderReadsJsonContext.Default, allowReflectionFallback: true);
        Order placed = await fallback.PlaceOrderAsync(newOrder, CancellationToken.None);
        Console.WriteLine(placed.Id); // 5
        SampleCheck.Equal(OrdersServer.OrderId, placed.Id);
#endif
    }
}
