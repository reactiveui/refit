// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Provides a mechanism for formatting URL parameter keys, allowing customization of key naming conventions.</summary>
public interface IUrlParameterKeyFormatter
{
    /// <summary>Formats the specified key.</summary>
    /// <param name="key">The key.</param>
    /// <returns>The formatted key.</returns>
    string Format(string key);
}
