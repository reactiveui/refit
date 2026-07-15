// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.GeneratorTests;

/// <summary>Generator tests for the per-call <c>[Timeout]</c> attribute emission.</summary>
public partial class GeneratedRequestBuildingTests
{
    /// <summary>The generated call that stashes a method's per-call timeout on the request.</summary>
    private const string SetRequestTimeoutCall = "GeneratedRequestRunner.SetRequestTimeout(refitRequest, 50)";

    /// <summary>An eligible GET method that declares a per-call timeout.</summary>
    private const string TimeoutGetMethod =
        """
        [Get("/users")]
        [Timeout(50)]
        Task<string> Get(CancellationToken cancellationToken);
        """;

    /// <summary>Verifies a method with <c>[Timeout]</c> emits the timeout stash before sending.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task TimeoutAttributeEmitsSetRequestTimeout()
    {
        var generated = Fixture.GenerateForBody(
            TimeoutGetMethod,
            GeneratedClientHintName);

        await Assert.That(generated).Contains(SetRequestTimeoutCall);
        await Assert.That(generated).Contains(GeneratedRequestRunnerSendAsync);
    }

    /// <summary>Verifies a method without <c>[Timeout]</c> emits no timeout stash.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task WithoutTimeoutAttributeEmitsNoSetRequestTimeout()
    {
        var generated = Fixture.GenerateForBody(
            SimpleGetMethod,
            GeneratedClientHintName);

        await Assert.That(generated).DoesNotContain("SetRequestTimeout");
    }
}
