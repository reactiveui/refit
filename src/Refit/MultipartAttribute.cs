// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Send the request as multipart.</summary>
/// <remarks>
/// Currently, multipart methods only support the following parameter types: <see cref="string"/>, <see cref="byte"/> array, <see cref="System.IO.Stream"/>, <see cref="System.IO.FileInfo"/>.
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="MultipartAttribute"/> class.
/// </remarks>
/// <param name="boundaryText">The boundary text.</param>
[AttributeUsage(AttributeTargets.Method)]
public sealed class MultipartAttribute(string boundaryText = "----MyGreatBoundary") : Attribute
{
    /// <summary>Gets the boundary text.</summary>
    /// <value>
    /// The boundary text.
    /// </value>
    public string BoundaryText { get; } = boundaryText;
}
