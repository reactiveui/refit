// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json.Serialization;

namespace Refit.Tests;

/// <summary>A row of a dataset query result returned by <see cref="IDataMosApi"/>.</summary>
/// <typeparam name="TResultRow">The type of the row value.</typeparam>
public class DatasetQueryItem<TResultRow>
    where TResultRow : class, new()
{
    /// <summary>Gets or sets the global identifier of the row.</summary>
    [JsonPropertyName("global_id")]
    public long GlobalId { get; set; }

    /// <summary>Gets or sets the row number.</summary>
    public long Number { get; set; }

    /// <summary>Gets or sets the row value.</summary>
    [JsonPropertyName("Cells")]
    public required TResultRow Value { get; set; }
}
