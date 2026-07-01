// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if !NET7_0_OR_GREATER
namespace System.Diagnostics.CodeAnalysis;

/// <summary>Polyfill marking members that require runtime code generation.</summary>
/// <param name="message">A message describing the dynamic code requirement.</param>
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Constructor | AttributeTargets.Method, Inherited = false)]
internal sealed class RequiresDynamicCodeAttribute(string message) : Attribute
{
    /// <summary>Gets the message describing the dynamic code requirement.</summary>
    public string Message { get; } = message;

    /// <summary>Gets or sets a value indicating whether static members are excluded.</summary>
    public bool ExcludeStatics { get; set; }

    /// <summary>Gets or sets an optional URL with more information.</summary>
    public string? Url { get; set; }
}
#endif
