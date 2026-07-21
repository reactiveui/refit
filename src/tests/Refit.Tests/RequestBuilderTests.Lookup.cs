// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Refit.Testing;

namespace Refit.Tests;

/// <summary>Tests for the reflection request builder's method-lookup guards.</summary>
public partial class RequestBuilderTests
{
    /// <summary>Rejects a null request-builder target.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ConstructorRejectsNullTargets() =>
        await Assert.That(static () => new RequestBuilderImplementation(null!))
            .ThrowsExactly<ArgumentException>();

    /// <summary>Reports a missing declared method rather than returning null.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task FindDeclaredMethodThrowsWhenTheMethodDoesNotExist() =>
        await Assert.That(static () => RequestBuilderImplementation.FindDeclaredMethod("NoSuchBuilderMethod"))
            .ThrowsExactly<MissingMethodException>();

    /// <summary>Reports an unknown interface method name.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task BuildRestResultFuncForMethodThrowsForAnUnknownMethodName()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();

        await Assert.That(() => fixture.BuildRestResultFuncForMethod("NoSuchApiMethod"))
            .ThrowsExactly<ArgumentException>();
    }

    /// <summary>Reports that no overload accepts the supplied parameter types.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task BuildRestResultFuncForMethodThrowsWhenNoOverloadMatchesTheParameterTypes()
    {
        var fixture = new RequestBuilderImplementation<IDummyHttpApi>();

        await Assert
            .That(() => fixture.BuildRestResultFuncForMethod(
                nameof(IDummyHttpApi.FetchSomeStuff),
                [typeof(int), typeof(int)]))
            .ThrowsExactly<InvalidOperationException>();
    }

    /// <summary>Builds a task delegate for a method that declares a cancellation token.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task TaskFuncForMethodPassesTheDeclaredCancellationToken()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Template = "*", Reusable = true },
                Reply.Content(new StringContent("ok"))
            },
        };

        var fixture = new RequestBuilderImplementation<ICancellableMethods>();
        var func = fixture.BuildRestResultFuncForMethod(nameof(ICancellableMethods.GetWithCancellationAndReturn));
        using var client = HttpClientTestFactory.Create(handler, new(ApiBaseUrlWithSlash));

        var result = await (Task<string>)func(client, [CancellationToken.None])!;

        await Assert.That(result).IsEqualTo("ok");
    }
}
