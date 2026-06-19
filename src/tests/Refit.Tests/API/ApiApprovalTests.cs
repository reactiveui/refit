// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if !NET48
using System.Diagnostics.CodeAnalysis;

namespace Refit.Tests.API;

/// <summary>Checks to make sure that the API is consistent with previous releases, and new API changes are highlighted.</summary>
[ExcludeFromCodeCoverage]
public class ApiApprovalTests
{
    /// <summary>Generates public API for the ReactiveUI API.</summary>
    /// <returns>A task to monitor the process.</returns>
    [Test]
    public Task Refit() => typeof(ApiResponse).Assembly.CheckApproval(["Refit"]);
}
#endif
