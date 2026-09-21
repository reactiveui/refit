// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation.JsonContexts;

/// <summary>Calls the serializer directly with metadata passed to each call instead of looked up from the options.</summary>
internal static class JsonTypeInfoSerializerSample
{
    /// <summary>The identifier of the order the sample writes.</summary>
    private const int OrderId = 5;

    /// <summary>The price of the keyboard on the order.</summary>
    private const decimal KeyboardPrice = 49.5M;

    /// <summary>The number of orders in the JSON Lines body.</summary>
    private const int StreamedOrders = 2;

    /// <summary>Runs every overload of the metadata capability.</summary>
    /// <returns>A task that completes after every assertion.</returns>
    /// <exception cref="InvalidOperationException">The serializer does not offer the metadata capability.</exception>
    internal static async Task RunAsync()
    {
        RefitSettings settings = RefitSettings.ForJsonContext(OrdersJsonContext.Default);
        if (settings.ContentSerializer is not IJsonTypeInfoContentSerializer serializer)
        {
            throw new InvalidOperationException("The System.Text.Json serializer offers the metadata capability.");
        }

        Order order = new(OrderId, "Ada", OrderStatus.Shipped, [new("KB-1", 1, KeyboardPrice)]);

        using HttpContent content = serializer.ToHttpContent(order, OrdersJsonContext.Default.Order);
        string json = await content.ReadAsStringAsync();
        Console.WriteLine(json);
        SampleCheck.Equal("""{"id":5,"customer":"Ada","status":"Shipped","lines":[{"sku":"KB-1","quantity":1,"unitPrice":49.5}]}""", json);

        Order? read = await serializer.FromHttpContentAsync(content, OrdersJsonContext.Default.Order);
        Order? fromText = serializer.DeserializeFromString(json, OrdersJsonContext.Default.Order);
        Console.WriteLine(read?.Customer); // Ada
        SampleCheck.Equal("Ada", read?.Customer);
        SampleCheck.Equal("Ada", fromText?.Customer);

        using HttpContent buffered = serializer.ToHttpContentSynchronous(order, OrdersJsonContext.Default.Order);
        using HttpContent streamed = serializer.ToStreamingHttpContent(order, OrdersJsonContext.Default.Order);
        SampleCheck.Equal(json, await buffered.ReadAsStringAsync());
        SampleCheck.Equal(json, await streamed.ReadAsStringAsync());

        await ReadStreamAsync(serializer);
    }

    /// <summary>Reads one order at a time from a JSON Lines body.</summary>
    /// <param name="serializer">The serializer that offers the metadata capability.</param>
    /// <returns>A task that completes after every order is read.</returns>
    private static async Task ReadStreamAsync(IJsonTypeInfoContentSerializer serializer)
    {
        const string body = $"{OrdersServer.OrderJson}\n{OrdersServer.OrderJson}";
        using StringContent content = new(body);
        await using Stream stream = await content.ReadAsStreamAsync();

        int count = 0;
        await foreach (Order? item in serializer.DeserializeStreamAsync(stream, StreamingContentFormat.JsonLines, OrdersJsonContext.Default.Order))
        {
            count++;
            Console.WriteLine(item?.Customer); // Ada
        }

        SampleCheck.Equal(StreamedOrders, count);
    }
}
