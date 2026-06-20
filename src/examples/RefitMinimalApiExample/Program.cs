// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.MinimalApi.Example;

/// <summary>The entry point for the Refit Minimal API example application.</summary>
public static class Program
{
    /// <summary>Runs the example application.</summary>
    /// <param name="args">The command-line arguments.</param>
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();

        var implementation = new TodoApi();
        app.MapGet("/", static () => Results.Redirect("/todos/1"));
        app.MapGeneratedRefitApi<ITodoApi>(implementation);

        app.Run();
    }
}
