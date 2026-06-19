// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A Refit service accepting nullable custom reference and value parameters.</summary>
public interface ICustomReferenceAndValueParametersService
{
    /// <summary>Sends a request with nullable custom parameters.</summary>
    /// <param name="reference">A nullable <see cref="CustomReferenceType"/> argument.</param>
    /// <param name="value">A nullable <see cref="CustomValueType"/> argument.</param>
    /// <returns>A task representing the request.</returns>
    [Get("/")]
    Task Get(CustomReferenceType? reference, CustomValueType? value);
}
