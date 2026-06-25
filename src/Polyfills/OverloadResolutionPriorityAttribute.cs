// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if !NET9_0_OR_GREATER
namespace System.Runtime.CompilerServices;

/// <summary>
/// Polyfill for the .NET 9 attribute that biases overload resolution toward the annotated member.
/// The C# compiler recognizes this attribute by its full name, so defining it here lets older target
/// frameworks honor <c>[OverloadResolutionPriority]</c> the same way net9.0+ does.
/// </summary>
/// <param name="priority">The relative priority; higher values win during overload resolution.</param>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[AttributeUsage(
    AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Property,
    AllowMultiple = false,
    Inherited = false)]
internal sealed class OverloadResolutionPriorityAttribute(int priority) : Attribute
{
    /// <summary>Gets the priority of the annotated member.</summary>
    public int Priority { get; } = priority;
}
#endif
