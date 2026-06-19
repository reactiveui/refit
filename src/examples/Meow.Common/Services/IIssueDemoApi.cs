// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Refit;

namespace Meow;

/// <summary>Refit API used by the issue demo.</summary>
public interface IIssueDemoApi
{
    /// <summary>Echoes the customer id supplied as a request property.</summary>
    /// <param name="customerId">The customer id to echo.</param>
    /// <returns>The echoed customer response.</returns>
    [Get("/echo-customer")]
    Task<CustomerEchoResponse> EchoCustomerAsync([Property("CustomerId")] int customerId);

    /// <summary>Gets a large payload containing the requested number of items.</summary>
    /// <param name="size">The number of items to request.</param>
    /// <returns>The large payload response.</returns>
    [Get("/large-payload")]
    Task<LargePayloadResponse> GetLargePayloadAsync([AliasAs("size")] int size);
}
