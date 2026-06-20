// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;

namespace Refit.MinimalApi.Tests;

/// <summary>Runtime tests for Refit Minimal API endpoint mapping.</summary>
public sealed class RefitEndpointRouteBuilderExtensionsTests
{
    /// <summary>Identifier returned by the create operation.</summary>
    internal const int CreatedItemId = 201;

    /// <summary>Identifier used by descriptor mapping.</summary>
    private const int DescriptorItemId = 42;

    /// <summary>Identifier used by reflection mapping.</summary>
    private const int ReflectedItemId = 7;

    /// <summary>Verifies that descriptor-based endpoint mapping invokes the supplied handler.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task MapRefitApiWithGeneratedDescriptorMapsAndInvokesEndpoint()
    {
        var app = new TestEndpointRouteBuilder();
        var api = new TodoApi();

        app.MapRefitApi<ITodoApi>(
            api,
            [
                new(
                    "/todos/{id}",
                    HttpMethods.Get,
                    static async (context, implementation) =>
                    {
                        var id = Convert.ToInt32(
                            context.Request.RouteValues["id"],
                            CultureInfo.InvariantCulture);
                        var item = await implementation
                            .GetTodoAsync(id, "descriptor", "generated", context.RequestAborted)
                            .ConfigureAwait(false);

                        context.Response.ContentType = "application/json";
                        await JsonSerializer
                            .SerializeAsync(
                                context.Response.Body,
                                item,
                                MinimalApiTestJsonContext.Default.Todo,
                                context.RequestAborted)
                            .ConfigureAwait(false);
                    })
            ]);

        var endpoint = GetEndpoint(app, "/todos/{id}", HttpMethods.Get);
        var context = CreateHttpContext(app);
        SetRouteValue(context, "id", DescriptorItemId.ToString(CultureInfo.InvariantCulture));

        await endpoint.RequestDelegate!(context).ConfigureAwait(false);

        var item = await ReadResponseAsync<Todo>(
            context,
            MinimalApiTestJsonContext.Default.Todo).ConfigureAwait(false);

        await Assert.That(item).IsNotNull();
        await Assert.That(item!.Id).IsEqualTo(DescriptorItemId);
        await Assert.That(item.Title).IsEqualTo("descriptor:generated:False");
    }

    /// <summary>Verifies that reflection-based endpoint mapping binds route, query, header and body values.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:RequiresUnreferencedCode",
        Justification = "This test intentionally exercises reflection-based Minimal API mapping.")]
    [UnconditionalSuppressMessage(
        "AOT",
        "IL3050:RequiresDynamicCode",
        Justification = "This test intentionally exercises reflection-based Minimal API mapping.")]
    [SkipIfNativeAot]
    public async Task MapRefitApiWithReflectionBindsRequestValues()
    {
        var app = new TestEndpointRouteBuilder();
        var api = new TodoApi();

        app.MapRefitApi<ITodoApi>(api);

        var getEndpoint = GetEndpoint(app, "/todos/{id}", HttpMethods.Get);
        var getContext = CreateHttpContext(app);
        SetRouteValue(getContext, "id", ReflectedItemId.ToString(CultureInfo.InvariantCulture));
        getContext.Request.QueryString = new("?tag=reflected");
        getContext.Request.Headers["X-Trace"] = "header";

        await getEndpoint.RequestDelegate!(getContext).ConfigureAwait(false);

        var getItem = await ReadResponseAsync<Todo>(
            getContext,
            MinimalApiTestJsonContext.Default.Todo).ConfigureAwait(false);

        await Assert.That(getItem).IsNotNull();
        await Assert.That(getItem!.Id).IsEqualTo(ReflectedItemId);
        await Assert.That(getItem.Title).IsEqualTo("reflected:header:False");

        var postEndpoint = GetEndpoint(app, "/todos", HttpMethods.Post);
        var postContext = CreateHttpContext(app);
        postContext.Request.Headers["X-Trace"] = "created";
        postContext.Request.ContentType = "application/json";
        postContext.Request.Body = new MemoryStream(
            Encoding.UTF8.GetBytes("""{"title":"from body"}"""));

        await postEndpoint.RequestDelegate!(postContext).ConfigureAwait(false);

        var postItem = await ReadResponseAsync<Todo>(
            postContext,
            MinimalApiTestJsonContext.Default.Todo).ConfigureAwait(false);

        await Assert.That(postItem).IsNotNull();
        await Assert.That(postItem!.Id).IsEqualTo(CreatedItemId);
        await Assert.That(postItem.Title).IsEqualTo("from body:created");
    }

    /// <summary>Finds a mapped route endpoint.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="method">The HTTP method.</param>
    /// <returns>The matching route endpoint.</returns>
    private static RouteEndpoint GetEndpoint(TestEndpointRouteBuilder app, string pattern, string method) =>
        app.DataSources
            .SelectMany(static source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(
                endpoint =>
                    endpoint.RoutePattern.RawText == pattern
                    && endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.Contains(method) == true);

    /// <summary>Creates an HTTP context for direct endpoint invocation.</summary>
    /// <param name="app">The endpoint route builder that owns the request services.</param>
    /// <returns>The prepared HTTP context.</returns>
    private static DefaultHttpContext CreateHttpContext(TestEndpointRouteBuilder app) =>
        new()
        {
            RequestServices = app.ServiceProvider,
            Response = { Body = new MemoryStream() },
        };

    /// <summary>Sets a route value on a directly invoked endpoint context.</summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="name">The route value name.</param>
    /// <param name="value">The route value.</param>
    private static void SetRouteValue(DefaultHttpContext context, string name, object value)
    {
        var routeValuesFeature = context.Features.Get<IRouteValuesFeature>();
        if (routeValuesFeature is null)
        {
            routeValuesFeature = new TestRouteValuesFeature();
            context.Features.Set(routeValuesFeature);
        }

        routeValuesFeature.RouteValues[name] = value;
        context.Request.RouteValues = routeValuesFeature.RouteValues;

        var routingFeature = context.Features.Get<IRoutingFeature>();
        if (routingFeature is null)
        {
            routingFeature = new RoutingFeature { RouteData = new() };
            context.Features.Set(routingFeature);
        }

        routingFeature.RouteData ??= new();
        routingFeature.RouteData.Values[name] = value;
    }

    /// <summary>Reads and deserializes the response body.</summary>
    /// <typeparam name="T">The response type.</typeparam>
    /// <param name="context">The HTTP context containing the response.</param>
    /// <param name="jsonTypeInfo">The JSON type metadata.</param>
    /// <returns>The deserialized response value.</returns>
    private static ValueTask<T?> ReadResponseAsync<T>(
        HttpContext context,
        JsonTypeInfo<T> jsonTypeInfo)
    {
        context.Response.Body.Position = 0;
        return JsonSerializer.DeserializeAsync(
            context.Response.Body,
            jsonTypeInfo,
            context.RequestAborted);
    }
}
