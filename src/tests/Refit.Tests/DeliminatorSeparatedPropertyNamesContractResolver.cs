// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Newtonsoft.Json.Serialization;

namespace Refit.Tests;

/// <summary>Contract resolver that joins PascalCase property name words with a configurable separator.</summary>
[RequiresUnreferencedCode("Refit's reflection-based serialization and request building are exercised by these tests.")]
[RequiresDynamicCode("Refit's reflection-based serialization and request building are exercised by these tests.")]
public class DeliminatorSeparatedPropertyNamesContractResolver : DefaultContractResolver
{
    /// <summary>The separator inserted between lowercased property name words.</summary>
    private readonly string _separator;

    /// <summary>Initializes a new instance of the <see cref="DeliminatorSeparatedPropertyNamesContractResolver"/> class.</summary>
    /// <param name="separator">The character placed between property name words.</param>
    protected DeliminatorSeparatedPropertyNamesContractResolver(char separator) => _separator = separator.ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    protected override string ResolvePropertyName(string propertyName)
    {
        var parts = new List<string>();
        var currentWord = new StringBuilder();

        foreach (var c in propertyName)
        {
            if (char.IsUpper(c) && currentWord.Length > 0)
            {
                parts.Add(currentWord.ToString());
                currentWord.Clear();
            }

            currentWord.Append(char.ToLower(c));
        }

        if (currentWord.Length > 0)
        {
            parts.Add(currentWord.ToString());
        }

        return string.Join(_separator, parts);
    }
}
