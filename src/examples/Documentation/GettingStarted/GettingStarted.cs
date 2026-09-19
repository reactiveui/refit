// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Refit.Testing;

namespace Refit.Documentation;

/// <summary>Checks a generated client call against a typed local reply.</summary>
internal static class GettingStarted
{
    /// <summary>Reads one person and checks the matching handler expectation.</summary>
    /// <param name="host">The local client and generated serializer used by the scenario.</param>
    /// <returns>A task that completes after the response and expectation assertions.</returns>
    internal static async Task RunAsync(SampleHost host)
    {
        host.Http.Add(Route.Get("/people/{id}"), Reply.With(new Person(1, "Ada")));

        IPeopleApi api = RestService.ForGenerated<IPeopleApi>(host.Client, host.Settings);
        Person person = await api.GetPersonAsync(1, CancellationToken.None);
        Console.WriteLine(person.Name); // Ada

        await host.Http.VerifyAllCalledAsync();
        SampleCheck.Equal(new(1, "Ada"), person);
    }
}
