// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using LibraryWithSDKandRefitService;

namespace RestApiforTest;

/// <summary>Hosts the five routes called by the local Refit client.</summary>
internal static class Program
{
    /// <summary>Runs the HTTP service until the host shuts down.</summary>
    /// <param name="args">The web host arguments.</param>
    /// <returns>The lifetime of the HTTP service.</returns>
    internal static async Task Main(string[] args)
    {
        const string IdRoute = "/{id}";
        var builder = WebApplication.CreateSlimBuilder(args);
        _ = builder.Services.ConfigureHttpJsonOptions(static options => options.SerializerOptions.TypeInfoResolverChain.Insert(0, LocalApiJsonContext.Default));
        await using var app = builder.Build();
        var values = app.MapGroup("/api/values");
        _ = values.MapGet("/", static () => "Get Api with no argument was Called");
        _ = values.MapGet(IdRoute, static (int id) => "Get Api was called");
        _ = values.MapPost("/", static (ModelForTest testObject) => "Post Api was Called");
        _ = values.MapPut(IdRoute, static (int id, ModelForTest testObject) => "Put Api was called");
        _ = values.MapDelete(IdRoute, static (int id) => "Delete Api was Called");
        await app.RunAsync().ConfigureAwait(false);
    }
}
