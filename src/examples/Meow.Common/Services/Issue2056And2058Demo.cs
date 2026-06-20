// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Refit;

namespace Meow;

/// <summary>Demonstrates and validates the fixes for issues 2056 and 2058.</summary>
public static class Issue2056And2058Demo
{
    /// <summary>The number of items requested in the large payload validation.</summary>
    private const int LargePayloadItemCount = 2000;

    /// <summary>Runs both issue validations against an in-memory backend.</summary>
    /// <returns>A task that completes when both validations have run.</returns>
    public static async Task RunAsync()
    {
        using var httpClient = new HttpClient(
            new CustomerIdHeaderHandler(new DemoBackendHandler())) { BaseAddress = new("https://demo.local") };

        var api = RestService.For<IIssueDemoApi>(
            httpClient,
            new RefitSettings { ContentSerializer = new NewtonsoftJsonContentSerializer() });

        await ValidateIssue2056Async(api).ConfigureAwait(false);
        await ValidateIssue2058Async(api).ConfigureAwait(false);
    }

    /// <summary>Validates that per-request customer id headers are not shared across concurrent requests.</summary>
    /// <param name="api">The demo API client.</param>
    /// <returns>A task that completes when the validation has finished.</returns>
    private static async Task ValidateIssue2056Async(IIssueDemoApi api)
    {
        const int firstCustomerId = 1000;
        const int customerCount = 50;

        var requests = new Task<(int Expected, string? Actual)>[customerCount];
        for (var i = 0; i < requests.Length; i++)
        {
            var customerId = firstCustomerId + i;
            requests[i] = EchoCustomerAsync(api, customerId);
        }

        var responses = await Task.WhenAll(requests).ConfigureAwait(false);
        var mismatchCount = 0;
        for (var i = 0; i < responses.Length; i++)
        {
            if (responses[i].Expected.ToString() != responses[i].Actual)
            {
                mismatchCount++;
            }
        }

        if (mismatchCount == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Issue #2056 check failed. Found {mismatchCount} mismatched CustomerId headers.");

        static async Task<(int Expected, string? Actual)> EchoCustomerAsync(
            IIssueDemoApi api,
            int customerId)
        {
            var echo = await api.EchoCustomerAsync(customerId).ConfigureAwait(false);
            return (customerId, echo.CustomerIdHeader);
        }
    }

    /// <summary>Validates that a large async-only payload is fully read and deserialized.</summary>
    /// <param name="api">The demo API client.</param>
    /// <returns>A task that completes when the validation has finished.</returns>
    private static async Task ValidateIssue2058Async(IIssueDemoApi api)
    {
        var payload = await api.GetLargePayloadAsync(LargePayloadItemCount).ConfigureAwait(false);
        if (payload.Items.Count == LargePayloadItemCount)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Issue #2058 check failed. Expected {LargePayloadItemCount} items but got {payload.Items.Count}.");
    }
}
