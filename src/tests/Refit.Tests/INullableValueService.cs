// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A Refit service whose synchronous result is a nullable value type.</summary>
public interface INullableValueService
{
    /// <summary>Gets a nullable value-type value.</summary>
    /// <returns>A nullable integer.</returns>
    [Get("/")]
    int? Get();
}
