// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>
/// Flattens a complex object whose properties are only known at run time into form entries. Core Refit has no
/// implementation; the Refit.Reflection package registers one when it loads.
/// </summary>
internal interface IFormObjectFlattener
{
    /// <summary>Adds the entries for an object source, composing nested keys under the given prefix.</summary>
    /// <param name="map">The form map the entries are added to.</param>
    /// <param name="source">The object source.</param>
    /// <param name="sourceType">The type whose readable public properties are flattened: the declared type at the top level, the run-time type when nested.</param>
    /// <param name="settings">The Refit settings controlling formatting.</param>
    /// <param name="keyPrefix">The parent key prefix, or <see langword="null"/> for a top-level object.</param>
    /// <param name="delimiter">The delimiter composing each property key under the prefix.</param>
    /// <param name="depth">The current nesting depth.</param>
    void AddObject(
        FormValueMultimap map,
        object source,
        Type sourceType,
        RefitSettings settings,
        string? keyPrefix,
        string? delimiter,
        int depth);
}
