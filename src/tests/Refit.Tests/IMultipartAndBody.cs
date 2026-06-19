// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Tests;

/// <summary>Fixture combining a multipart request with a body parameter, used to verify Refit rejects it.</summary>
public interface IMultipartAndBody
{
    /// <summary>Endpoint marked multipart that also declares a body parameter.</summary>
    /// <param name="body">The body content.</param>
    /// <returns>The response body.</returns>
    [Get("/}")]
    [Multipart]
    Task<string> GetValue([Body] string body);
}
