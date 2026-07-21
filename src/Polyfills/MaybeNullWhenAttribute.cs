// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if !NETCOREAPP3_0_OR_GREATER && !NETSTANDARD2_1_OR_GREATER
using System.Diagnostics;

namespace System.Diagnostics.CodeAnalysis;

/// <summary>Specifies that an output parameter may be null when the method returns the given value.</summary>
/// <param name="returnValue">The return value condition under which the parameter may be null.</param>
[ExcludeFromCodeCoverage]
[DebuggerNonUserCode]
[AttributeUsage(AttributeTargets.Parameter)]
internal sealed class MaybeNullWhenAttribute(bool returnValue)
    : Attribute, MaybeNullWhenAttribute.IMetadata
{
    /// <summary>Defines the nullable-analysis public metadata contract.</summary>
    internal interface IMetadata
    {
        /// <summary>Gets the return value for which the parameter may be null.</summary>
        bool ReturnValue { get; }
    }

    /// <summary>Gets the return value condition under which the parameter may be null.</summary>
    public bool ReturnValue { get; } = returnValue;
}
#endif
