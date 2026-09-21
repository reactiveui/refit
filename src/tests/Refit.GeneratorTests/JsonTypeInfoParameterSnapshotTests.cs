// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.GeneratorTests;

/// <summary>Snapshot tests for the source emitted for methods that take <c>JsonTypeInfo&lt;T&gt;</c> metadata as a parameter.</summary>
public class JsonTypeInfoParameterSnapshotTests
{
    /// <summary>Verifies a body written with one piece of metadata and a reply read with another.</summary>
    /// <returns>A task representing the asynchronous verification.</returns>
    [Test]
    public Task BodyAndReplyMetadata() =>
        Fixture.VerifyForBody(
            """
            public sealed class Shipment
            {
                public string? Id { get; set; }
            }

            public sealed class Label
            {
                public string? Url { get; set; }
            }

            [Post("/shipments/{carrier}")]
            Task<Label> Ship(
                string carrier,
                [Body] Shipment shipment,
                System.Text.Json.Serialization.Metadata.JsonTypeInfo<Shipment> shipmentInfo,
                System.Text.Json.Serialization.Metadata.JsonTypeInfo<Label> labelInfo);
            """);

    /// <summary>Verifies metadata that reads each element of a streamed reply.</summary>
    /// <returns>A task representing the asynchronous verification.</returns>
    [Test]
    public Task StreamedReplyMetadata() =>
        Fixture.VerifyForBody(
            """
            public sealed class Position
            {
                public double Latitude { get; set; }
            }

            [Get("/positions")]
            IAsyncEnumerable<Position> Track(System.Text.Json.Serialization.Metadata.JsonTypeInfo<Position> positionInfo, CancellationToken cancellationToken);
            """);
}
