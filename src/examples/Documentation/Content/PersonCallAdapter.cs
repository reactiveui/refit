// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation.Content;

/// <summary>Supplies a generic adapter discovered by the Refit source generator.</summary>
/// <typeparam name="T">The response body wrapped by the deferred call.</typeparam>
public sealed class PersonCallAdapter<T> : IReturnTypeAdapter<PersonCall<T>, T>
{
    /// <summary>Retains the deferred call without starting it.</summary>
    /// <param name="invoke">The generated HTTP invocation.</param>
    /// <returns>A wrapper that the caller invokes once.</returns>
    public PersonCall<T> Adapt(Func<CancellationToken, Task<T>> invoke) => new(invoke);
}
