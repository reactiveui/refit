// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Tests;

/// <summary>A Refit interface fixture that is declared but never used, exercising generator discovery.</summary>
public interface IAmARefitInterfaceButNobodyUsesMe
{
    /// <summary>A simple GET method used as a generator fixture.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get("whatever")]
    Task RefitMethod();

    /// <summary>A GET method declared with the fully qualified attribute name.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Refit.GetAttribute("something-else")]
    Task AnotherRefitMethod();

    /// <summary>A GET method whose route comes from a referenced constant.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get(ThisIsDumbButMightHappen.PeopleDoWeirdStuff)]
    Task NoConstantsAllowed();

    /// <summary>A GET method whose route contains spaces, verifying the generator tolerates them.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get("spaces-shouldnt-break-me")]
    Task SpacesShouldntBreakMe();

    /// <summary>Verifies the generator handles parameters named with reserved keywords.</summary>
    /// <param name="int">A parameter named with the reserved word <c>int</c>.</param>
    /// <param name="string">A parameter named with the reserved word <c>string</c>.</param>
    /// <param name="long">A parameter named with the reserved word <c>long</c>.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get("anything")]
    Task ReservedWordsForParameterNames(int @int, string @string, float @long);
}
