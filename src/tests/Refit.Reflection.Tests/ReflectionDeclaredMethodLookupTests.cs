// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Reflection.Tests;

/// <summary>Pins that the reflection request builder resolves each of its private delegate-factory methods to the method
/// of that exact name, so the by-name declared-method cache keys correctly, and that an unknown name throws rather than
/// poisoning the cache.</summary>
public sealed class ReflectionDeclaredMethodLookupTests
{
    /// <summary>Verifies a known factory name resolves to a method of that name declared on the builder type, across
    /// repeated lookups that exercise both the cache miss and the cache hit.</summary>
    /// <param name="name">The declared factory-method name to resolve.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments(nameof(RequestBuilderImplementation.BuildTaskFuncForMethod))]
    [Arguments(nameof(RequestBuilderImplementation.BuildVoidTaskFuncForMethod))]
    [Arguments(nameof(RequestBuilderImplementation.BuildRxFuncForMethod))]
    public async Task ResolvesEachFactoryMethodToItsOwnName(string name)
    {
        var first = RequestBuilderImplementation.FindDeclaredMethod(name);
        var second = RequestBuilderImplementation.FindDeclaredMethod(name);

        await Assert.That(first.Name).IsEqualTo(name);
        await Assert.That(first.DeclaringType).IsEqualTo(typeof(RequestBuilderImplementation));
        await Assert.That(second).IsSameReferenceAs(first);
    }

    /// <summary>Verifies an unknown method name throws and is not cached, so a later valid lookup still succeeds.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UnknownMethodNameThrowsWithoutPoisoningTheCache()
    {
        await Assert.That(static () => RequestBuilderImplementation.FindDeclaredMethod("NoSuchFactoryMethod"))
            .ThrowsExactly<MissingMethodException>();

        var resolved = RequestBuilderImplementation.FindDeclaredMethod(
            nameof(RequestBuilderImplementation.BuildTaskFuncForMethod));

        await Assert.That(resolved.Name).IsEqualTo(nameof(RequestBuilderImplementation.BuildTaskFuncForMethod));
    }
}
