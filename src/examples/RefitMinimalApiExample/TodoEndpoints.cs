// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;

namespace Refit.MinimalApi.Example;

/// <summary>Generated-style endpoint descriptors for the example Refit interface.</summary>
internal static class TodoEndpoints
{
    /// <summary>Gets all endpoint descriptors.</summary>
    public static IReadOnlyList<RefitMinimalApiEndpoint<ITodoApi>> All { get; } =
    [
        new("/todos/{id}", HttpMethods.Get, GetAsync),
        new("/todos", HttpMethods.Post, CreateAsync)
    ];

    /// <summary>Handles the get endpoint.</summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="implementation">The Refit implementation.</param>
    /// <returns>A task that completes when the response is written.</returns>
    private static async ValueTask GetAsync(HttpContext context, ITodoApi implementation)
    {
        var id = Convert.ToInt32(context.Request.RouteValues["id"], CultureInfo.InvariantCulture);
        var item = await implementation.GetAsync(id, context.RequestAborted).ConfigureAwait(false);

        await WriteItemAsync(context, item).ConfigureAwait(false);
    }

    /// <summary>Handles the create endpoint.</summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="implementation">The Refit implementation.</param>
    /// <returns>A task that completes when the response is written.</returns>
    private static async ValueTask CreateAsync(HttpContext context, ITodoApi implementation)
    {
        var request = await JsonSerializer
            .DeserializeAsync(
                context.Request.Body,
                TodoJsonContext.Default.CreateTodoRequest,
                context.RequestAborted)
            .ConfigureAwait(false);

        if (request is null)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var item = await implementation.CreateAsync(request, context.RequestAborted).ConfigureAwait(false);
        await WriteItemAsync(context, item).ConfigureAwait(false);
    }

    /// <summary>Writes an item response.</summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="item">The item to write.</param>
    /// <returns>A task that completes when the response is written.</returns>
    private static async ValueTask WriteItemAsync(HttpContext context, Todo item)
    {
        context.Response.ContentType = "application/json";
        await JsonSerializer
            .SerializeAsync(
                context.Response.Body,
                item,
                TodoJsonContext.Default.Todo,
                context.RequestAborted)
            .ConfigureAwait(false);
    }
}
