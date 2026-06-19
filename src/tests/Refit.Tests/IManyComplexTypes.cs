// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Tests;

/// <summary>Fixture with multiple complex-type parameters and no body marker, used to verify Refit rejects it.</summary>
public interface IManyComplexTypes
{
    /// <summary>Endpoint declaring two complex-type parameters.</summary>
    /// <param name="body0">First complex body.</param>
    /// <param name="body1">Second complex body.</param>
    /// <returns>The response body.</returns>
    [Post("/")]
    Task<string> PostValue(UserBody body0, UserBody body1);
}
