// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System;
using System.IO;

namespace Refit.Tests;

/// <summary>Tests that multipart item content types reject a null payload value.</summary>
public class MultipartItemArgumentValidationTests
{
    /// <summary>Verifies a byte-array part rejects a null value.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ByteArrayPartRejectsNullValue() =>
        await Assert.That(static () => new ByteArrayPart(null!, "file.bin"))
            .ThrowsExactly<ArgumentNullException>();

    /// <summary>Verifies a stream part rejects a null value.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task StreamPartRejectsNullValue() =>
        await Assert.That(static () => new StreamPart((Stream)null!, "file.bin"))
            .ThrowsExactly<ArgumentNullException>();
}
