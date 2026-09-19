// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation.Tooling;

/// <summary>Runs compiler-host examples against the Refit source projects.</summary>
internal static class Program
{
    /// <summary>Fails when a demonstrated tooling contract is not observed.</summary>
    /// <returns>The completion of every compiler demonstration.</returns>
    internal static async Task Main()
    {
        GeneratorSample.Run();
        NameSample.Run();
        PolyfillSample.Run();
        await AnalyzerSample.RunAsync();
        await CodeFixSample.RunAsync();
        CompatibilitySample.CheckAttachmentName();
        await CompatibilitySample.CheckFormObjectAsync();
        CompatibilitySample.CheckJsonContentSerializer();
        CompatibilitySample.CheckJsonBodyMode();
        CompatibilitySample.CheckXmlDtdWarning();
        Console.WriteLine("Tooling samples passed.");
    }
}
