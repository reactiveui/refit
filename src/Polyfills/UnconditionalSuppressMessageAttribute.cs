// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if !NET5_0_OR_GREATER
namespace System.Diagnostics.CodeAnalysis;

/// <summary>Polyfill of the attribute that suppresses analysis diagnostics independently of build configuration.</summary>
/// <param name="category">The category for the suppressed diagnostic.</param>
/// <param name="checkId">The identifier for the suppressed diagnostic.</param>
[AttributeUsage(
    AttributeTargets.All,
    AllowMultiple = true,
    Inherited = false)]
[ExcludeFromCodeCoverage]
internal sealed class UnconditionalSuppressMessageAttribute(string category, string checkId) : Attribute
{
    /// <summary>Gets the category for the suppressed diagnostic.</summary>
    public string Category { get; } = category;

    /// <summary>Gets the identifier for the suppressed diagnostic.</summary>
    public string CheckId { get; } = checkId;

    /// <summary>Gets or sets the suppression justification.</summary>
    public string? Justification { get; set; }

    /// <summary>Gets or sets the diagnostic scope.</summary>
    public string? Scope { get; set; }

    /// <summary>Gets or sets the target covered by this suppression.</summary>
    public string? Target { get; set; }
}
#endif
