// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json;
using Refit.Testing;

namespace Refit.Documentation;

/// <summary>Checks generated client dispatch with explicitly registered JSON metadata.</summary>
internal static class Aot
{
    /// <summary>Reusable metadata for the person returned by the native-compatible call.</summary>
    private static readonly JsonSerializerOptions JsonOptions = new(SampleJsonContext.Default.Options) { TypeInfoResolver = SampleJsonContext.Default };

    /// <summary>The generated serializer settings used by the native-compatible client.</summary>
    private static readonly RefitSettings Settings = new(new SystemTextJsonContentSerializer(JsonOptions));

    /// <summary>Reads a person through generated dispatch and verifies the local expectation.</summary>
    /// <param name="host">The local handler and client used for the request.</param>
    /// <returns>A task that completes after the body and expectation assertions.</returns>
    internal static async Task RunAsync(SampleHost host)
    {
        host.Http.Add(Route.Get("/people/{id}"), Reply.Json("{\"id\":1,\"name\":\"Ada\"}"));

        IPeopleApi api = RestService.ForGenerated<IPeopleApi>(host.Client, Settings);
        Person person = await api.GetPersonAsync(1, CancellationToken.None);
        Console.WriteLine(person.Name); // Ada

        SampleCheck.Equal(new(1, "Ada"), person);
        await host.Http.VerifyAllCalledAsync();
    }
}
