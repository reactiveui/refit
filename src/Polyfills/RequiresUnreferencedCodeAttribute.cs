// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if !NET5_0_OR_GREATER
namespace System.Diagnostics.CodeAnalysis;

/// <summary>Polyfill of the attribute that marks members whose use requires code that may be trimmed.</summary>
/// <param name="message">The message describing why the code is required.</param>
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Event, Inherited = false)]
internal sealed class RequiresUnreferencedCodeAttribute(string message) : Attribute
{
    /// <summary>Gets the message describing why the code is required.</summary>
    public string Message { get; } = message;

    /// <summary>Gets or sets an optional URL with more information.</summary>
    public string? Url { get; set; }
}
#endif
