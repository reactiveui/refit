// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>One pending query-string entry collected while building a request.</summary>
/// <param name="Key">The query key, unescaped.</param>
/// <param name="Value">The formatted value, or <see langword="null"/> to omit the entry.</param>
internal readonly record struct QueryParameterEntry(string Key, string? Value);
