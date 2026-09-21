// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Refit.Documentation;

using NativePeopleClient client = new();

Person person = await client.Api.GetPersonAsync(1, CancellationToken.None);

SampleCheck.Equal(new(1, "Ada"), person);

Person fromSettings = await client.SettingsApi.GetPersonAsync(1, CancellationToken.None);

SampleCheck.Equal(person, fromSettings);

await Serialization.RunAsync();

using SampleHost host = new();

await Responses.RunAsync(host);

await Errors.RunAsync(host);

await Refit.Documentation.Testing.RunAsync();

await Clients.RunAsync(host);

await GettingStarted.RunAsync(host);

await ReturnTypes.RunAsync(host);

await Streaming.RunAsync(host);

await Routes.RunAsync(host);

await Queries.RunAsync(host);

await Formatters.RunAsync(host);

await Converters.RunAsync(host);

await Headers.RunAsync(host);

await Bodies.RunAsync(host);

await Rationale.RunAsync(host);

await Aot.RunAsync(host);

await Refit.Documentation.JsonContexts.JsonContextSample.RunAsync();

Console.WriteLine("Native AOT documentation example passed.");

/// <summary>The entry point for executable native documentation checks.</summary>
internal sealed partial class Program
{
    /// <summary>Initializes a new instance of the <see cref="Program"/> class.</summary>
    private Program()
    {
    }
}
