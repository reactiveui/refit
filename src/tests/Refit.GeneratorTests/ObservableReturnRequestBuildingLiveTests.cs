// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;

namespace Refit.GeneratorTests;

/// <summary>
/// Compiles, loads and subscribes generated <see cref="IObservable{T}"/> request building, asserting the cold
/// per-subscription semantics and parity with the reflection request builder.
/// </summary>
public sealed class ObservableReturnRequestBuildingLiveTests
{
    /// <summary>The request count expected after subscribing to the cold observable a second time.</summary>
    private const int TwoSubscriptionRequestCount = 2;

    /// <summary>Verifies a cold generated observable rebuilds and re-sends per subscription, never reusing a disposed
    /// request, and that the request it sends matches the reflection request builder.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [RequiresUnreferencedCode("Loads a generated assembly and reflects over generated types and members.")]
    [RequiresDynamicCode("Compares generated request building against the reflection request builder.")]
    public async Task ColdObservableResendsPerSubscriptionAndMatchesReflection()
    {
        using var harness = ObservableHarness.Create();

        var observable = harness.InvokeGenerated("Watch", ["42", "hello"]);

        // Cold: no request is sent until the first subscription.
        await Assert.That(harness.RequestCount).IsEqualTo(0);

        var first = await harness.SubscribeAndCaptureAsync(observable);
        await Assert.That(harness.RequestCount).IsEqualTo(1);

        // A second subscription rebuilds and re-sends over a fresh request instead of throwing on a disposed one.
        var second = await harness.SubscribeAndCaptureAsync(observable);
        await Assert.That(harness.RequestCount).IsEqualTo(TwoSubscriptionRequestCount);
        await Assert.That(second.RequestUri!.AbsoluteUri).IsEqualTo(first.RequestUri!.AbsoluteUri);

        var reflection = await harness.SubscribeReflectionAsync("Watch", ["42", "hello"]);
        await Assert.That(first.Method).IsEqualTo(reflection.Method);
        await Assert.That(first.RequestUri!.AbsoluteUri).IsEqualTo(reflection.RequestUri!.AbsoluteUri);
    }

