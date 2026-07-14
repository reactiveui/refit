// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Refit;

/// <summary>Intentional fixture declared in the global namespace to test generation without a namespace.</summary>
[SuppressMessage(
    "Design",
    "SST2312:Types should be declared in a named namespace",
    Justification = "Fixture must stay in the global namespace; consumed as a source file by the generator no-namespace smoke test.")]
[SuppressMessage(
    "Design",
    "CA1050:Declare types in namespaces",
    Justification = "Fixture must stay in the global namespace; consumed as a source file by the generator no-namespace smoke test.")]
public interface IServiceWithoutNamespace
{
    /// <summary>Gets the root resource.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get("/")]
    Task GetRoot();

    /// <summary>Posts to the root resource.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Post("/")]
    Task PostRoot();
}
