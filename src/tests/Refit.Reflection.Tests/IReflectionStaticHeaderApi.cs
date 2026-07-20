// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Reflection.Tests;

/// <summary>An API carrying class-level static headers, with one method that only emits them and another that also carries a
/// dynamic <see cref="HeaderAttribute"/> parameter overriding one of them, pinning that the shared static headers are never
/// mutated across calls.</summary>
[Headers("X-Static: static-value", "X-Override: original")]
public interface IReflectionStaticHeaderApi
{
    /// <summary>Emits only the class-level static headers.</summary>
    /// <returns>The response body.</returns>
    [Get("/static-only")]
    Task<string> StaticOnly();

    /// <summary>Emits the static headers while overriding one of them from a dynamic header parameter.</summary>
    /// <param name="overrideValue">The value overriding the static <c>X-Override</c> header.</param>
    /// <returns>The response body.</returns>
    [Get("/with-dynamic")]
    Task<string> WithDynamic([Header("X-Override")] string overrideValue);
}
