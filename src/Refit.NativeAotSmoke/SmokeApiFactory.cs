// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Refit.NativeAotSmoke;

/// <summary>Builds the Refit API used by the native AOT smoke test.</summary>
internal static class SmokeApiFactory
{
    /// <summary>Explains why the trimming warning is suppressed for the generator-backed call.</summary>
    private const string TrimSafeJustification =
        "The Refit source generator (referenced as an analyzer) registers a generated factory for "
        + "INativeAotApi, so RestService.For resolves it without reflection. This project builds with "
        + "PublishAot to prove that path stays AOT-safe.";

    /// <summary>Explains why the AOT warning is suppressed for the generator-backed call.</summary>
    private const string AotSafeJustification =
        "The Refit source generator (referenced as an analyzer) emits the request builder for "
        + "INativeAotApi, so no runtime code generation occurs. This project builds with PublishAot "
        + "to prove that path stays AOT-safe.";

    /// <summary>Creates the <see cref="INativeAotApi"/> implementation backed by the source generator.</summary>
    /// <param name="client">The <see cref="HttpClient"/> the implementation will use.</param>
    /// <param name="jsonOptions">The source-generated JSON serializer options.</param>
    /// <returns>An AOT-safe implementation of <see cref="INativeAotApi"/>.</returns>
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:RequiresUnreferencedCode",
        Justification = TrimSafeJustification)]
    [UnconditionalSuppressMessage(
        "AOT",
        "IL3050:RequiresDynamicCode",
        Justification = AotSafeJustification)]
    public static INativeAotApi Create(HttpClient client, JsonSerializerOptions jsonOptions) =>
        RestService.For<INativeAotApi>(
            client,
            new RefitSettings(new SystemTextJsonContentSerializer(jsonOptions)));
}
