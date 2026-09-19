// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation.Content;

/// <summary>Runs body writers, stream readers and a generated return adapter.</summary>
internal static class Program
{
    /// <summary>Checks the local examples without a live service.</summary>
    /// <returns>Completion of all content demonstrations.</returns>
    internal static async Task Main()
    {
        await ContentSample.RunAsync();
        Console.WriteLine("Content and adapter examples passed.");
    }
}
