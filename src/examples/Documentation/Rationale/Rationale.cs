// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Refit.Testing;

namespace Refit.Documentation;

/// <summary>Compares a generated Refit client with an explicitly implemented HttpClient request.</summary>
internal static class Rationale
{
    /// <summary>Calls both client forms against identical replies and checks their results agree.</summary>
    /// <param name="host">The local handler and generated-metadata client configuration for these samples.</param>
    /// <returns>A task that completes after the sample assertions pass.</returns>
    internal static async Task RunAsync(SampleHost host)
    {
        host.Http.Add(Route.Get("/people/{id}"), Reply.Json("{\"id\":1,\"name\":\"Ada\"}"));
        host.Http.Add(Route.Get("/people/{id}"), Reply.Json("{\"id\":1,\"name\":\"Ada\"}"));

        IPeopleApi api = RestService.ForGenerated<IPeopleApi>(host.Client, host.Settings);

        Person fromRefit = await api.GetPersonAsync(1, CancellationToken.None);
        Person? fromRaw = await RawPeopleApi.GetPersonAsync(host.Client, 1, CancellationToken.None);
        Console.WriteLine(fromRefit.Name); // Ada
        Console.WriteLine(fromRaw?.Name); // Ada

        SampleCheck.Equal(fromRefit, fromRaw);
        await host.Http.VerifyAllCalledAsync();
    }
}
