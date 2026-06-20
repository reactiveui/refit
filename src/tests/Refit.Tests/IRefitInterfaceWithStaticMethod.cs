// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A Refit interface that exposes a C# 8 static factory method.</summary>
public interface IRefitInterfaceWithStaticMethod
{
#if NETCOREAPP3_1_OR_GREATER
    /// <summary>Creates an instance of the interface via a static factory method.</summary>
    /// <returns>A Refit-backed implementation of the interface.</returns>
    public static IRefitInterfaceWithStaticMethod Create() =>
        RestService.For<IRefitInterfaceWithStaticMethod>("http://foo/");
#endif

    /// <summary>Gets the configured endpoint.</summary>
    /// <returns>A task that completes when the request finishes.</returns>
    [Get("")]
    Task Get();
}
