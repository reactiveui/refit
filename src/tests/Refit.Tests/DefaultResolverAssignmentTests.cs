// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Refit.Tests;

/// <summary>Verifies the default reflection resolver is assigned once and never to options that cannot take it.</summary>
public sealed class DefaultResolverAssignmentTests
{
    /// <summary>Verifies assigning twice keeps the first resolver.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SecondAssignmentKeepsTheFirstResolver()
    {
        ReflectionSupport.EnsureRegistered();
        var options = SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions();
        var serializer = new SystemTextJsonContentSerializer(options);

        serializer.AssignDefaultResolver();
        var first = options.TypeInfoResolver;
        serializer.AssignDefaultResolver();

        await Assert.That(first).IsNotNull();
        await Assert.That(options.TypeInfoResolver).IsSameReferenceAs(first);
    }

    /// <summary>Verifies options that already carry a resolver, even read-only ones, are left alone.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task ReadOnlyOptionsWithAResolverAreNotAssigned()
    {
        var options = SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions();
        options.TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver();
        var resolver = options.TypeInfoResolver;
        options.MakeReadOnly();
        var serializer = new SystemTextJsonContentSerializer(options);

        serializer.AssignDefaultResolver();

        await Assert.That(options.TypeInfoResolver).IsSameReferenceAs(resolver);
    }
}
