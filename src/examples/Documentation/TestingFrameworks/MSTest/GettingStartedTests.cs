// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Refit.Testing;

namespace Refit.Documentation.TestingFrameworks;

/// <summary>The first two things you need to test a Refit client: reading a reply, and sending a body.</summary>
[TestClass]
public sealed class GettingStartedTests
{
    // Problem: does my code correctly read back the person my API returns?
    [TestMethod]
    public async Task GetPerson_ReturnsTheStubbedPerson()
    {
        using StubHttp http = new()
        {
            { Route.Get("/people/{id}"), Reply.With(new Person(1, "Ada")) },
        };
        IPeopleApi api = http.CreateGeneratedClient<IPeopleApi>("https://api.example.com", TestSettings.Create());

        Person person = await api.GetPersonAsync(1);

        Assert.AreEqual("Ada", person.Name);
    }

    // Problem: did my app actually send the data I expected to the server?
    [TestMethod]
    public async Task CreatePerson_SendsTheRightJsonBody()
    {
        using StubHttp http = new()
        {
            { Route.Post("/people"), Reply.With(new Person(2, "Grace"), HttpStatusCode.Created) },
        };
        IPeopleApi api = http.CreateGeneratedClient<IPeopleApi>("https://api.example.com", TestSettings.Create());

        await api.CreatePersonAsync(new Person(2, "Grace"));

        Person? sentPerson = await http.LastRequestBodyAsync<Person>();
        Assert.AreEqual("Grace", sentPerson?.Name);
    }
}
