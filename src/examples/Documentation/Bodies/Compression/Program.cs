// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation.Compression;

/// <summary>Runs coding options that require the .NET 11 API surface.</summary>
internal static class Program
{
    /// <summary>Checks all supported compressors against a local handler.</summary>
    /// <returns>Completion of the coding checks.</returns>
    internal static async Task Main()
    {
        await CompressionSample.RunAsync();
        Console.WriteLine(".NET 11 compression examples passed.");
    }
}
