// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A Refit service whose result is a nullable custom value type.</summary>
public interface ICustomNullableValueService
{
    /// <summary>Gets a nullable custom value.</summary>
    /// <returns>A nullable <see cref="CustomValueType"/>.</returns>
    [Get("/")]
    CustomValueType? Get();
}
