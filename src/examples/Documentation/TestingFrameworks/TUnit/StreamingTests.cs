// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net;
using Refit.Testing;

namespace Refit.Documentation.TestingFrameworks;

/// <summary>Watching people arrive one at a time, instead of waiting for a whole response or upload to finish.</summary>
public sealed class StreamingTests
{
    // Problem: can my code react to each item the moment it streams in, instead of waiting for everything?
    [Test]
    public async Task WatchPeople_SeesEachOneAsTheyArrive()
    {
        StreamSource source = new(StreamingContentFormat.JsonLines);
        using StubHttp http = new() { { Route.Get("/people/live"), Reply.Stream(source) } };
        IPeopleApi api = http.CreateGeneratedClient<IPeopleApi>("https://api.example.com", TestSettings.Create());
        await using IAsyncEnumerator<Person> people = api.WatchPeopleAsync(CancellationToken.None).GetAsyncEnumerator();

        source.Release(new Person(1, "Ada"));
        await Assert.That(await people.MoveNextAsync()).IsTrue();
        await Assert.That(people.Current.Name).IsEqualTo("Ada");

        ValueTask<bool> nextPerson = people.MoveNextAsync();
        await Assert.That(nextPerson.IsCompleted).IsFalse(); // Grace has not arrived yet.

        source.Release(new Person(2, "Grace"));
        await Assert.That(await nextPerson).IsTrue();
        await Assert.That(people.Current.Name).IsEqualTo("Grace");

        source.Complete();
    }

    // Problem: can the server start handling my upload before I've finished sending it?
    [Test]
    public async Task UploadPeople_ServerDoesNotWaitForTheWholeUpload()
    {
        int peopleProducedSoFar = 0;

        IEnumerable<Person> ProducePeople()
        {
            peopleProducedSoFar++;
            yield return new(1, "Ada");
            peopleProducedSoFar++;
            yield return new(2, "Grace");
        }

        int producedWhenServerStartedReading = -1;
        string? uploadedJson = null;
        using StubHttp http = new()
        {
            {
                Route.Post("/people/import"),
                Reply.From(async (request, cancellationToken) =>
                {
                    // RequestCapture.None (set below) means StubHttp never buffered the body ahead of
                    // time, so this line genuinely runs before ProducePeople() has finished.
                    producedWhenServerStartedReading = peopleProducedSoFar;
                    uploadedJson = await request.Content!.ReadAsStringAsync(cancellationToken);
                    return new HttpResponseMessage(HttpStatusCode.Accepted);
                })
            },
        };
        http.RequestCapture = RequestCapture.None;
        IPeopleApi api = http.CreateGeneratedClient<IPeopleApi>("https://api.example.com", TestSettings.Create());

        await api.ImportPeopleAsync(ProducePeople());

        await Assert.That(producedWhenServerStartedReading).IsEqualTo(0);
        await Assert.That(uploadedJson).Contains("Grace");
    }
}
