// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A Refit API surface exercising optional <c>{name?}</c> URL path segments (issue #591).</summary>
public interface IOptionalSegmentApi
{
    /// <summary>Gets a value where the trailing segment is optional.</summary>
    /// <param name="deviceId">The required device identifier.</param>
    /// <param name="notifMsgId">The optional notification message identifier; a null value drops the segment.</param>
    /// <returns>The response body.</returns>
    [Get("/push/{deviceId}/{notifMsgId?}")]
    Task<string> GetTrailingOptional(string deviceId, string? notifMsgId);

    /// <summary>Gets a value where an interior segment is optional.</summary>
    /// <param name="first">The optional interior segment; a null value drops the segment and its preceding slash.</param>
    /// <returns>The response body.</returns>
    [Get("/a/{first?}/b")]
    Task<string> GetInteriorOptional(string? first);

    /// <summary>Gets a value where an optional placeholder immediately follows another placeholder in one segment.</summary>
    /// <param name="first">The required leading value.</param>
    /// <param name="second">The optional trailing value; a null value leaves the leading value untouched.</param>
    /// <returns>The response body.</returns>
    [Get("/foo/{first}{second?}")]
    Task<string> GetAdjacentOptional(string first, string? second);

    /// <summary>Gets a value where an optional dotted object-property placeholder binds the trailing segment.</summary>
    /// <param name="repo">The object whose properties bind the path.</param>
    /// <returns>The response body.</returns>
    [Get("/orgs/{repo.Owner}/{repo.Name?}")]
    Task<string> GetOptionalObjectProperty(Repository repo);
}
