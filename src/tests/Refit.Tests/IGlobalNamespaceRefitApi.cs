// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

/// <summary>
/// A Refit-style interface declared in the global namespace, used to verify unique-name generation for
/// namespace-less types. It must have no namespace: unique-name generation reads <c>Type.Namespace</c>, and
/// its null-namespace branch is only reachable for a type without a namespace, so the namespace-placement
/// analyzers are suppressed here rather than worked around.
/// </summary>
[SuppressMessage(
    "Design",
    "CA1050:Declare types in namespaces",
    Justification = "Intentionally namespace-less so UniqueName.ForType's null-Namespace branch is reachable.")]
[SuppressMessage(
    "RoslynCommonAnalyzers",
    "SST2312:Move type into a named namespace",
    Justification = "Intentionally namespace-less so UniqueName.ForType's null-Namespace branch is reachable.")]
public interface IGlobalNamespaceRefitApi
{
    /// <summary>A placeholder member so the fixture is a non-empty interface.</summary>
    /// <returns>A task representing the request.</returns>
    Task GetAsync();
}
