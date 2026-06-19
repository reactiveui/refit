// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>Refit fixture interface exercising default, private, internal and static interface members.</summary>
public interface IHaveDims
{
    // DIMs require C# 8.0 which requires .NET Core 3.x or .NET Standard 2.1
#if NETCOREAPP3_1_OR_GREATER
    /// <summary>Returns a constant identifying name via a static interface method.</summary>
    /// <returns>The name of the interface.</returns>
    [SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "Fixture intentionally exercises a static interface method to verify default interface member support.")]
    static string GetStatic() => nameof(IHaveDims);

    /// <summary>Performs a request through a default interface method.</summary>
    /// <returns>The response body text.</returns>
    Task<string> GetDim() => GetPrivate();
#endif

    /// <summary>Performs a GET request through an internal interface member.</summary>
    /// <returns>The response body text.</returns>
    [Get("")]
    internal Task<string> GetInternal();

    // DIMs require C# 8.0 which requires .NET Core 3.x or .NET Standard 2.1
#if NETCOREAPP3_1_OR_GREATER
    /// <summary>Performs a request through a private interface method.</summary>
    /// <returns>The response body text.</returns>
    private Task<string> GetPrivate() => GetInternal();
#endif
}
