// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Receives formatted enumerable query values so they can be appended in place without an intermediate sequence
/// or iterator state machine.</summary>
internal interface IQueryValueSink
{
    /// <summary>Appends one formatted value.</summary>
    /// <param name="value">The formatted value, or <see langword="null"/>.</param>
    void Add(string? value);
}
