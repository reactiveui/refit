// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Refit.Testing;

namespace Refit.Documentation;

/// <summary>Returns a model using the JSON contract supplied by the route.</summary>
internal static class TypedJsonReply
{
    /// <summary>Reads a typed reply through a plain HTTP client and a generated JSON context.</summary>
    /// <param name="cancellationToken">Cancels the HTTP send and JSON read.</param>
    /// <returns>Completion after the status and returned person have been checked.</returns>
    internal static async Task RunAsync(CancellationToken cancellationToken)
    {
        TestingPerson person = new(1, "Ada");
        using StubHttp stub = new();
        stub.Add(Route.Get("/people/1"), Reply.With(person, TestingJsonContext.Default.TestingPerson));
        stub.Add(Route.Post("/people"), Reply.With(person, TestingJsonContext.Default.TestingPerson, HttpStatusCode.Created));
        JsonObject profile = new() { ["display_name"] = person.Name };
        stub.Add(Route.Get("/profile"), Reply.With(profile, TestingJsonContext.Default.JsonObject));

        using HttpClient client = new(stub) { BaseAddress = new Uri("https://api.example.com") };
        TestingPerson? found = await client.GetFromJsonAsync(new Uri("/people/1", UriKind.Relative), TestingJsonContext.Default.TestingPerson, cancellationToken);
        using HttpResponseMessage created = await client.PostAsJsonAsync(new Uri("/people", UriKind.Relative), person, TestingJsonContext.Default.TestingPerson, cancellationToken);
        TestingPerson? saved = await created.Content.ReadFromJsonAsync(TestingJsonContext.Default.TestingPerson, cancellationToken);
        JsonObject? document = await client.GetFromJsonAsync(new Uri("/profile", UriKind.Relative), TestingJsonContext.Default.JsonObject, cancellationToken);
        SampleCheck.Equal(person.Name, (string?)document?["display_name"]);
        SampleCheck.Equal(person, found);
        SampleCheck.Equal(HttpStatusCode.Created, created.StatusCode);
        SampleCheck.Equal(person, saved);
    }
}
