// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation.JsonContexts;

/// <summary>Runs the examples that hand a JSON context to Refit.</summary>
internal static class JsonContextSample
{
    /// <summary>Runs every scenario against a local stand-in for the orders API.</summary>
    /// <returns>A task that completes after every assertion.</returns>
    internal static async Task RunAsync()
    {
        await ContextClientSample.RunAsync();
        await JsonDefaultsSample.RunAsync();
        await ReflectionFallbackSample.RunAsync();
        await OwnOptionsSample.RunAsync();
        await ExistingSettingsSample.RunAsync();
        await JsonTypeInfoParameterSample.RunAsync();
        await JsonTypeInfoSerializerSample.RunAsync();
    }
}
