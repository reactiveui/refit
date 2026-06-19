// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A Refit service whose synchronous result is a nullable reference type.</summary>
public interface INullableReferenceService
{
    /// <summary>Gets a nullable reference value.</summary>
    /// <returns>A nullable string.</returns>
    [Get("/")]
    string? Get();
}
