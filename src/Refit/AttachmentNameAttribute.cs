// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Names an attachment in a multipart request.</summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AttachmentNameAttribute"/> class.
/// </remarks>
/// <param name="name">The name.</param>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
[Obsolete(
    "Use Refit.StreamPart, Refit.ByteArrayPart, Refit.FileInfoPart or if necessary, inherit from Refit.MultipartItem",
    false)]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Major Code Smell",
    "S1133:Deprecated code should be removed",
    Justification = "Public API retained for backwards compatibility; cannot remove without a breaking change.")]
public sealed class AttachmentNameAttribute(string name) : Attribute
{
    /// <summary>Gets the name.</summary>
    /// <value>
    /// The name.
    /// </value>
    public string Name { get; } = name;
}
