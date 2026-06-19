// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A Refit interface exercising URL fragment handling.</summary>
public interface IFragmentApi
{
    /// <summary>Gets with a named fragment.</summary>
    /// <returns>A task that completes when the request finishes.</returns>
    [Get("/foo#name")]
    Task Fragment();

    /// <summary>Gets with an empty fragment.</summary>
    /// <returns>A task that completes when the request finishes.</returns>
    [Get("/foo#")]
    Task EmptyFragment();

    /// <summary>Gets with multiple fragment markers.</summary>
    /// <returns>A task that completes when the request finishes.</returns>
    [Get("/foo#first#second")]
    Task ManyFragments();

    /// <summary>Gets with a parameter-mapped fragment.</summary>
    /// <param name="frag">The fragment value.</param>
    /// <returns>A task that completes when the request finishes.</returns>
    [Get("/foo#{frag}")]
    Task ParameterFragment(string frag);

    /// <summary>Gets with a fragment that follows a query string.</summary>
    /// <returns>A task that completes when the request finishes.</returns>
    [Get("/foo?key=value#name")]
    Task FragmentAfterQuery();

    /// <summary>Gets with a query string that follows a fragment marker.</summary>
    /// <returns>A task that completes when the request finishes.</returns>
    [Get("/foo#?key=value")]
    Task QueryAfterFragment();
}
