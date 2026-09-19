// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation.RuntimeHelpers;

/// <summary>Executes generated-helper and explicitly reflected metadata examples.</summary>
internal static class Program
{
    /// <summary>Checks every helper demonstration against the source projects.</summary>
    /// <returns>Completion of the HTTP body and dispatch demonstrations.</returns>
    internal static async Task Main()
    {
        QuerySample.Run();
        AttributeSample.Run();
        PathSample.Run();
        MetadataSample.Run();
        await BodySample.RunAsync();
        await DispatchSample.RunAsync();
        Console.WriteLine("Runtime helper samples passed.");
    }
}
