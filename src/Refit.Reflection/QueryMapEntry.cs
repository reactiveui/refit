// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>One flattened key/value produced while expanding an object into query-map entries.</summary>
/// <param name="Key">The flattened query key, before prefixing.</param>
/// <param name="Value">The raw property or element value.</param>
internal readonly record struct QueryMapEntry(string Key, object? Value);
