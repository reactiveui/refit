// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Serilog;

namespace Meow;

/// <summary>The entry point for the Meow example application.</summary>
public static class Program
{
    /// <summary>Runs the example application.</summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .MinimumLevel.Verbose()
            .CreateLogger();

        await Issue2056And2058Demo.RunAsync();

        Log.Information("Issue #2056 and #2058 demo checks passed.");

        using var service = new CatsService(new("https://api.thecatapi.com"));
        var results = await service.SearchAsync("bengal");

        Log.Debug("{Results}", results);
    }
}
