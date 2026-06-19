// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A Refit interface returning a value type, used to verify value-type responses still work.</summary>
public interface IBrokenWebApi
{
    /// <summary>Posts a value and returns a boolean response.</summary>
    /// <param name="derp">The request body.</param>
    /// <returns>The boolean response.</returns>
    [Post("/what-spec")]
    Task<bool> PostAValue([Body] string derp);
}
