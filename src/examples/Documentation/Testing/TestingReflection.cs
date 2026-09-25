// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
#if !NATIVE_AOT
using Refit.Testing;

namespace Refit.Documentation;

/// <summary>Exercises reflection-capable client factories in a JIT test host.</summary>
internal static class TestingReflection
{
    /// <summary>Problem: How do you create a reflection-based Refit client, with no source generator, against a stub?</summary>
    /// <returns>A task that completes after both reflection-capable factory overloads are checked.</returns>
    internal static async Task RunAsync()
    {
        using StubHttp http = new StubHttp { { Route.Get("/people/1"), Reply.Json("{\"id\":1,\"name\":\"Ada\"}") } };
        ITestingApi defaults = http.CreateClient<ITestingApi>("https://api.example.com");
        TestingPerson first = await defaults.GetAsync(1); // first.Name == "Ada"

        RefitSettings settings = new RefitSettings(new SystemTextJsonContentSerializer(TestingJsonContext.Default.Options));
        http.Add(Route.Get("/people/1"), Reply.Json("{\"id\":1,\"name\":\"Ada\"}"));
        ITestingApi configured = http.CreateClient<ITestingApi>("https://api.example.com", settings);
        TestingPerson second = await configured.GetAsync(1); // second == first

        // Checks for this sample (not part of the documentation excerpt):
        SampleCheck.Equal("Ada", first.Name);
        SampleCheck.Equal(first, second);
        await http.VerifyAllCalledAsync();
    }
}
#endif
