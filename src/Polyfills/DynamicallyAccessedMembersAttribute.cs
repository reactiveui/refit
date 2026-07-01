// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if !NET
namespace System.Diagnostics.CodeAnalysis;

/// <summary>Polyfill of the trimming attribute indicating which members are accessed dynamically.</summary>
/// <param name="memberTypes">The member types that are dynamically accessed.</param>
[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Field |
    AttributeTargets.GenericParameter |
    AttributeTargets.Interface |
    AttributeTargets.Method |
    AttributeTargets.Parameter |
    AttributeTargets.Property |
    AttributeTargets.ReturnValue |
    AttributeTargets.Struct,
    Inherited = false)]
[ExcludeFromCodeCoverage]
internal sealed class DynamicallyAccessedMembersAttribute(DynamicallyAccessedMemberTypes memberTypes) : Attribute
{
    /// <summary>Gets the member types that are dynamically accessed.</summary>
    public DynamicallyAccessedMemberTypes MemberTypes { get; } = memberTypes;
}
#endif
