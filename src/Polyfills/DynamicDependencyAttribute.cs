// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if !NET5_0_OR_GREATER
namespace System.Diagnostics.CodeAnalysis;

/// <summary>Polyfill of the DynamicDependency attribute for older target frameworks.</summary>
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method, AllowMultiple = true)]
internal sealed class DynamicDependencyAttribute
    : Attribute, DynamicDependencyAttribute.IMetadata
{
    /// <summary>Initializes a new instance of the <see cref="DynamicDependencyAttribute"/> class.</summary>
    /// <param name="memberTypes">The member types to preserve.</param>
    /// <param name="type">The owning type.</param>
    public DynamicDependencyAttribute(DynamicallyAccessedMemberTypes memberTypes, Type type)
    {
        MemberTypes = memberTypes;
        Type = type;
    }

    /// <summary>Initializes a new instance of the <see cref="DynamicDependencyAttribute"/> class.</summary>
    /// <param name="memberSignature">The signature of the depended-upon member.</param>
    /// <param name="type">The owning type.</param>
    public DynamicDependencyAttribute(string memberSignature, Type type)
    {
        MemberSignature = memberSignature;
        Type = type;
    }

    /// <summary>Defines the trimming-required public metadata contract.</summary>
    internal interface IMetadata
    {
        /// <summary>Gets the member categories that form the dependency.</summary>
        DynamicallyAccessedMemberTypes MemberTypes { get; }

        /// <summary>Gets the type that owns the dependency.</summary>
        Type? Type { get; }

        /// <summary>Gets the dependent member signature.</summary>
        string? MemberSignature { get; }
    }

    /// <summary>Gets the member types that must be preserved.</summary>
    public DynamicallyAccessedMemberTypes MemberTypes { get; }

    /// <summary>Gets the type that owns the depended-upon members.</summary>
    public Type? Type { get; }

    /// <summary>Gets the signature of the depended-upon member.</summary>
    public string? MemberSignature { get; }
}
#endif