    /// <summary>Hosts one compiled generated observable client plus the reflection builder for parity assertions.</summary>
    /// <param name="context">The collectible load context holding the compiled assembly.</param>
    /// <param name="handler">The capturing message handler.</param>
    /// <param name="client">The HTTP client shared by both request paths.</param>
    /// <param name="interfaceType">The compiled Refit interface type.</param>
    /// <param name="generatedApi">The generated client instance.</param>
    /// <param name="requestBuilder">The reflection request builder for the compiled interface.</param>
    private sealed class ObservableHarness(
        CollectibleAssemblyLoadContext context,
        CapturingHandler handler,
        HttpClient client,
        Type interfaceType,
        object generatedApi,
        IRequestBuilder requestBuilder) : IDisposable
    {
        /// <summary>The base address the relative request URIs resolve against.</summary>
        private const string BaseAddress = "https://example.test/base/";

        /// <summary>The interface source compiled through the generator for every scenario.</summary>
        private const string ApiSource =
            """
            using System;
            using Refit;

            namespace Refit.LiveObservable;

            public interface IObservableApi
            {
                [Get("/items/{id}")]
                IObservable<string> Watch(string id, string q);
            }
            """;

        /// <summary>Gets the number of requests sent through the handler so far.</summary>
        public int RequestCount => handler.RequestCount;

        /// <summary>Compiles the scenario interface and creates the generated and reflection clients.</summary>
        /// <returns>The live harness.</returns>
        [RequiresUnreferencedCode("Loads a generated assembly and reflects over generated types and members.")]
        public static ObservableHarness Create()
        {
            var result = Fixture.RunGenerator(ApiSource, generatedRequestBuilding: true);
            if (!result.CompilesWithoutErrors)
            {
                throw new InvalidOperationException(
                    $"Generated compilation failed: {string.Join(Environment.NewLine, result.CompilationErrors)}");
            }

            var (assembly, loadContext) = Fixture.EmitAndLoad(result);
            var interfaceType = assembly.GetType("Refit.LiveObservable.IObservableApi", throwOnError: true)!;
            var generatedType = assembly
                .GetTypes()
                .Single(type => type.IsClass && interfaceType.IsAssignableFrom(type));

            var handler = new CapturingHandler();
            var client = new HttpClient(handler) { BaseAddress = new(BaseAddress) };
            var requestBuilder = RequestBuilder.ForType(interfaceType, new RefitSettings());
            var generatedApi = Activator.CreateInstance(generatedType, [client, requestBuilder])!;
            return new(loadContext, handler, client, interfaceType, generatedApi, requestBuilder);
        }

        /// <summary>Invokes a method on the generated client, returning the cold observable it produces.</summary>
        /// <param name="methodName">The interface method name.</param>
        /// <param name="args">The argument values.</param>
        /// <returns>The observable returned by the generated method.</returns>
        [RequiresUnreferencedCode("Reflects over generated types and members.")]
        public object InvokeGenerated(string methodName, object?[] args) =>
            interfaceType.GetMethod(methodName)!.Invoke(generatedApi, args)!;

        /// <summary>Subscribes to an observable, awaits its first notification and returns the captured request.</summary>
        /// <param name="observable">The observable to subscribe to.</param>
        /// <returns>The request captured for the subscription.</returns>
        public async Task<HttpRequestMessage> SubscribeAndCaptureAsync(object observable)
        {
            var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (((IObservable<object?>)observable).Subscribe(new AwaitingObserver(completion)))
            {
                _ = await completion.Task.ConfigureAwait(false);
            }

            return handler.TakeLastRequest();
        }

        /// <summary>Builds and subscribes the reflection observable for the same method, returning its request.</summary>
        /// <param name="methodName">The interface method name.</param>
        /// <param name="args">The argument values.</param>
        /// <returns>The request captured for the reflection subscription.</returns>
        [RequiresUnreferencedCode("Reflects over generated types and members.")]
        [RequiresDynamicCode("Builds the reflection request delegate for parity comparison.")]
        public Task<HttpRequestMessage> SubscribeReflectionAsync(string methodName, object?[] args)
        {
            var reflectionFunc = requestBuilder.BuildRestResultFuncForMethod(methodName);
            return SubscribeAndCaptureAsync(reflectionFunc(client, args!)!);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            client.Dispose();
            handler.Dispose();
            context.Dispose();
        }
    }

    /// <summary>Completes a task from the first notification of a single-value observable.</summary>
    /// <param name="completion">The completion source signalled by the first notification.</param>
    private sealed class AwaitingObserver(TaskCompletionSource<object?> completion) : IObserver<object?>
    {
        /// <inheritdoc/>
        public void OnCompleted() => completion.TrySetResult(null);

        /// <inheritdoc/>
        public void OnError(Exception error) => completion.TrySetException(error);

        /// <inheritdoc/>
        public void OnNext(object? value) => completion.TrySetResult(value);
    }

    /// <summary>Captures every outgoing request and returns a fixed JSON string response.</summary>
    private sealed class CapturingHandler : HttpMessageHandler
    {
        /// <summary>The last request sent through the handler.</summary>
        private HttpRequestMessage? _lastRequest;

        /// <summary>Gets the number of requests sent through the handler so far.</summary>
        public int RequestCount { get; private set; }

        /// <summary>Takes the last captured request, clearing the slot.</summary>
        /// <returns>The captured request.</returns>
        public HttpRequestMessage TakeLastRequest()
        {
            var request = _lastRequest ?? throw new InvalidOperationException("No request was captured.");
            _lastRequest = null;
            return request;
        }

        /// <inheritdoc/>
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            _lastRequest = request;
            RequestCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("\"done\"", Encoding.UTF8, "application/json")
            });
        }
    }
}
