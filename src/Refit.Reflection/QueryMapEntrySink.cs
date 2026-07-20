// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Appends formatted enumerable values as <see cref="QueryMapEntry"/> items under a fixed key.</summary>
/// <param name="Entries">The list receiving the entries.</param>
/// <param name="Key">The query key applied to every appended value.</param>
internal readonly record struct QueryMapEntrySink(List<QueryMapEntry> Entries, string Key) : IQueryValueSink
{
    /// <inheritdoc/>
    public void Add(string? value) => Entries.Add(new(Key, value));
}
