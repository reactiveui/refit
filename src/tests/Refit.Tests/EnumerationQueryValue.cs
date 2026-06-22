// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A value-object style type whose string form comes from <see cref="ToString"/>, not its properties.</summary>
public sealed class EnumerationQueryValue
{
    /// <summary>Initializes a new instance of the <see cref="EnumerationQueryValue"/> class.</summary>
    /// <param name="key">The underlying key.</param>
    public EnumerationQueryValue(string key) => Key = key;

    /// <summary>Gets the underlying key.</summary>
    public string Key { get; }

    /// <inheritdoc/>
    public override string ToString() => Key;
}
