// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A Refit interface exercising a variety of POST body serialization scenarios.</summary>
public interface IRequestBin
{
    /// <summary>Posts to a fixed endpoint with no body.</summary>
    /// <returns>A task that completes when the request finishes.</returns>
    [Post("/1h3a5jm1")]
    Task Post();

    /// <summary>Posts a raw string body using the default serialization method.</summary>
    /// <param name="str">The raw string body.</param>
    /// <returns>A task that completes when the request finishes.</returns>
    [Post("/foo")]
    Task PostRawStringDefault([Body] string str);

    /// <summary>Posts a raw string body serialized as JSON.</summary>
    /// <param name="str">The raw string body.</param>
    /// <returns>A task that completes when the request finishes.</returns>
    [Post("/foo")]
    Task PostRawStringJson([Body(BodySerializationMethod.Serialized)] string str);

    /// <summary>Posts a raw string body url-encoded.</summary>
    /// <param name="str">The raw string body.</param>
    /// <returns>A task that completes when the request finishes.</returns>
    [Post("/foo")]
    Task PostRawStringUrlEncoded([Body(BodySerializationMethod.UrlEncoded)] string str);

    /// <summary>Posts a generic body parameter.</summary>
    /// <typeparam name="T">The body type.</typeparam>
    /// <param name="param">The body value.</param>
    /// <returns>A task that completes when the request finishes.</returns>
    [Post("/1h3a5jm1")]
    Task PostGeneric<T>(T param);

    /// <summary>Posts a buffered generic body with a void return.</summary>
    /// <typeparam name="T">The body type.</typeparam>
    /// <param name="param">The body value.</param>
    /// <returns>A task that completes when the request finishes.</returns>
    [Post("/foo")]
    Task PostVoidReturnBodyBuffered<T>([Body(buffered: true)] T param);

    /// <summary>Posts a buffered generic body and returns the response string.</summary>
    /// <typeparam name="T">The body type.</typeparam>
    /// <param name="param">The body value.</param>
    /// <returns>The response body as a string.</returns>
    [Post("/foo")]
    Task<string> PostNonVoidReturnBodyBuffered<T>([Body(buffered: true)] T param);

    /// <summary>Posts a large object body.</summary>
    /// <param name="big">The large object to post.</param>
    /// <returns>A task that completes when the request finishes.</returns>
    [Post("/big")]
    Task PostBig(BigObject big);

    /// <summary>Exercises a route whose template parameter shares the generated code-gen variable name.</summary>
    /// <param name="arguments">The path segment value.</param>
    /// <returns>A task that completes when the request finishes.</returns>
    [Get("/foo/{arguments}")]
    Task SomeApiThatUsesVariableNameFromCodeGen(string arguments);
}
