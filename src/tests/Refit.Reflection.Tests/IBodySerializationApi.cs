// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Reflection.Tests;

/// <summary>A Refit interface with a serialized body parameter, exercised through the reflection builder.</summary>
public interface IBodySerializationApi
{
    /// <summary>Posts a serialized body.</summary>
    /// <param name="payload">The body payload.</param>
    /// <returns>A task that completes when the request finishes.</returns>
    [Post("/upload")]
    Task Post([Body] BodyPayload payload);
}
