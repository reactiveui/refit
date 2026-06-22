// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>Shared helpers for the Newtonsoft-flavored integration tests in this assembly.</summary>
public partial class RestServiceIntegrationTests
{
    /// <summary>Asserts that a captured stack trace contains the expected member name.</summary>
    /// <param name="expectedSubstring">The substring expected within the stack trace.</param>
    /// <param name="actualString">The captured stack trace text.</param>
    /// <returns>A task representing the asynchronous assertion.</returns>
    private static async Task AssertStackTraceContains(string expectedSubstring, string? actualString) =>
        await Assert.That(actualString).Contains(expectedSubstring);
}
