// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>
/// Verifies the observable request adapter's cancellation-token resolution: a method that declares a
/// cancellation token uses the supplied token, while a method without one uses <see cref="CancellationToken.None"/>.
/// </summary>
public sealed class ObservableRequestCancellationTests
{
    /// <summary>The base address used to build the request messages.</summary>
    private const string BaseAddress = "http://api/";

    /// <summary>The identifier passed to the cancellation-token-free observable method.</summary>
    private const int PatchId = 7;

    /// <summary>An observable method declaring a cancellation token completes using the supplied token.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ObservableWithCancellationTokenUsesSuppliedToken()
    {
        var fixture = new RequestBuilderImplementation<IObservableCancellableMethods>();
        var factory = fixture.BuildRestResultFuncForMethod(nameof(IObservableCancellableMethods.GetWithCancellation));
        var handler = new TestHttpMessageHandler();

        using var cts = new CancellationTokenSource();
        var observable = (IObservable<string>)factory(
            new(handler) { BaseAddress = new(BaseAddress) },
            ["value", cts.Token])!;

        var result = await ObservableTestHelpers.Await(observable);

        await Assert.That(result).IsEqualTo("test");
        await Assert.That(handler.RequestMessage!.RequestUri!.ToString()).IsEqualTo("http://api/value/value");
    }

    /// <summary>An observable method without a cancellation token completes using <see cref="CancellationToken.None"/>.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ObservableWithoutCancellationTokenUsesNone()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();
        var factory = fixture.BuildRestResultFuncForMethod(nameof(IDummyHttpApi.PatchSomething));
        var handler = new TestHttpMessageHandler();

        var observable = (IObservable<string>)factory(
            new(handler) { BaseAddress = new(BaseAddress) },
            [PatchId, "body"])!;

        var result = await ObservableTestHelpers.Await(observable);

        await Assert.That(result).IsEqualTo("test");
        await Assert.That(handler.RequestMessage!.RequestUri!.ToString()).IsEqualTo("http://api/foo/bar/7");
    }
}
