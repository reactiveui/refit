// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if !NET7_0_OR_GREATER
namespace System.Runtime.CompilerServices;

/// <summary>Polyfill marker the compiler emits for language features such as <c>required</c> members on older target frameworks.</summary>
/// <param name="featureName">The name of the required compiler feature.</param>
[Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
internal sealed class CompilerFeatureRequiredAttribute(string featureName) : Attribute
{
    /// <summary>The <see cref="FeatureName"/> value used for the <c>required</c> members feature.</summary>
    public const string RequiredMembers = nameof(RequiredMembers);

    /// <summary>Gets the name of the required compiler feature.</summary>
    public string FeatureName { get; } = featureName;

    /// <summary>Gets a value indicating whether the feature can be safely ignored by a compiler that does not understand it.</summary>
    public bool IsOptional { get; init; }
}
#endif
