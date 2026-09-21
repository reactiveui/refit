// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net;
using System.Text;
using Refit.Testing;

namespace Refit.Documentation.JsonContexts;

/// <summary>Stands in for the shop's HTTP service with fixed replies, so the examples run without a network.</summary>
internal static class OrdersServer
{
    /// <summary>The base address of the shop's API.</summary>
    internal const string Origin = "https://shop.example";

    /// <summary>The identifier of the order every reply describes.</summary>
    internal const int OrderId = 5;

    /// <summary>An order with camelCase names, as the shop's own API writes them.</summary>
    internal const string OrderJson = """{"id":5,"customer":"Ada","status":"Shipped","lines":[{"sku":"KB-1","quantity":1,"unitPrice":49.5},{"sku":"MS-2","quantity":2,"unitPrice":19}]}""";

    /// <summary>The order list with camelCase names.</summary>
    internal const string OrderListJson = $"[{OrderJson}]";

    /// <summary>A courier shipment whose <c>kind</c> property selects the derived type.</summary>
    internal const string ShipmentJson = """{"kind":"courier","reference":"S-77","trackingNumber":"TN-5501"}""";

    /// <summary>The request body the client writes for a new order.</summary>
    internal const string NewOrderJson = """{"customer":"Ada","lines":[{"sku":"KB-1","quantity":1,"unitPrice":49.5}]}""";

    /// <summary>The request body of an order with no lines, which the shop rejects.</summary>
    internal const string EmptyOrderJson = """{"customer":"Ada","lines":[]}""";

    /// <summary>The problem document the shop returns for an order with no lines.</summary>
    internal const string ProblemJson = """{"type":"about:blank","title":"Invalid order","status":400,"errors":{"lines":["An order needs at least one line."]}}""";

    /// <summary>An order with snake_case names and prices written as text, as a different service might write it.</summary>
    internal const string SnakeCaseOrderJson = """
        {"id":5,"customer":"Ada","status":"Shipped","lines":[{"sku":"KB-1","quantity":1,"unit_price":"49.50"},{"sku":"MS-2","quantity":2,"unit_price":"19.00"}]}
        """;

    /// <summary>The request body a snake_case client writes for a new order.</summary>
    internal const string SnakeCaseNewOrderJson = """{"customer":"Ada","lines":[{"sku":"KB-1","quantity":1,"unit_price":"49.50"}]}""";

    /// <summary>A pickup shipment, a kind that only the app's own serializer settings register.</summary>
    internal const string PickupShipmentJson = """{"kind":"pickup","reference":"S-78","store":"Harbour Street"}""";

    /// <summary>Creates a handler that answers the orders API with camelCase JSON.</summary>
    /// <returns>A handler that records every request it receives.</returns>
    internal static StubHttp Create() => new()
    {
        { Reads("/orders/{id}"), Reply.Json(OrderJson) },
        { Reads("/orders"), Reply.Json(OrderListJson) },
        { Reads("/orders/{id}/shipment"), Reply.Json(ShipmentJson) },
        { Places(NewOrderJson), Reply.Json(OrderJson) },
        { Places(EmptyOrderJson), Reply.From(Problem) },
    };

    /// <summary>Creates a handler that answers the orders API with snake_case JSON and text prices.</summary>
    /// <returns>A handler that records every request it receives.</returns>
    internal static StubHttp CreateSnakeCase() => new() { { Reads("/orders/{id}"), Reply.Json(SnakeCaseOrderJson) }, { Places(SnakeCaseNewOrderJson), Reply.Json(SnakeCaseOrderJson) } };

    /// <summary>Creates a handler that answers a shipment read with a pickup shipment.</summary>
    /// <returns>A handler that records every request it receives.</returns>
    internal static StubHttp CreatePickupShipment() => new() { { Reads("/orders/{id}/shipment"), Reply.Json(PickupShipmentJson) } };

    /// <summary>Creates a client whose requests go to a handler.</summary>
    /// <param name="http">The handler that answers every request.</param>
    /// <returns>A client with the shop's base address. The caller owns the handler.</returns>
    internal static HttpClient CreateClient(StubHttp http) => new(http, disposeHandler: false) { BaseAddress = new(Origin) };

    /// <summary>Describes a reusable route that reads a path.</summary>
    /// <param name="template">The path template.</param>
    /// <returns>The route.</returns>
    private static RouteMatcher Reads(string template) => new() { Method = HttpMethod.Get, Template = template, Reusable = true };

    /// <summary>Describes a reusable route that accepts one exact request body.</summary>
    /// <param name="body">The JSON text the request must carry.</param>
    /// <returns>The route.</returns>
    private static RouteMatcher Places(string body) => new() { Method = HttpMethod.Post, Template = "/orders", Body = body, Reusable = true };

    /// <summary>Builds the 400 reply that carries the problem document.</summary>
    /// <param name="request">The request being answered.</param>
    /// <returns>The reply.</returns>
    private static HttpResponseMessage Problem(HttpRequestMessage request) => new(HttpStatusCode.BadRequest)
    {
        RequestMessage = request,
        Content = new StringContent(ProblemJson, Encoding.UTF8, "application/problem+json"),
    };
}
