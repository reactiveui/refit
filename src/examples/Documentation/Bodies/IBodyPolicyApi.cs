// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.IO.Compression;

namespace Refit.Documentation;

/// <summary>Declares explicit buffering, coding opt-out, URI and timeout policies.</summary>
internal interface IBodyPolicyApi
{
    /// <summary>Forces buffering before the request is sent.</summary>
    /// <param name="person">The body to serialize.</param>
    /// <returns>The local person reply.</returns>
    [Post("/body/buffered")]
    Task<Person> BufferedAsync([Body(true)] Person person);

    /// <summary>Skips an instance-wide gzip coding for this body.</summary>
    /// <param name="person">The body to send without coding.</param>
    /// <returns>The local person reply.</returns>
    [Post("/body/none")]
    Task<Person> NoneAsync([Body(BodySerializationMethod.Serialized, false, Compression = RequestCompression.None)] Person person);

    /// <summary>Overrides an instance-wide gzip coding with Brotli.</summary>
    /// <param name="person">The body to compress.</param>
    /// <returns>The local person reply.</returns>
    [Post("/body/brotli")]
    Task<Person> BrotliAsync([Body(BodySerializationMethod.Serialized, Compression = RequestCompression.Brotli, CompressionLevel = CompressionLevel.Fastest)] Person person);

    /// <summary>URL-encodes the entire supplied string as one form-body value.</summary>
    /// <param name="text">The string escaped by the form helper.</param>
    /// <returns>The local person reply.</returns>
    [Post("/body/form-text")]
    Task<Person> FormTextAsync([Body(BodySerializationMethod.UrlEncoded)] string text);

    /// <summary>Wraps one model as a single JSON Lines item.</summary>
    /// <param name="person">The sole line value.</param>
    /// <returns>The local person reply.</returns>
    [Post("/body/one-line")]
    Task<Person> OneLineAsync([Body(BodySerializationMethod.JsonLines)] Person person);

    /// <summary>Uses a slash-rooted path whose resolution depends on settings.</summary>
    /// <returns>The local person reply.</returns>
    [Get("/child")]
    Task<Person> RootedAsync();

    /// <summary>Uses an RFC relative path appended to a slash-terminated base address.</summary>
    /// <returns>The local person reply.</returns>
    [Get("child")]
    Task<Person> RelativeAsync();

    /// <summary>Applies a positive per-call deadline to the local wait handler.</summary>
    /// <returns>The canceled local operation.</returns>
    [Get("/body/timeout")]
    [Timeout(BodyPolicies.TimeoutMilliseconds)]
    Task<Person> TimeoutAsync();
}
