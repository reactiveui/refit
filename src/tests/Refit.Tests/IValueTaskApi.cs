// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A Refit interface returning <see cref="ValueTask{TResult}"/> results.</summary>
public interface IValueTaskApi
{
    /// <summary>Gets a value for the supplied key.</summary>
    /// <param name="value">The path value to retrieve.</param>
    /// <returns>The retrieved value.</returns>
    [Get("/{value}")]
    ValueTask<string> GetValue(string value);
}
