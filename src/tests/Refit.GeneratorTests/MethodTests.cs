// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.GeneratorTests;

/// <summary>Generator tests covering interface methods.</summary>
public class MethodTests
{
    /// <summary>Verifies generation for methods that declare generic constraints.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task MethodsWithGenericConstraints() =>
        Fixture.VerifyForBody(
            """
            [Get("/users")]
            Task<string> Get<T1, T2, T3, T4, T5>()
                where T1 : class
                where T2 : unmanaged
                where T3 : struct
                where T4 : notnull
                where T5 : class, IDisposable, new();

            void NonRefitMethod<T1, T2, T3, T4, T5>()
                where T1 : class
                where T2 : unmanaged
                where T3 : struct
                where T4 : notnull
                where T5 : class, IDisposable, new();
            """);
}
