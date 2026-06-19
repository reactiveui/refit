// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Default Url parameter key formatter. Does not do any formatting.</summary>
public class DefaultUrlParameterKeyFormatter : IUrlParameterKeyFormatter
{
    /// <summary>Formats the specified key.</summary>
    /// <param name="key">The key.</param>
    /// <returns>The unmodified key.</returns>
    public virtual string Format(string key) => key;
}
