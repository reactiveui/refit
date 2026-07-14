// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Refit.Tests;

/// <summary>A strongly-typed URL-encoded form payload.</summary>
public class GeneratedFormData
{
    /// <summary>Gets or sets the aliased user name.</summary>
    [AliasAs("user_name")]
    public string? UserName { get; set; }

    /// <summary>Gets or sets the password named via the JSON serializer attribute.</summary>
    [JsonPropertyName("pwd")]
    public string? Password { get; set; }

    /// <summary>Gets or sets a plain property.</summary>
    public string? Plain { get; set; }

    /// <summary>Gets or sets a nullable property serialized even when null.</summary>
    [Query(SerializeNull = true)]
    public string? Nullable { get; set; }

    /// <summary>Gets or sets an integer scalar rendered without boxing on the fast path.</summary>
    public int Age { get; set; }

    /// <summary>Gets or sets an enum scalar rendered by the generated formatter.</summary>
    public GeneratedFormColor Color { get; set; }

    /// <summary>Gets or sets a formatted numeric scalar.</summary>
    [Query(Format = "0.00")]
    public double Ratio { get; set; }

    /// <summary>Gets or sets a prefixed scalar composing its field name from the query prefix.</summary>
    [Query("-", "addr")]
    public string? City { get; set; }
}
