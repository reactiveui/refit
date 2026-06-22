// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.IO;
using System.Net.Http;

namespace Refit.Tests;

/// <summary>Tests for <see cref="JsonLinesContent"/> and the generated JSON Lines body factory.</summary>
public class JsonLinesContentTests
{
    /// <summary>Verifies the constructor rejects a null item sequence.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ConstructorRejectsNullItems() =>
        await Assert
            .That(() => new JsonLinesContent(null!, new SystemTextJsonContentSerializer()))
            .ThrowsExactly<ArgumentNullException>();

    /// <summary>Verifies the constructor rejects a null serializer.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ConstructorRejectsNullSerializer() =>
        await Assert
            .That(() => new JsonLinesContent(Array.Empty<int>(), null!))
            .ThrowsExactly<ArgumentNullException>();

    /// <summary>Verifies an HttpContent body is passed through unchanged.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CreateJsonLinesBodyContentPassesThroughHttpContent()
    {
        using var inner = new StringContent("passthrough");
        var content = GeneratedRequestRunner.CreateJsonLinesBodyContent(new RefitSettings(), inner);
        await Assert.That(content).IsSameReferenceAs(inner);
    }

    /// <summary>Verifies a stream body is wrapped in stream content.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CreateJsonLinesBodyContentWrapsStream()
    {
        await using var stream = new MemoryStream([1, 2, 3]);
        var content = GeneratedRequestRunner.CreateJsonLinesBodyContent(new RefitSettings(), stream);
        await Assert.That(content).IsTypeOf<StreamContent>();
    }

    /// <summary>Verifies a single non-enumerable body becomes a one-line JSON Lines content.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CreateJsonLinesBodyContentWrapsSingleValue()
    {
        var content = GeneratedRequestRunner.CreateJsonLinesBodyContent(
            new RefitSettings(),
            new JsonLineRecord { Id = "1", Name = "single" });

        await Assert.That(content).IsTypeOf<JsonLinesContent>();
        var body = await content.ReadAsStringAsync();
        await Assert.That(body).IsEqualTo("{\"id\":\"1\",\"name\":\"single\"}");
    }
}
