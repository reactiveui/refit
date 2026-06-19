// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Threading.Tasks;
using Refit.Tests.ModelNamespace;

namespace Refit.Tests;

/// <summary>Refit API used to verify reduced using directives declared inside a namespace.</summary>
public interface IReducedUsingInsideNamespaceApi
{
    /// <summary>Sends a request returning a type from the model namespace.</summary>
    /// <returns>A task that resolves to the model-namespace type.</returns>
    [Get("/")]
    Task<SomeType> SomeRequest();
}
