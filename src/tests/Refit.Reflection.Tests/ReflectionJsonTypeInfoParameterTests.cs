// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Reflection;

namespace Refit.Reflection.Tests;

/// <summary>Pins that the reflection request builder refuses a <c>JsonTypeInfo&lt;T&gt;</c> parameter instead of sending it as a body or query value.</summary>
public sealed class ReflectionJsonTypeInfoParameterTests
{
    /// <summary>Verifies a metadata parameter is refused with a message that names the method and the parameter.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MetadataParameterIsRefused()
    {
        var method = typeof(IReflectionJsonTypeInfoApi).GetMethod(nameof(IReflectionJsonTypeInfoApi.GetValue))!;
        var parameters = method.GetParameters();

        var exception = await Assert.That(() => RestMethodInfoInternal.VerifyNoJsonTypeInfoParameters(method, parameters))
            .ThrowsExactly<ArgumentException>();

        using (Assert.Multiple())
        {
            await Assert.That(exception!.Message).Contains(nameof(IReflectionJsonTypeInfoApi.GetValue));
            await Assert.That(exception.Message).Contains("typeInfo");
        }
    }

    /// <summary>Verifies other generic parameters are accepted.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task OtherGenericParametersAreAccepted()
    {
        var method = typeof(IReflectionJsonTypeInfoApi).GetMethod(nameof(IReflectionJsonTypeInfoApi.GetValues))!;
        var parameters = method.GetParameters();

        await Assert.That(() => RestMethodInfoInternal.VerifyNoJsonTypeInfoParameters(method, parameters)).ThrowsNothing();
    }

    /// <summary>Verifies a method without parameters is accepted.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ParameterlessMethodIsAccepted()
    {
        var method = typeof(object).GetMethod(nameof(ToString))!;

        await Assert.That(() => RestMethodInfoInternal.VerifyNoJsonTypeInfoParameters(method, [])).ThrowsNothing();
    }
}
