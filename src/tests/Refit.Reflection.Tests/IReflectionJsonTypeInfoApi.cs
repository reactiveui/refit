// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json.Serialization.Metadata;

namespace Refit.Reflection.Tests;

/// <summary>An interface with no Refit attributes whose methods take JSON metadata or another generic parameter.</summary>
public interface IReflectionJsonTypeInfoApi
{
    /// <summary>Gets a value with metadata the reflection request builder cannot pass on.</summary>
    /// <param name="typeInfo">The metadata for the value.</param>
    /// <returns>The value.</returns>
    Task<string> GetValue(JsonTypeInfo<string> typeInfo);

    /// <summary>Gets a value with an ordinary generic parameter.</summary>
    /// <param name="ids">The identifiers to look up.</param>
    /// <returns>The value.</returns>
    Task<string> GetValues(List<int> ids);
}
