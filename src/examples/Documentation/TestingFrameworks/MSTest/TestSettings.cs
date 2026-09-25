// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json;

namespace Refit.Documentation.TestingFrameworks;

/// <summary>Builds the <see cref="RefitSettings"/> every test uses, so JSON matches the generated <see cref="Person"/> metadata.</summary>
public static class TestSettings
{
    /// <summary>The JSON options every test shares, built once from the source-generated <see cref="Person"/> metadata.</summary>
    private static readonly JsonSerializerOptions Options = new(PersonJsonContext.Default.Options) { TypeInfoResolver = PersonJsonContext.Default };

    /// <summary>Creates settings that read and write <see cref="Person"/> using the source-generated metadata.</summary>
    /// <returns>Settings ready to pass to a generated client.</returns>
    public static RefitSettings Create() => new(new SystemTextJsonContentSerializer(Options));
}
