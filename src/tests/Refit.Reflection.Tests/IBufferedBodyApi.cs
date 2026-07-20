// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Reflection.Tests;

/// <summary>A Refit interface with a buffered body parameter, exercised through the reflection builder.</summary>
public interface IBufferedBodyApi
{
    /// <summary>Posts a buffered serialized body.</summary>
    /// <param name="payload">The body payload.</param>
    /// <returns>A task that completes when the request finishes.</returns>
    [Post("/buffered")]
    Task Post([Body(true)] BodyPayload payload);
}
