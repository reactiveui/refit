// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Verifies the hook through which core Refit reaches the reflection-based form flattening.</summary>
public sealed class ReflectionHooksTests
{
    /// <summary>Verifies a missing flattener fails with a message that explains how to enable the reflection support.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task MissingFlattenerExplainsHowToEnableIt()
    {
        var exception = await Assert
            .That(static () => ReflectionHooks.Require(null))
            .ThrowsExactly<NotSupportedException>();

        await Assert.That(exception!.Message).Contains("ReflectionSupport.EnsureRegistered");
    }

    /// <summary>Verifies missing JSON type metadata fails with a message that names the context and the reflection registration.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task MissingJsonResolverExplainsHowToSupplyMetadata()
    {
        var exception = await Assert
            .That(static () => ReflectionHooks.CreateDefaultJsonResolver(null))
            .ThrowsExactly<NotSupportedException>();

        await Assert.That(exception!.Message).Contains("JsonSerializerContext");
        await Assert.That(exception.Message).Contains("ReflectionSupport.EnsureRegistered");
    }

    /// <summary>Verifies the default serializer reads and writes JSON once the reflection support is registered.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task DefaultSerializerUsesRegisteredReflectionResolver()
    {
        ReflectionSupport.EnsureRegistered();
        var serializer = new SystemTextJsonContentSerializer();

        var roundTrip = serializer.DeserializeFromString<NestedValue>("""{"child":{"name":"x"}}""");

        await Assert.That(roundTrip!.Child!.Name).IsEqualTo("x");
        await Assert.That(serializer.SerializerOptions.TypeInfoResolver).IsNotNull();
    }

    /// <summary>Verifies a nested form value flattens once the reflection support is registered.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task NestedFormValueFlattensWhenSupportIsRegistered()
    {
        ReflectionSupport.EnsureRegistered();

        var map = new FormValueMultimap(new NestedValue { Child = new() }, new RefitSettings());

        await Assert.That(map.Keys.Single()).IsEqualTo("Child.Name");
    }

    /// <summary>Verifies registering twice keeps the first flattener.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task EnsureRegisteredIsIdempotent()
    {
        ReflectionSupport.EnsureRegistered();
        var first = ReflectionHooks.FormObjectFlattener;

        ReflectionSupport.EnsureRegistered();

        await Assert.That(first).IsNotNull();
        await Assert.That(ReflectionHooks.FormObjectFlattener).IsSameReferenceAs(first);
    }

    /// <summary>Form value whose property is a complex object, which needs the reflection flattener.</summary>
    private sealed class NestedValue
    {
        /// <summary>Gets or sets the nested complex value.</summary>
        public Inner? Child { get; set; }
    }

    /// <summary>Complex value nested in <see cref="NestedValue"/>.</summary>
    private sealed class Inner
    {
        /// <summary>Gets or sets a scalar the flattener would emit.</summary>
        public string? Name { get; set; } = "n";
    }
}
