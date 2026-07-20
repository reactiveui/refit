// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Reflection.Tests;

/// <summary>A multipart API with a plain text part and a <see cref="FormObjectAttribute"/> parameter that flattens into one
/// text part per property, pinning the reflection builder's per-parameter form-object classification.</summary>
public interface IReflectionMultipartFormObjectApi
{
    /// <summary>Uploads a title text part alongside a flattened address object.</summary>
    /// <param name="title">A plain text part.</param>
    /// <param name="address">An object flattened into one text part per property.</param>
    /// <returns>The response body.</returns>
    [Multipart]
    [Post("/upload")]
    Task<string> Upload(string title, [FormObject] MultipartAddress address);
}
