// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Reflection.Benchmarks;

/// <summary>A sample return-wrapper type surfaced by the return-type adapters under benchmark.</summary>
/// <typeparam name="T">The wrapped result type.</typeparam>
public sealed class ReflectionResult<T>
{
    /// <summary>Gets or sets the wrapped value.</summary>
    public T? Value { get; set; }
}
