// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if !NET5_0_OR_GREATER
using System.Diagnostics;

namespace System.Diagnostics.CodeAnalysis;

/// <summary>Indicates that the method does not return when the associated parameter has the specified value.</summary>
/// <param name="parameterValue">The parameter value that signals the method will not return.</param>
[ExcludeFromCodeCoverage]
[DebuggerNonUserCode]
[AttributeUsage(AttributeTargets.Parameter)]
internal sealed class DoesNotReturnIfAttribute(bool parameterValue) : Attribute
{
    /// <summary>Gets the parameter value that signals the method will not return.</summary>
    public bool ParameterValue { get; } = parameterValue;
}
#endif
