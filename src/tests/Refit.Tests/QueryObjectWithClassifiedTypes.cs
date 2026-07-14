// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Globalization;

namespace Refit.Tests;

/// <summary>A query object whose property types span the classifier's simple and formattable cases.</summary>
public sealed class QueryObjectWithClassifiedTypes
{
    /// <summary>Gets or sets a string property.</summary>
    public string? Text { get; set; }

    /// <summary>Gets or sets a boolean property.</summary>
    public bool Flag { get; set; }

    /// <summary>Gets or sets a character property.</summary>
    public char Symbol { get; set; }

    /// <summary>Gets or sets a <see cref="Uri"/> property.</summary>
    public Uri? Link { get; set; }

    /// <summary>Gets or sets a <see cref="CultureInfo"/> property.</summary>
    public CultureInfo? Culture { get; set; }

    /// <summary>Gets or sets a nullable value-type property.</summary>
    public int? OptionalNumber { get; set; }

    /// <summary>Gets or sets a plain formattable value-type property.</summary>
    public int Number { get; set; }
}
