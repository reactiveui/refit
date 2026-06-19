// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Threading.Tasks;
using Refit.Tests.SomeNamespace;

namespace Refit.Tests;

/// <summary>Refit API used to verify handling of global-aliased using directives.</summary>
public interface INamespaceWithGlobalAliasApi
{
    /// <summary>Sends a request returning a type from an aliased namespace.</summary>
    /// <returns>A task that resolves to the aliased-namespace type.</returns>
    [Get("/")]
    Task<SomeType> SomeRequest();
}
