// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Send the request as multipart.</summary>
/// <param name="boundaryText">The boundary text.</param>
/// <remarks>
/// Parts can be text, bytes, streams, files, HTTP content, MultipartItem wrappers, formatted values or serialized models.
/// FormObject property flattening requires the reflection request builder; file properties must be separate parameters.
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="MultipartAttribute"/> class.
/// </remarks>
[System.Diagnostics.DebuggerDisplay("{BoundaryText}")]
[AttributeUsage(AttributeTargets.Method)]
public sealed class MultipartAttribute(string boundaryText = "----MyGreatBoundary") : Attribute
{
    /// <summary>Gets the boundary text.</summary>
    /// <value>
    /// The boundary text.
    /// </value>
    public string BoundaryText { get; } = boundaryText;
}
