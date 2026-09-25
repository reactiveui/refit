// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.GeneratorTests;

/// <summary>Snapshot tests for the source emitted for a JSON Lines body declared as a synchronous or asynchronous sequence.</summary>
public class JsonLinesBodySnapshotTests
{
    /// <summary>Verifies the source emitted for an asynchronous JSON Lines body.</summary>
    /// <returns>A task representing the asynchronous verification.</returns>
    [Test]
    public Task AsyncSequenceBody() =>
        Fixture.VerifyForBody(
            """
            public sealed class LogEntry
            {
                public string? Message { get; set; }
            }

            [Post("/logs")]
            Task Upload([Body(BodySerializationMethod.JsonLines)] IAsyncEnumerable<LogEntry> entries, CancellationToken cancellationToken);
            """);

    /// <summary>Verifies the source emitted for a synchronous JSON Lines body whose element type is sealed.</summary>
    /// <returns>A task representing the asynchronous verification.</returns>
    [Test]
    public Task TypedSynchronousSequenceBody() =>
        Fixture.VerifyForBody(
            """
            public sealed class LogEntry
            {
                public string? Message { get; set; }
            }

            [Post("/logs")]
            Task Upload([Body(BodySerializationMethod.JsonLines)] IEnumerable<LogEntry> entries);
            """);
}
