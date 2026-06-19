// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.GeneratorTests;

/// <summary>Generator tests covering the return types that Refit interface methods can declare.</summary>
public class ReturnTypeTests
{
    /// <summary>Verifies that a generic task return type generates correctly.</summary>
    /// <returns>A task representing the asynchronous verification.</returns>
    [Test]
    public Task GenericTaskShouldWork() =>
        Fixture.VerifyForBody(
            """
            [Get("/users")]
            Task<string> Get();
            """);

    /// <summary>Verifies that a nullable reference return type generates correctly.</summary>
    /// <returns>A task representing the asynchronous verification.</returns>
    [Test]
    public Task ReturnNullableObject() =>
        Fixture.VerifyForBody(
            """
            [Get("/users")]
            Task<string?> Get();
            """);

    /// <summary>Verifies that a nullable value type return type generates correctly.</summary>
    /// <returns>A task representing the asynchronous verification.</returns>
    [Test]
    public Task ReturnNullableValueType() =>
        Fixture.VerifyForBody(
            """
            [Get("/users")]
            Task<int?> Get();
            """);

    /// <summary>Verifies that a non-generic task return type generates correctly.</summary>
    /// <returns>A task representing the asynchronous verification.</returns>
    [Test]
    public Task VoidTaskShouldWork() =>
        Fixture.VerifyForBody(
            """
            [Post("/users")]
            Task Post();
            """);

    /// <summary>Verifies that a generic value task return type generates correctly.</summary>
    /// <returns>A task representing the asynchronous verification.</returns>
    [Test]
    public Task GenericValueTaskShouldWork() =>
        Fixture.VerifyForBody(
            """
            [Get("/users")]
            ValueTask<string> Get();
            """);

    /// <summary>Verifies that a value task wrapping an API response generates correctly.</summary>
    /// <returns>A task representing the asynchronous verification.</returns>
    [Test]
    public Task ValueTaskApiResponseShouldWork() =>
        Fixture.VerifyForBody(
            """
            [Get("/users")]
            ValueTask<ApiResponse<string>> Get();
            """);

    /// <summary>Verifies that a method with class, interface and new constraints generates correctly.</summary>
    /// <returns>A task representing the asynchronous verification.</returns>
    [Test]
    public Task GenericConstraintReturnTask() =>
        Fixture.VerifyForBody(
            """
            [Get("/users")]
            Task<string> Get<T>() where T : class, IDisposable, new();
            """);

    /// <summary>Verifies that a method with an unmanaged constraint generates correctly.</summary>
    /// <returns>A task representing the asynchronous verification.</returns>
    [Test]
    public Task GenericUnmanagedConstraintReturnTask() =>
        Fixture.VerifyForBody(
            """
            [Get("/users")]
            Task<string> Get<T>() where T : unmanaged;
            """);

    /// <summary>Verifies that a method with a struct constraint generates correctly.</summary>
    /// <returns>A task representing the asynchronous verification.</returns>
    [Test]
    public Task GenericStructConstraintReturnTask() =>
        Fixture.VerifyForBody(
            """
            [Get("/users")]
            Task<string> Get<T>() where T : struct
            """);

    /// <summary>Verifies that an observable return type generates correctly.</summary>
    /// <returns>A task representing the asynchronous verification.</returns>
    [Test]
    public Task ReturnIObservable() =>
        Fixture.VerifyForBody(
            """
            [Get("/users/{user}")]
            IObservable<HttpResponseMessage> GetUser(string user);
            """);

    /// <summary>Verifies that an unsupported synchronous return type is reported.</summary>
    /// <returns>A task representing the asynchronous verification.</returns>
    [Test]
    public Task ReturnUnsupportedType() =>
        Fixture.VerifyForBody(
            """
            [Get("/users/{user}")]
            string GetUser(string user);
            """);
}
