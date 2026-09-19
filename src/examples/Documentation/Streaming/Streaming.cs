// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Refit.Testing;

namespace Refit.Documentation;

/// <summary>Checks that three streaming wire formats yield the same people.</summary>
internal static class Streaming
{
    /// <summary>Enumerates each supported format with a deadline and checks item and request counts.</summary>
    /// <param name="host">The local handler and generated-metadata client configuration for these samples.</param>
    /// <returns>A task that completes after the sample assertions pass.</returns>
    internal static async Task RunAsync(SampleHost host)
    {
        const int ExpectedPeople = 2;
        const int DeadlineSeconds = 10;

        foreach ((string body, string mediaType) in new[]
        {
            ("[{\"id\":1,\"name\":\"Ada\"},{\"id\":2,\"name\":\"Grace\"}]", "application/json"),
            ("{\"id\":1,\"name\":\"Ada\"}\n{\"id\":2,\"name\":\"Grace\"}\n", "application/x-ndjson"),
            ("data: {\"id\":1,\"name\":\"Ada\"}\n\ndata: {\"id\":2,\"name\":\"Grace\"}\n\n", "text/event-stream"),
        })
        {
            host.Http.Add(Route.Get("/people"), Reply.Text(body, mediaType));

            int before = host.Http.Requests.Count;
            IStreamingApi api = RestService.ForGenerated<IStreamingApi>(host.Client, host.Settings);
            int count = 0;

            using CancellationTokenSource cancellation = new(TimeSpan.FromSeconds(DeadlineSeconds));
            await foreach (Person person in api.ReadPeopleAsync(cancellation.Token))
            {
                Console.WriteLine(person.Name); // Ada, then Grace
                count++;
            }

            await host.Http.VerifyAllCalledAsync();
            SampleCheck.Equal(ExpectedPeople, count);
            SampleCheck.Equal(before + 1, host.Http.Requests.Count);
        }
    }
}
