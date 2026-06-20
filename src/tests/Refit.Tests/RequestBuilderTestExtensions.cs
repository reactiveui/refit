// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Tests;

/// <summary>Test helpers for building and running requests through an <see cref="IRequestBuilder"/>.</summary>
internal static class RequestBuilderTestExtensions
{
    /// <summary>Request-building helpers on <see cref="IRequestBuilder"/> for tests.</summary>
    /// <param name="builder">The request builder under test.</param>
    extension(IRequestBuilder builder)
    {
        /// <summary>Builds a factory that produces the request message for a method.</summary>
        /// <param name="methodName">The name of the interface method to build a request for.</param>
        /// <param name="baseAddress">The base address used by the test HTTP client.</param>
        /// <returns>A factory that maps a parameter array to the produced request message.</returns>
        public Func<object[], HttpRequestMessage> BuildRequestFactoryForMethod(
            string methodName,
            string baseAddress = "http://api/")
        {
            var factory = builder.BuildRestResultFuncForMethod(methodName);
            var testHttpMessageHandler = new TestHttpMessageHandler();

            return paramList =>
            {
                var task = (Task)factory(
                    new(testHttpMessageHandler) { BaseAddress = new(baseAddress) },
                    paramList)!;
                task.Wait();
                return testHttpMessageHandler.RequestMessage!;
            };
        }

        /// <summary>Builds a factory that runs a request and returns the handler that observed it.</summary>
        /// <param name="methodName">The name of the interface method to invoke.</param>
        /// <param name="returnContent">Optional response content the handler returns.</param>
        /// <param name="baseAddress">The base address used by the test HTTP client.</param>
        /// <returns>A factory that maps a parameter array to the handler that observed the request.</returns>
        public Func<object[], TestHttpMessageHandler> RunRequest(
            string methodName,
            string? returnContent = null,
            string baseAddress = "http://api/")
        {
            var factory = builder.BuildRestResultFuncForMethod(methodName);
            var testHttpMessageHandler = new TestHttpMessageHandler();
            if (returnContent is not null)
            {
                testHttpMessageHandler.Content = new StringContent(returnContent);
            }

            return paramList =>
            {
                var task = (Task)factory(
                    new(testHttpMessageHandler) { BaseAddress = new(baseAddress) },
                    paramList)!;
                try
                {
                    task.Wait();
                }
                catch (AggregateException e) when (e.InnerException is TaskCanceledException) { }

                return testHttpMessageHandler;
            };
        }
    }
}
