// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Refit.Generator;

namespace Refit.GeneratorTests;

/// <summary>Verifies the body model reports a difference in any single member, which incremental caching relies on.</summary>
public sealed class RequestParameterBodyModelEqualityTests
{
    /// <summary>The model every case is compared against.</summary>
    private static readonly RequestParameterBodyModel Baseline = new(
        "Serialize",
        BodyBufferMode.Settings,
        null,
        "gzip",
        "fast",
        new MultipartPartModel(default, "field", "file", false),
        "Item",
        false);

    /// <summary>Verifies two models with the same members are equal and hash alike.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task EqualModelsAreEqualAndHashAlike()
    {
        var copy = Baseline with { };

        await Assert.That(copy.Equals(Baseline)).IsTrue();
        await Assert.That(copy.GetHashCode()).IsEqualTo(Baseline.GetHashCode());
    }

    /// <summary>Verifies a change to the serialization method makes the models differ.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SerializationMethodDifferenceIsDetected() =>
        await Assert.That(Baseline.Equals(Baseline with { BodySerializationMethod = "Other" })).IsFalse();

    /// <summary>Verifies a change to the buffer mode makes the models differ.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task BufferModeDifferenceIsDetected() =>
        await Assert.That(Baseline.Equals(Baseline with { BodyBufferMode = BodyBufferMode.None })).IsFalse();

    /// <summary>Verifies a change to the form fields makes the models differ.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task FormFieldsDifferenceIsDetected() =>
        await Assert.That(Baseline.Equals(Baseline with { FormFields = [] })).IsFalse();

    /// <summary>Verifies a change to the compression makes the models differ.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task CompressionDifferenceIsDetected() =>
        await Assert.That(Baseline.Equals(Baseline with { Compression = "br" })).IsFalse();

    /// <summary>Verifies a change to the compression level makes the models differ.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task CompressionLevelDifferenceIsDetected() =>
        await Assert.That(Baseline.Equals(Baseline with { CompressionLevel = "best" })).IsFalse();

    /// <summary>Verifies a change to the multipart part makes the models differ.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task MultipartPartDifferenceIsDetected() =>
        await Assert.That(Baseline.Equals(Baseline with { MultipartPart = null })).IsFalse();

    /// <summary>Verifies a change to the JSON Lines element type makes the models differ.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task JsonLinesElementTypeDifferenceIsDetected() =>
        await Assert.That(Baseline.Equals(Baseline with { JsonLinesElementType = "Other" })).IsFalse();

    /// <summary>Verifies a change to the async JSON Lines flag makes the models differ.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task AsyncJsonLinesDifferenceIsDetected() =>
        await Assert.That(Baseline.Equals(Baseline with { IsAsyncJsonLines = true })).IsFalse();
}
