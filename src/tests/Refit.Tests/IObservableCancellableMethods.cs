// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>API surface exercising observable returns with cancellation.</summary>
public interface IObservableCancellableMethods
{
    /// <summary>Gets a resource as an observable supporting cancellation.</summary>
    /// <param name="value">The value segment included in the request URL.</param>
    /// <param name="token">The cancellation token for the request.</param>
    /// <returns>An observable that produces the resource value.</returns>
    [Get("/value/{value}")]
    IObservable<string> GetWithCancellation(string value, CancellationToken token);
}
