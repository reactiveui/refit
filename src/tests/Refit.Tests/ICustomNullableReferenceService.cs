// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A Refit service whose result is a nullable custom reference type.</summary>
public interface ICustomNullableReferenceService
{
    /// <summary>Gets a nullable custom reference value.</summary>
    /// <returns>A nullable <see cref="CustomReferenceType"/>.</returns>
    [Get("/")]
    CustomReferenceType? Get();
}
