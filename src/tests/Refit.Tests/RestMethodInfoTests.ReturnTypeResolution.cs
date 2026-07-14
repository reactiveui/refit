// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;

namespace Refit.Tests;

/// <summary>Tests for <see cref="RestMethodInfoInternal"/> return type, HTTP method and body serialization resolution.</summary>
public partial class RestMethodInfoTests
{
    /// <summary>Verifies value-type body parameters do not throw when buffered.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ValueTypesDontBlowUpBuffered()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(static x => x.Name == nameof(IRestMethodInfoTests.OhYeahValueTypes)));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo!.Item1).IsEqualTo(BodySerializationMethod.Default);
        await Assert.That(fixture.BodyParameterInfo.Item2).IsTrue(); // buffered default
        await Assert.That(fixture.BodyParameterInfo.Item3).IsEqualTo(1);

        await Assert.That(fixture.ReturnResultType).IsEqualTo(typeof(bool));
    }

    /// <summary>Verifies value-type body parameters do not throw when unbuffered.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ValueTypesDontBlowUpUnBuffered()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(static x => x.Name == nameof(IRestMethodInfoTests.OhYeahValueTypesUnbuffered)));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo!.Item1).IsEqualTo(BodySerializationMethod.Default);
        await Assert.That(fixture.BodyParameterInfo.Item2).IsFalse(); // unbuffered specified
        await Assert.That(fixture.BodyParameterInfo.Item3).IsEqualTo(1);

        await Assert.That(fixture.ReturnResultType).IsEqualTo(typeof(bool));
    }

    /// <summary>Verifies a stream pull method parses its body parameter correctly.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task StreamMethodPullWorks()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(static x => x.Name == nameof(IRestMethodInfoTests.PullStreamMethod)));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo!.Item1).IsEqualTo(BodySerializationMethod.Default);
        await Assert.That(fixture.BodyParameterInfo.Item2).IsTrue();
        await Assert.That(fixture.BodyParameterInfo.Item3).IsEqualTo(1);

        await Assert.That(fixture.ReturnResultType).IsEqualTo(typeof(bool));
    }

    /// <summary>Verifies a method returning a non-generic task resolves its return type.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ReturningTaskShouldWork()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input.GetMethods().First(static x => x.Name == nameof(IRestMethodInfoTests.VoidPost)));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);

        await Assert.That(fixture.ReturnType).IsEqualTo(typeof(Task));
        await Assert.That(fixture.ReturnResultType).IsEqualTo(typeof(void));
    }

    /// <summary>Verifies a synchronous method throws an argument exception.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SyncMethodsShouldThrow()
    {
        var shouldDie = true;

        try
        {
            var input = typeof(IRestMethodInfoTests);
            _ = new RestMethodInfoInternal(
                input,
                input
                    .GetMethods()
                    .First(static x => x.Name == nameof(IRestMethodInfoTests.AsyncOnlyBuddy)));
        }
        catch (ArgumentException)
        {
            shouldDie = false;
        }

        await Assert.That(shouldDie).IsFalse();
    }

    /// <summary>Verifies the patch attribute sets the HTTP method to PATCH.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task UsingThePatchAttributeSetsTheCorrectMethod()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input.GetMethods().First(static x => x.Name == nameof(IRestMethodInfoTests.PatchSomething)));

        await Assert.That(fixture.HttpMethod.Method).IsEqualTo("PATCH");
    }

    /// <summary>Verifies the options attribute sets the HTTP method to OPTIONS.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task UsingOptionsAttribute()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input.GetMethods().First(static x => x.Name == nameof(IDummyHttpApi.SendOptions)));

        await Assert.That(fixture.HttpMethod.Method).IsEqualTo("OPTIONS");
    }

    /// <summary>Verifies the api response flag is set for a method returning an API response.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ApiResponseShouldBeSet()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(static x => x.Name == nameof(IRestMethodInfoTests.PostReturnsApiResponse)));

        await Assert.That(fixture.IsApiResponse).IsTrue();
    }

    /// <summary>Verifies the api response flag is not set for a method returning a non-API response.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ApiResponseShouldNotBeSet()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(static x => x.Name == nameof(IRestMethodInfoTests.PostReturnsNonApiResponse)));

        await Assert.That(fixture.IsApiResponse).IsFalse();
    }

    /// <summary>Verifies a generic return type that is not a task or observable throws.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task GenericReturnTypeIsNotTaskOrObservableShouldThrow()
    {
        var input = typeof(IRestMethodInfoTests);
        await Assert.That(
            () =>
                new RestMethodInfoInternal(
                    input,
                    input
                        .GetMethods()
                        .First(
                            static x => x.Name == nameof(IRestMethodInfoTests.InvalidGenericReturnType)))).ThrowsExactly<ArgumentException>();
    }

    /// <summary>Verifies an internal sync generic return type sets the deserialized type to the return type.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task InternalSyncGenericReturnTypeSetsDeserializedTypeToReturnType()
    {
        var input = typeof(IInternalSyncGenericReturnTypeApi);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetTypeInfo()
                .DeclaredMethods
                .First(static x => x.Name == nameof(IInternalSyncGenericReturnTypeApi.GetValues)));

        await Assert.That(fixture.ReturnType).IsEqualTo(typeof(List<string>));
        await Assert.That(fixture.ReturnResultType).IsEqualTo(typeof(List<string>));
        await Assert.That(fixture.DeserializedResultType).IsEqualTo(typeof(List<string>));
    }

    /// <summary>Verifies an internal sync IApiResponse generic return type sets the deserialized type to the generic argument.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task InternalSyncIApiResponseGenericReturnTypeSetsDeserializedTypeToGenericArgument()
    {
        var input = typeof(IInternalSyncGenericApiResponseReturnTypeApi);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetTypeInfo()
                .DeclaredMethods
                .First(static x => x.Name == nameof(IInternalSyncGenericApiResponseReturnTypeApi.GetResponse)));

        await Assert.That(fixture.ReturnType).IsEqualTo(typeof(IApiResponse<string>));
        await Assert.That(fixture.ReturnResultType).IsEqualTo(typeof(IApiResponse<string>));
        await Assert.That(fixture.DeserializedResultType).IsEqualTo(typeof(string));
    }
}
