// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Verifies the per-call <see cref="TimeoutAttribute"/> for both the source-generated and reflection request
/// paths: a slow response over the deadline cancels, a fast response under it succeeds, and a caller cancellation token
/// still cancels alongside the timeout.</summary>
public sealed class TimeoutAttributeTests
{
    /// <summary>The base address used to build the request messages.</summary>
    private const string BaseAddress = "http://localhost/";

    /// <summary>Source-generated: a void method whose response outlasts its short timeout is canceled.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SourceGenVoidTimeoutCancelsSlowResponse()
    {
        using var handler = new DelayingHttpMessageHandler(Timeout.InfiniteTimeSpan);
        using var client = new HttpClient(handler) { BaseAddress = new(BaseAddress) };
        var api = RestService.For<ITimeoutApi>(client);

        await Assert.That(() => api.GetVoidShortTimeout()).Throws<OperationCanceledException>();
    }

    /// <summary>Source-generated: a value-returning method whose response outlasts its short timeout is canceled.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SourceGenValueTimeoutCancelsSlowResponse()
    {
        using var handler = new DelayingHttpMessageHandler(Timeout.InfiniteTimeSpan);
        using var client = new HttpClient(handler) { BaseAddress = new(BaseAddress) };
        var api = RestService.For<ITimeoutApi>(client);

        await Assert.That(() => (Task)api.GetStringShortTimeout()).Throws<OperationCanceledException>();
    }

    /// <summary>Source-generated: a void method that responds under its timeout completes normally.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SourceGenVoidTimeoutAllowsFastResponse()
    {
        using var handler = new DelayingHttpMessageHandler(TimeSpan.Zero);
        using var client = new HttpClient(handler) { BaseAddress = new(BaseAddress) };
        var api = RestService.For<ITimeoutApi>(client);

        await api.GetVoidLongTimeout();

        await Assert.That(handler.RequestCount).IsEqualTo(1);
    }

    /// <summary>Source-generated: a value-returning method that responds under its timeout returns its content.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SourceGenValueTimeoutAllowsFastResponse()
    {
        using var handler = new DelayingHttpMessageHandler(TimeSpan.Zero);
        using var client = new HttpClient(handler) { BaseAddress = new(BaseAddress) };
        var api = RestService.For<ITimeoutApi>(client);

        var result = await api.GetStringLongTimeout();

        await Assert.That(result).IsEqualTo("ok");
    }

    /// <summary>Source-generated: a caller cancellation token still cancels a request that also declares a timeout.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SourceGenCallerCancellationCancelsAlongsideTimeout()
    {
        using var handler = new DelayingHttpMessageHandler(Timeout.InfiniteTimeSpan);
        using var client = new HttpClient(handler) { BaseAddress = new(BaseAddress) };
        var api = RestService.For<ITimeoutApi>(client);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert
            .That(() => (Task)api.GetStringLongTimeoutCancellable(cts.Token))
            .Throws<OperationCanceledException>();
    }

    /// <summary>Reflection: a void method whose response outlasts its short timeout is canceled.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ReflectionVoidTimeoutCancelsSlowResponse()
    {
        var fixture = new RequestBuilderImplementation<ITimeoutApi>();
        var factory = fixture.BuildRestResultFuncForMethod(nameof(ITimeoutApi.GetVoidShortTimeout));
        using var handler = new DelayingHttpMessageHandler(Timeout.InfiniteTimeSpan);
        using var client = new HttpClient(handler) { BaseAddress = new(BaseAddress) };

        await Assert.That(() => (Task)factory(client, [])!).Throws<OperationCanceledException>();
    }

    /// <summary>Reflection: a value-returning method whose response outlasts its short timeout is canceled.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ReflectionValueTimeoutCancelsSlowResponse()
    {
        var fixture = new RequestBuilderImplementation<ITimeoutApi>();
        var factory = fixture.BuildRestResultFuncForMethod(nameof(ITimeoutApi.GetStringShortTimeout));
        using var handler = new DelayingHttpMessageHandler(Timeout.InfiniteTimeSpan);
        using var client = new HttpClient(handler) { BaseAddress = new(BaseAddress) };

        await Assert.That(() => (Task)factory(client, [])!).Throws<OperationCanceledException>();
    }

    /// <summary>Reflection: a value-returning method that responds under its timeout returns its content.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ReflectionValueTimeoutAllowsFastResponse()
    {
        var fixture = new RequestBuilderImplementation<ITimeoutApi>();
        var factory = fixture.BuildRestResultFuncForMethod(nameof(ITimeoutApi.GetStringLongTimeout));
        using var handler = new DelayingHttpMessageHandler(TimeSpan.Zero);
        using var client = new HttpClient(handler) { BaseAddress = new(BaseAddress) };

        var result = await (Task<string?>)factory(client, [])!;

        await Assert.That(result).IsEqualTo("ok");
    }

    /// <summary>Reflection: a caller cancellation token still cancels a request that also declares a timeout.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ReflectionCallerCancellationCancelsAlongsideTimeout()
    {
        var fixture = new RequestBuilderImplementation<ITimeoutApi>();
        var factory = fixture.BuildRestResultFuncForMethod(nameof(ITimeoutApi.GetStringLongTimeoutCancellable));
        using var handler = new DelayingHttpMessageHandler(Timeout.InfiniteTimeSpan);
        using var client = new HttpClient(handler) { BaseAddress = new(BaseAddress) };
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.That(() => (Task)factory(client, [cts.Token])!).Throws<OperationCanceledException>();
    }
}
