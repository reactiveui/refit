// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text;

namespace Refit.Tests;

/// <summary>Verifies both the reflection and generated request builders surface a custom return type through a
/// registered or discovered <see cref="IReturnTypeAdapter{TReturn, TResult}"/> (issue #2165).</summary>
public sealed class ReturnTypeAdapterTests
{
    /// <summary>The id of the sample user returned by the stub handler.</summary>
    private const int UserId = 7;

    /// <summary>The name of the sample user returned by the stub handler.</summary>
    private const string UserName = "Ada";

    /// <summary>The number of sends expected after the deferred call is invoked twice.</summary>
    private const int SendsAfterTwoInvocations = 2;

    /// <summary>The sample user response body carrying <see cref="UserId"/> and <see cref="UserName"/>.</summary>
    private const string UserJson = """{"id":7,"name":"Ada"}""";

    /// <summary>Verifies the reflection builder surfaces a custom return type from a runtime-registered adapter,
    /// defers the request until invoked, and rebuilds the request on each invocation.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReflectionPathAdaptsCustomReturnTypeThroughSeam()
    {
        var handler = new TestHttpMessageHandler
        {
            ContentFactory = static () => new StringContent(UserJson, Encoding.UTF8, "application/json"),
        };
        var client = new HttpClient(handler) { BaseAddress = new("https://api.example.com") };

        var settings = new RefitSettings();
        settings.ReturnTypeAdapters.Add(typeof(DeferredCallAdapter<>));

        // Build through the reflection request builder directly so the generated inline path is not taken.
        var builder = new RequestBuilderImplementation<IDeferredCallApi>(settings);
        var invoke = builder.BuildRestResultFuncForMethod(nameof(IDeferredCallApi.GetUser));
        var deferred = (DeferredCall<AdapterUser>)invoke(client, [UserId])!;

        // The adapter surfaces the call synchronously, so nothing is sent until it is invoked.
        await Assert.That(handler.MessagesSent).IsEqualTo(0);

        var user = await deferred.InvokeAsync(CancellationToken.None);
        await Assert.That(handler.MessagesSent).IsEqualTo(1);
        await Assert.That(user!.Id).IsEqualTo(UserId);
        await Assert.That(user.Name).IsEqualTo(UserName);

        // The reflection builder rebuilds the request on each invocation, so the deferred call re-runs.
        _ = await deferred.InvokeAsync(CancellationToken.None);
        await Assert.That(handler.MessagesSent).IsEqualTo(SendsAfterTwoInvocations);
    }

    /// <summary>Verifies the generated inline path surfaces a compile-time-discovered adapter's custom return type and
    /// defers the request until invoked, without any runtime adapter registration.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GeneratedPathAdaptsCustomReturnTypeThroughSeam()
    {
        var handler = new TestHttpMessageHandler
        {
            ContentFactory = static () => new StringContent(UserJson, Encoding.UTF8, "application/json"),
        };
        var client = new HttpClient(handler) { BaseAddress = new("https://api.example.com") };

        // No adapter is registered on the settings: the generated code discovered it at compile time. If this used the
        // reflection builder it would throw for the unrecognized synchronous return type, so success proves the
        // generated inline path handled the adapter.
        var api = RestService.For<IDeferredCallApi>(client, new RefitSettings());

        var deferred = api.GetUser(UserId);
        await Assert.That(handler.MessagesSent).IsEqualTo(0);

        var user = await deferred.InvokeAsync(CancellationToken.None);
        await Assert.That(handler.MessagesSent).IsEqualTo(1);
        await Assert.That(user!.Id).IsEqualTo(UserId);
        await Assert.That(user.Name).IsEqualTo(UserName);
    }
}
