// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Meow;

/// <summary>Delegating handler that copies a per-request customer id option into a request header.</summary>
/// <param name="innerHandler">The inner handler to delegate to.</param>
public sealed class CustomerIdHeaderHandler(HttpMessageHandler innerHandler) : DelegatingHandler(innerHandler)
{
    /// <summary>The request options key used to carry the customer id.</summary>
    private static readonly HttpRequestOptionsKey<object?> _customerIdKey = new("CustomerId");

    /// <inheritdoc/>
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.Options.TryGetValue(_customerIdKey, out var customerId) && customerId is not null)
        {
            request.Headers.Remove("CustomerId");
            request.Headers.TryAddWithoutValidation("CustomerId", customerId.ToString());
        }

        return base.SendAsync(request, cancellationToken);
    }
}
