// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.IO;
using System.IO.Compression;

namespace Refit.Documentation;

/// <summary>Declares POST requests that demonstrate each body serialization mode.</summary>
internal interface IBodyApi
{
    /// <summary>Serializes a person as JSON and reads the saved person.</summary>
    /// <param name="person">The person model serialized using the configured generated JSON metadata.</param>
    /// <returns>The person deserialized from the successful response.</returns>
    [Post("/body/json")]
    Task<Person> JsonAsync([Body] Person person);

    /// <summary>Sends text directly without JSON quoting.</summary>
    /// <param name="text">The text to send using the declared body serialization mode.</param>
    /// <returns>The person deserialized from the successful response.</returns>
    [Post("/body/text")]
    Task<Person> TextAsync([Body] string text);

    /// <summary>Serializes the text as a JSON string including its quotes.</summary>
    /// <param name="text">The text to send using the declared body serialization mode.</param>
    /// <returns>The person deserialized from the successful response.</returns>
    [Post("/body/quoted")]
    Task<Person> QuotedAsync([Body(BodySerializationMethod.Serialized)] string text);

    /// <summary>Sends the supplied stream while leaving stream ownership with the caller.</summary>
    /// <param name="stream">The readable body stream, which remains owned by the caller.</param>
    /// <returns>The person deserialized from the successful response.</returns>
    [Post("/body/stream")]
    Task<Person> StreamAsync([Body] Stream stream);

    /// <summary>Sends caller-created HTTP content without model serialization.</summary>
    /// <param name="content">The caller-created content to send without model serialization.</param>
    /// <returns>The person deserialized from the successful response.</returns>
    [Post("/body/content")]
    Task<Person> ContentAsync([Body] HttpContent content);

    /// <summary>Encodes aliases, repeated tags and null fields as a URL-encoded form.</summary>
    /// <param name="form">The values to encode as form fields using their aliases and query attributes.</param>
    /// <returns>The person deserialized from the successful response.</returns>
    [Post("/body/form")]
    Task<Person> FormAsync([Body(BodySerializationMethod.UrlEncoded)] ContactForm form);

    /// <summary>Serializes each person as a separate JSON line.</summary>
    /// <param name="people">The people emitted as JSON lines or indexed query entries, as declared.</param>
    /// <returns>The person deserialized from the successful response.</returns>
    [Post("/body/lines")]
    Task<Person> LinesAsync([Body(BodySerializationMethod.JsonLines)] IEnumerable<Person> people);

    /// <summary>Buffers and gzip-compresses the serialized person before sending it.</summary>
    /// <param name="person">The person model serialized using the configured generated JSON metadata.</param>
    /// <returns>The person deserialized from the successful response.</returns>
    [Post("/body/gzip")]
    Task<Person> GzipAsync([Body(
        BodySerializationMethod.Serialized,
        true,
        Compression = RequestCompression.GZip,
        CompressionLevel = CompressionLevel.Fastest)] Person person);
}
