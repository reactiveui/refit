// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>API surface with public generic methods used for method-key equality tests.</summary>
public interface IGenericMethodKeyFixture
{
    /// <summary>Generic method fixture used to obtain an open method definition.</summary>
    /// <typeparam name="T1">The first generic argument.</typeparam>
    /// <typeparam name="T2">The second generic argument.</typeparam>
    /// <param name="first">The first value.</param>
    /// <param name="second">The second value.</param>
    void GenericMethod<T1, T2>(T1 first, T2 second);

    /// <summary>Second generic method fixture used to verify method mismatches.</summary>
    /// <typeparam name="T1">The first generic argument.</typeparam>
    /// <typeparam name="T2">The second generic argument.</typeparam>
    /// <param name="first">The first value.</param>
    /// <param name="second">The second value.</param>
    void OtherGenericMethod<T1, T2>(T1 first, T2 second);
}
