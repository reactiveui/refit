// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Provides an implementation of <see cref="IUrlParameterKeyFormatter"/> that formats URL parameter keys in snake_case.</summary>
public class SnakeCaseUrlParameterKeyFormatter : IUrlParameterKeyFormatter
{
    /// <summary>Formats the specified key.</summary>
    /// <param name="key">The key.</param>
    /// <returns>The snake_case form of the key.</returns>
    public string Format(string key) => SeparatedCaseFormatter.Format(key, '_');
}
