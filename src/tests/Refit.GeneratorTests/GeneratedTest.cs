// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.GeneratorTests;

/// <summary>Generator tests covering which files are emitted for given inputs.</summary>
public class GeneratedTest
{
    /// <summary>Verifies all expected files are emitted for a valid Refit interface.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task ShouldEmitAllFiles() =>
        Fixture.VerifyForBody(
            """
            [Get("/users")]
            Task<string> Get();
            """, false);

    /// <summary>Verifies no files are emitted when there are no valid Refit interfaces.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task ShouldNotEmitFilesWhenNoRefitInterfaces() =>

        // Refit should not generate any code when no valid Refit interfaces are present.
        Fixture.VerifyForBody(string.Empty, false);
}
