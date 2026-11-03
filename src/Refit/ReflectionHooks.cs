// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json.Serialization.Metadata;

namespace Refit;

/// <summary>Holds the run-time implementations the Refit.Reflection package registers for work core Refit cannot do without reflection.</summary>
internal static class ReflectionHooks
{
    /// <summary>Gets or sets the flattener for complex form values, or <see langword="null"/> when Refit.Reflection is not loaded.</summary>
    internal static IFormObjectFlattener? FormObjectFlattener { get; set; }

    /// <summary>Gets or sets the reader of <c>[EnumMember]</c> values for run-time-typed enums, or <see langword="null"/> when Refit.Reflection is not loaded.</summary>
    internal static Func<Type, object, string?>? EnumMemberValueResolver { get; set; }

    /// <summary>Gets or sets the factory for System.Text.Json's reflection resolver, or <see langword="null"/> when Refit.Reflection is not loaded.</summary>
    internal static Func<IJsonTypeInfoResolver>? DefaultJsonResolverFactory { get; set; }

    /// <summary>Gets or sets the test for a reflection-based resolver, or <see langword="null"/> when Refit.Reflection is not loaded.</summary>
    internal static Func<IJsonTypeInfoResolver, bool>? ReflectionResolverTest { get; set; }

    /// <summary>Gets or sets the copier of reflection resolver modifiers onto another resolver, or <see langword="null"/> when Refit.Reflection is not loaded.</summary>
    internal static Func<IJsonTypeInfoResolver, List<IJsonTypeInfoResolver>, IJsonTypeInfoResolver>? ModifierCarrier { get; set; }

    /// <summary>Determines whether a resolver is the reflection-based one.</summary>
    /// <param name="resolver">The resolver to test.</param>
    /// <returns><see langword="true"/> when Refit.Reflection is loaded and the resolver is reflection based.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsReflectionResolver(IJsonTypeInfoResolver resolver) => IsReflectionResolver(ReflectionResolverTest, resolver);

    /// <summary>Determines whether a resolver is the reflection-based one using a given test.</summary>
    /// <param name="test">The registered test, or <see langword="null"/> when none is registered.</param>
    /// <param name="resolver">The resolver to test.</param>
    /// <returns><see langword="true"/> when a test is registered and accepts the resolver.</returns>
    internal static bool IsReflectionResolver(Func<IJsonTypeInfoResolver, bool>? test, IJsonTypeInfoResolver resolver) =>
        test is not null && test(resolver);

    /// <summary>Applies the modifiers of reflection resolvers to another resolver.</summary>
    /// <param name="resolver">The resolver that receives the modifiers.</param>
    /// <param name="reflective">The reflection resolvers whose modifiers are carried over.</param>
    /// <returns>The resolver with every modifier applied, or the same resolver when none are carried.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static IJsonTypeInfoResolver CarryModifiers(IJsonTypeInfoResolver resolver, List<IJsonTypeInfoResolver> reflective) =>
        CarryModifiers(ModifierCarrier, resolver, reflective);

    /// <summary>Applies the modifiers of reflection resolvers to another resolver using a given carrier.</summary>
    /// <param name="carrier">The registered carrier, or <see langword="null"/> when none is registered.</param>
    /// <param name="resolver">The resolver that receives the modifiers.</param>
    /// <param name="reflective">The reflection resolvers whose modifiers are carried over.</param>
    /// <returns>The resolver with every modifier applied, or the same resolver when none are carried.</returns>
    internal static IJsonTypeInfoResolver CarryModifiers(
        Func<IJsonTypeInfoResolver, List<IJsonTypeInfoResolver>, IJsonTypeInfoResolver>? carrier,
        IJsonTypeInfoResolver resolver,
        List<IJsonTypeInfoResolver> reflective) =>
        carrier is null ? resolver : carrier(resolver, reflective);

    /// <summary>Creates System.Text.Json's reflection resolver or explains how to avoid needing one.</summary>
    /// <returns>A new reflection resolver.</returns>
    /// <exception cref="NotSupportedException">The Refit.Reflection package is not loaded.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static IJsonTypeInfoResolver CreateDefaultJsonResolver() => CreateDefaultJsonResolver(DefaultJsonResolverFactory);

    /// <summary>Creates System.Text.Json's reflection resolver from a factory or explains how to avoid needing one.</summary>
    /// <param name="factory">The registered factory, or <see langword="null"/> when none is registered.</param>
    /// <returns>A new reflection resolver.</returns>
    /// <exception cref="NotSupportedException"><paramref name="factory"/> is <see langword="null"/>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static IJsonTypeInfoResolver CreateDefaultJsonResolver(Func<IJsonTypeInfoResolver>? factory) =>
        (factory
        ?? throw new NotSupportedException(
            "These JSON serializer options have no type metadata. Supply a source-generated JsonSerializerContext "
            + "(for example RefitSettings.ForJsonContext), or reference the Refit.Reflection package and call "
            + "ReflectionSupport.EnsureRegistered() to use reflection-based JSON."))();

    /// <summary>Gets the registered form flattener or explains how to add one.</summary>
    /// <returns>The registered flattener.</returns>
    /// <exception cref="NotSupportedException">The Refit.Reflection package is not loaded.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static IFormObjectFlattener RequireFormObjectFlattener() => Require(FormObjectFlattener);

    /// <summary>Returns the flattener or explains how to add one.</summary>
    /// <param name="flattener">The flattener to check, or <see langword="null"/> when none is registered.</param>
    /// <returns>The same flattener.</returns>
    /// <exception cref="NotSupportedException"><paramref name="flattener"/> is <see langword="null"/>.</exception>
    internal static IFormObjectFlattener Require(IFormObjectFlattener? flattener) =>
        flattener
        ?? throw new NotSupportedException(
            "Flattening a nested or run-time-typed form value needs the reflection support in the Refit.Reflection "
            + "package. Add a reference to it and call ReflectionSupport.EnsureRegistered(), or use a form body made "
            + "only of simple values and collections so the source generator can handle it.");
}
