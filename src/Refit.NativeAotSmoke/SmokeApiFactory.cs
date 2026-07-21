// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json;

namespace Refit.NativeAotSmoke;

/// <summary>Builds the Refit API used by the native AOT smoke test.</summary>
internal static class SmokeApiFactory
{
    /// <summary>Creates the <see cref="INativeAotApi"/> implementation backed by the source generator.</summary>
    /// <remarks>The generated-only entry point never touches the reflection request builder, so this project
    /// does not reference the Refit.Reflection package and needs no trim/AOT suppressions.</remarks>
    /// <param name="client">The <see cref="HttpClient"/> the implementation will use.</param>
    /// <param name="jsonOptions">The source-generated JSON serializer options.</param>
    /// <returns>An AOT-safe implementation of <see cref="INativeAotApi"/>.</returns>
    internal static INativeAotApi Create(HttpClient client, JsonSerializerOptions jsonOptions) =>
        RestService.ForGenerated<INativeAotApi>(
            client,
            new RefitSettings(new SystemTextJsonContentSerializer(jsonOptions)));
}
