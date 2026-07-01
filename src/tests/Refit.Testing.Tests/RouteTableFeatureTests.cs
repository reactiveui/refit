// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;

namespace Refit.Testing.Tests;

/// <summary>
/// End-to-end tests for the Refit-native features: route templates that mirror the interface attributes,
/// typed responses serialized by the client's serializer, and typed request-body capture.
/// </summary>
public sealed class RouteTableFeatureTests
{
    /// <summary>The base address the sample client sends requests to.</summary>
    private const string BaseUrl = "https://api.test";

    /// <summary>A sample user identifier exercised by the template tests.</summary>
    private const int SampleUserId = 7;

    /// <summary>Verifies a <c>{id}</c> template placeholder matches any single path segment.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task TemplatePlaceholderMatchesPathSegment()
    {
        var handler = new StubHttp
        {
            {
                Route.Get("/users/{id}"),
                Reply.With(new User(SampleUserId, "octocat"))
            },
        };

        var api = handler.CreateClient<IUserApi>(BaseUrl);
        var user = await api.GetUser(SampleUserId);

        await Assert.That(user.Id).IsEqualTo(SampleUserId);
        await Assert.That(user.Login).IsEqualTo("octocat");
        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a typed reply is serialized with the client's own serializer and round-trips.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task TypedReplyIsSerializedByClientSerializer()
    {
        var handler = new StubHttp
        {
            {
                Route.Get("/users/1"),
                Reply.With(new User(1, "hubot"))
            },
        };

        var api = handler.CreateClient<IUserApi>(BaseUrl);
        var user = await api.GetUser(1);

        await Assert.That(user.Login).IsEqualTo("hubot");
    }

    /// <summary>Verifies the sent request body can be read back as a typed object.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task LastRequestBodyDeserializesSentPayload()
    {
        var handler = new StubHttp
        {
            {
                Route.Post("/users"),
                Reply.With(new User(2, "created"), HttpStatusCode.Created)
            },
        };

        var api = handler.CreateClient<IUserApi>(BaseUrl);
        _ = await api.CreateUser(new NewUser("mona"));

        var sent = await handler.LastRequestBodyAsync<NewUser>();

        await Assert.That(sent!.Login).IsEqualTo("mona");
        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a typed reply carries the configured non-default status code.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task TypedReplyCarriesStatusCode()
    {
        var handler = new StubHttp
        {
            {
                Route.Post("/users"),
                Reply.With(new User(3, "created"), HttpStatusCode.Created)
            },
        };

        var api = handler.CreateClient<IUserApi>(BaseUrl);
        using var response = await api.CreateUserResponse(new NewUser("lee"));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
    }
}
