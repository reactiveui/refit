// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json;
#if !NATIVE_AOT
using System.Text.Json.Serialization.Metadata;
#endif
using Refit.Testing;

namespace Refit.Documentation.JsonContexts;

/// <summary>Adds a JSON context to serializer options the app already has.</summary>
internal static class ExistingSettingsSample
{
    /// <summary>The number of lines on the order the service returns.</summary>
    private const int LineCount = 2;

    /// <summary>The price of the keyboard the sample orders.</summary>
    private const decimal KeyboardPrice = 49.5M;

#if !NATIVE_AOT
    /// <summary>The value of the <c>kind</c> property that selects a pickup shipment.</summary>
    private const string PickupKind = "pickup";
#endif

    /// <summary>The options the app already uses: snake_case names and prices written as text.</summary>
    private static readonly JsonSerializerOptions AppOptions = new(JsonSerializerDefaults.Web) { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower, Converters = { new PriceJsonConverter() } };

#if !NATIVE_AOT
    /// <summary>Options whose reflection resolver registers a derived type in code.</summary>
    private static readonly JsonSerializerOptions ModifiedOptions = new(JsonSerializerDefaults.Web) { TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { RegisterPickupShipments } } };
#endif

    /// <summary>Runs every scenario.</summary>
    /// <returns>A task that completes after every assertion.</returns>
    internal static async Task RunAsync()
    {
        await KeepNamingAndConvertersAsync();
        await UseJsonContextAsync();
#if !NATIVE_AOT
        await KeepContractModifiersAsync();
#endif
    }

    /// <summary>Passes the settings and the context to one call. The settings' naming policy and converter apply.</summary>
    /// <returns>A task that completes after the request and reply are checked.</returns>
    private static async Task KeepNamingAndConvertersAsync()
    {
        using StubHttp http = OrdersServer.CreateSnakeCase();
        using HttpClient client = OrdersServer.CreateClient(http);
        RefitSettings settings = new(new SystemTextJsonContentSerializer(AppOptions));

        IOrdersApi api = RestService.ForGenerated<IOrdersApi>(client, OrdersJsonContext.Default, settings);

        Order order = await api.GetOrderAsync(OrdersServer.OrderId, CancellationToken.None);
        Console.WriteLine(order.Lines[0].UnitPrice); // 49.50
        SampleCheck.Equal(LineCount, order.Lines.Count);
        SampleCheck.Equal(KeyboardPrice, order.Lines[0].UnitPrice);

        // The route only matches a body with snake_case names and a text price.
        Order placed = await api.PlaceOrderAsync(new("Ada", [new("KB-1", 1, KeyboardPrice)]), CancellationToken.None);
        SampleCheck.Equal(OrdersServer.OrderId, placed.Id);
    }

    /// <summary>Registers the context on the settings, so the same settings also work with the other creation methods.</summary>
    /// <returns>A task that completes after the reply and the registration are checked.</returns>
    private static async Task UseJsonContextAsync()
    {
        using StubHttp http = OrdersServer.CreateSnakeCase();
        using HttpClient client = OrdersServer.CreateClient(http);
        RefitSettings settings = new(new SystemTextJsonContentSerializer(AppOptions));

        RefitSettings registered = settings.UseJsonContext(OrdersJsonContext.Default);
        SampleCheck.Equal(true, ReferenceEquals(settings, registered));

        IOrdersApi api = RestService.ForGenerated<IOrdersApi>(client, settings);
        Order order = await api.GetOrderAsync(OrdersServer.OrderId, CancellationToken.None);
        SampleCheck.Equal(KeyboardPrice, order.Lines[0].UnitPrice);

        // Registering the same context again keeps the serializer.
        IHttpContentSerializer serializer = settings.ContentSerializer;
        _ = settings.UseJsonContext(OrdersJsonContext.Default);
        SampleCheck.Equal(true, ReferenceEquals(serializer, settings.ContentSerializer));
    }

#if !NATIVE_AOT
    /// <summary>Keeps a derived type that an existing reflection resolver registers in code.</summary>
    /// <returns>A task that completes after the reply is read as the derived type.</returns>
    private static async Task KeepContractModifiersAsync()
    {
        using StubHttp http = OrdersServer.CreatePickupShipment();
        using HttpClient client = OrdersServer.CreateClient(http);
        RefitSettings settings = new(new SystemTextJsonContentSerializer(ModifiedOptions));

        IOrdersApi api = RestService.ForGenerated<IOrdersApi>(client, OrdersJsonContext.Default, settings);

        Shipment shipment = await api.GetShipmentAsync(OrdersServer.OrderId, CancellationToken.None);
        Console.WriteLine(shipment.GetType().Name); // PickupShipment
        SampleCheck.Equal(new PickupShipment("S-78", "Harbour Street"), shipment);
    }

    /// <summary>Adds the pickup kind to the derived types of <c>Shipment</c>.</summary>
    /// <param name="typeInfo">The metadata System.Text.Json is about to use.</param>
    private static void RegisterPickupShipments(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Type == typeof(Shipment) && typeInfo.PolymorphismOptions is { } polymorphism)
        {
            polymorphism.DerivedTypes.Add(new(typeof(PickupShipment), PickupKind));
        }
    }
#endif
}
