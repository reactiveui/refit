// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
#if !NATIVE_AOT
using Refit.Testing;

namespace Refit.Documentation;

/// <summary>Exercises reflection-capable client factories in a JIT test host.</summary>
internal static class TestingReflection
{
    /// <summary>Checks both reflection-capable factory overloads through local typed HTTP calls.</summary>
    /// <returns>A task that completes after both clients return the expected person.</returns>
    internal static async Task RunAsync()
    {
        using StubHttp http = new();
        http.Add(Route.Get("/people/1"), Reply.Json("{\"id\":1,\"name\":\"Ada\"}"));
        ITestingApi defaults = http.CreateClient<ITestingApi>("https://people.example");
        SampleCheck.Equal(true, defaults is not null);
        TestingPerson first = await defaults!.GetAsync(1);
        SampleCheck.Equal("Ada", first.Name);
        RefitSettings settings = new(new SystemTextJsonContentSerializer(TestingJsonContext.Default.Options));
        http.Add(Route.Get("/people/1"), Reply.Json("{\"id\":1,\"name\":\"Ada\"}"));
        ITestingApi configured = http.CreateClient<ITestingApi>("https://people.example", settings);
        SampleCheck.Equal(true, configured is not null);
        TestingPerson second = await configured!.GetAsync(1);
        SampleCheck.Equal(first, second);
        await http.VerifyAllCalledAsync();
    }
}
#endif
