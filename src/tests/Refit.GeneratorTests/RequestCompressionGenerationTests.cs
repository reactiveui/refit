// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.GeneratorTests;

/// <summary>Generator tests for the request body content coding a <c>[Body]</c> parameter declares.</summary>
public class RequestCompressionGenerationTests
{
    /// <summary>The generated client hint name the compression fixtures assert on.</summary>
    private const string GeneratedClientHintName = "IGeneratedClient.g.cs";

    /// <summary>The emitted coding argument for a body that declared none, which defers to the settings.</summary>
    private const string DefaultCodingArgument = "global::Refit.RequestCompression.Default";

    /// <summary>The emitted level argument for a body that declared none.</summary>
    private const string OptimalLevelArgument = "global::System.IO.Compression.CompressionLevel.Optimal";

    /// <summary>The generated call that applies the coding.</summary>
    private const string CompressCall = "GeneratedRequestRunner.CompressBodyContent";

    /// <summary>Verifies a declared coding and level are emitted as the members the attribute named.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DeclaredCodingAndLevelAreEmitted()
    {
        var generated = Fixture.GenerateForDeclaration(
            """
            public interface IGeneratedClient
            {
                [Post("/users")]
                Task<string> Post([Body(Compression = RequestCompression.Brotli, CompressionLevel = System.IO.Compression.CompressionLevel.SmallestSize)] string body);
            }
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains(CompressCall);
        await Assert.That(generated).Contains("global::Refit.RequestCompression.Brotli");
        await Assert.That(generated).Contains("global::System.IO.Compression.CompressionLevel.SmallestSize");
    }

    /// <summary>Verifies each coding the attribute can name reaches the generated call.</summary>
    /// <param name="declared">The member named on the attribute.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments("GZip")]
    [Arguments("Brotli")]
    [Arguments("Zstandard")]
    public async Task EveryDeclaredCodingIsEmitted(string declared)
    {
        var generated = Fixture.GenerateForDeclaration(
            $$"""
            public interface IGeneratedClient
            {
                [Post("/users")]
                Task<string> Post([Body(Compression = RequestCompression.{{declared}})] string body);
            }
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains($"global::Refit.RequestCompression.{declared}");
    }

    /// <summary>Verifies each level the attribute can name reaches the generated call.</summary>
    /// <param name="declared">The member named on the attribute.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments("Optimal")]
    [Arguments("Fastest")]
    [Arguments("NoCompression")]
    [Arguments("SmallestSize")]
    public async Task EveryDeclaredLevelIsEmitted(string declared)
    {
        var generated = Fixture.GenerateForDeclaration(
            $$"""
            public interface IGeneratedClient
            {
                [Post("/users")]
                Task<string> Post([Body(Compression = RequestCompression.GZip, CompressionLevel = System.IO.Compression.CompressionLevel.{{declared}})] string body);
            }
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains($"global::System.IO.Compression.CompressionLevel.{declared}");
    }

    /// <summary>Verifies a body declaring no coding still emits the call, so the settings can supply one at request time.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UndeclaredCodingEmitsTheCallSoSettingsStillApply()
    {
        var generated = Fixture.GenerateForDeclaration(
            """
            public interface IGeneratedClient
            {
                [Post("/users")]
                Task<string> Post([Body] string body);
            }
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains(CompressCall);
        await Assert.That(generated).Contains(DefaultCodingArgument);
        await Assert.That(generated).Contains(OptimalLevelArgument);
    }

    /// <summary>Verifies a body declaring <c>None</c> emits no call at all, since no coding can ever apply to it.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DeclaredNoneEmitsNoCall()
    {
        var generated = Fixture.GenerateForDeclaration(
            """
            public interface IGeneratedClient
            {
                [Post("/users")]
                Task<string> Post([Body(Compression = RequestCompression.None)] string body);
            }
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).DoesNotContain(CompressCall);
    }

    /// <summary>Verifies a named argument the compiler already rejected leaves the defaults in place instead of
    /// crashing the generator or emitting a member name read from a value that is not there.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AnErroneousNamedArgumentFallsBackToTheDefaults()
    {
        var generated = Fixture.GenerateForDeclaration(
            """
            public interface IGeneratedClient
            {
                [Post("/users")]
                Task<string> Post([Body(Compression = "not an enum", CompressionLevel = "not an enum")] string body);
            }
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains(DefaultCodingArgument);
        await Assert.That(generated).Contains(OptimalLevelArgument);
    }

    /// <summary>Verifies a level declared without a coding still reaches the generated call.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ALevelDeclaredWithoutACodingIsStillEmitted()
    {
        var generated = Fixture.GenerateForDeclaration(
            """
            public interface IGeneratedClient
            {
                [Post("/users")]
                Task<string> Post([Body(CompressionLevel = System.IO.Compression.CompressionLevel.Fastest)] string body);
            }
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains(DefaultCodingArgument);
        await Assert.That(generated).Contains("global::System.IO.Compression.CompressionLevel.Fastest");
    }

    /// <summary>Verifies the coding applies to a URL-encoded form body, which builds its content on its own path.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CodingAppliesToAUrlEncodedFormBody()
    {
        var generated = Fixture.GenerateForDeclaration(
            """
            public interface IGeneratedClient
            {
                [Post("/users")]
                Task<string> Post([Body(BodySerializationMethod.UrlEncoded, Compression = RequestCompression.GZip)] string body);
            }
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains(CompressCall);
        await Assert.That(generated).Contains("global::Refit.RequestCompression.GZip");
    }

    /// <summary>Verifies the coding applies to a JSON Lines body, which builds its content on its own path.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CodingAppliesToAJsonLinesBody()
    {
        var generated = Fixture.GenerateForDeclaration(
            """
            public interface IGeneratedClient
            {
                [Post("/users")]
                Task<string> Post([Body(BodySerializationMethod.JsonLines, Compression = RequestCompression.GZip)] System.Collections.Generic.IEnumerable<string> body);
            }
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains(CompressCall);
        await Assert.That(generated).Contains("global::Refit.RequestCompression.GZip");
    }
}
