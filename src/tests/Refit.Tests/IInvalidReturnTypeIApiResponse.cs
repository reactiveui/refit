// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;

namespace Refit.Tests;

/// <summary>Fixture whose method returns a raw IApiResponse, used to verify Refit rejects it.</summary>
public interface IInvalidReturnTypeIApiResponse
{
    /// <summary>Endpoint with an unsupported non-generic IApiResponse return type.</summary>
    /// <returns>The API response.</returns>
    [Get("/")]
    [SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "Refit endpoint method fixture referenced by name in RequestBuilder tests; must remain a method.")]
    IApiResponse GetValue();
}
