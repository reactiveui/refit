// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Refit;

/// <summary>A <see cref="JsonNamingPolicy"/> that renders property names in a lower-case, separator-delimited form across all target frameworks.</summary>
/// <param name="separator">The character inserted between words.</param>
internal sealed class SeparatedCaseJsonNamingPolicy(char separator) : JsonNamingPolicy
{
    /// <summary>Gets a policy that renders names in snake_case.</summary>
    internal static JsonNamingPolicy Snake { get; } = new SeparatedCaseJsonNamingPolicy('_');

    /// <summary>Gets a policy that renders names in kebab-case.</summary>
    internal static JsonNamingPolicy Kebab { get; } = new SeparatedCaseJsonNamingPolicy('-');

    /// <inheritdoc/>
    public override string ConvertName(string name) => SeparatedCaseFormatter.Format(name, separator);
}
