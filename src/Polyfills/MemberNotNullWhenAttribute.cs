// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if !NET5_0_OR_GREATER
using System.Diagnostics;

namespace System.Diagnostics.CodeAnalysis;

/// <summary>Polyfill for the member-not-null-when attribute on older target frameworks.</summary>
/// <param name="returnValue">The return value condition under which the members are non-null.</param>
/// <param name="members">The member names guaranteed to be non-null.</param>
[ExcludeFromCodeCoverage]
[DebuggerNonUserCode]
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
internal sealed class MemberNotNullWhenAttribute(bool returnValue, params string[] members)
    : Attribute, MemberNotNullWhenAttribute.IMetadata
{
    /// <summary>Defines the nullable-analysis public metadata contract.</summary>
    internal interface IMetadata
    {
        /// <summary>Gets the return value for which the members are non-null.</summary>
        bool ReturnValue { get; }

        /// <summary>Gets the members that are non-null.</summary>
        string[] Members { get; }
    }

    /// <summary>Gets the return value condition under which the members are non-null.</summary>
    public bool ReturnValue { get; } = returnValue;

    /// <summary>Gets the member names guaranteed to be non-null.</summary>
    public string[] Members { get; } = members;
}
#endif
