// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Globalization;

namespace Refit.Tests;

/// <summary>A domain value whose default <see cref="IFormattable"/> rendering differs from its custom URL rendering.</summary>
/// <remarks>Implementing <see cref="IFormattable"/> makes both request builders treat it as a URL scalar rather than an
/// object to flatten, so the whole value passes through the URL parameter formatter.</remarks>
public readonly struct Temperature : IFormattable, IEquatable<Temperature>
{
    /// <summary>Initializes a new instance of the <see cref="Temperature"/> struct.</summary>
    /// <param name="celsius">The temperature in degrees Celsius.</param>
    public Temperature(int celsius) => Celsius = celsius;

    /// <summary>Gets the temperature in degrees Celsius.</summary>
    public int Celsius { get; }

    /// <summary>Determines whether two temperatures are equal.</summary>
    /// <param name="left">The first temperature.</param>
    /// <param name="right">The second temperature.</param>
    /// <returns><see langword="true"/> when both temperatures are equal.</returns>
    public static bool operator ==(Temperature left, Temperature right) => left.Equals(right);

    /// <summary>Determines whether two temperatures are not equal.</summary>
    /// <param name="left">The first temperature.</param>
    /// <param name="right">The second temperature.</param>
    /// <returns><see langword="true"/> when the temperatures differ.</returns>
    public static bool operator !=(Temperature left, Temperature right) => !left.Equals(right);

    /// <summary>Renders the temperature as its invariant Celsius number, the default formatter's output.</summary>
    /// <param name="format">The format string, ignored.</param>
    /// <param name="formatProvider">The format provider used to render the number.</param>
    /// <returns>The Celsius value as a string.</returns>
    public string ToString(string? format, IFormatProvider? formatProvider) =>
        Celsius.ToString(formatProvider ?? CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public override string ToString() => ToString(null, CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public bool Equals(Temperature other) => Celsius == other.Celsius;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Temperature other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Celsius;
}
