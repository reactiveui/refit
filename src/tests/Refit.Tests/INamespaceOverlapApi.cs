// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Threading.Tasks;
using Common.Helper;
using Refit.Tests.Common;

namespace Refit.Tests;

/// <summary>Refit API used to verify behavior when namespaces overlap with Refit attribute namespaces.</summary>
[SomeHelper]
public interface INamespaceOverlapApi
{
    /// <summary>Sends a request returning a type from an overlapping namespace.</summary>
    /// <returns>A task that resolves to the overlapping-namespace type.</returns>
    [Get("/")]
    Task<SomeOtherType> SomeRequest();
}
