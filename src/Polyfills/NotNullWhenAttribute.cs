// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if !NETCOREAPP3_0_OR_GREATER && !NETSTANDARD2_1_OR_GREATER
using System.Diagnostics;

namespace System.Diagnostics.CodeAnalysis;

/// <summary>Polyfill of the attribute that specifies a parameter is not null when the method returns the given value.</summary>
/// <param name="returnValue">The return value for which the parameter is guaranteed not null.</param>
[ExcludeFromCodeCoverage]
[DebuggerNonUserCode]
[AttributeUsage(AttributeTargets.Parameter)]
internal sealed class NotNullWhenAttribute(bool returnValue) : Attribute
{
    /// <summary>Gets the return value for which the parameter is guaranteed not null.</summary>
    public bool ReturnValue { get; } = returnValue;
}
#endif
