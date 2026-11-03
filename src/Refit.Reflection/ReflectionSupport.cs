// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Refit;

/// <summary>Registers the reflection support that core Refit calls when a generated client meets a value only reflection can handle.</summary>
public static class ReflectionSupport
{
    /// <summary>
    /// Registers the reflection-based flattening of nested form values with core Refit. It is safe to call more than
    /// once, and every reflection entry point in the Refit.Reflection packages calls it. Call it yourself when a
    /// generated client posts a nested or run-time-typed form body and nothing else loads this package.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void EnsureRegistered()
    {
        ReflectionHooks.FormObjectFlattener ??= new FormObjectFlattener();
        ReflectionHooks.EnumMemberValueResolver ??= EnumMemberValues.GetEnumMemberValue;
    }

    /// <summary>Creates a form map for a source object, registering the reflection support first.</summary>
    /// <param name="source">The source object or dictionary to convert into form entries.</param>
    /// <param name="settings">The Refit settings controlling formatting.</param>
    /// <returns>The created form map.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static FormValueMultimap CreateFormMap(object source, RefitSettings settings)
    {
        EnsureRegistered();
        return new(source, settings);
    }
}
