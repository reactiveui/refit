// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json;
using Refit.Testing;

namespace Refit.Documentation;

/// <summary>Owns the shared local handler, client and generated serializer used by request scenarios.</summary>
internal sealed class SampleHost : IDisposable
{
    /// <summary>Reusable JSON metadata for the shared person and collection bodies.</summary>
    private static readonly JsonSerializerOptions JsonOptions = new(SampleJsonContext.Default.Options) { TypeInfoResolver = SampleJsonContext.Default };

    /// <summary>The settings whose serializer is adopted by this host's handler.</summary>
    private readonly RefitSettings _settings = new(new SystemTextJsonContentSerializer(JsonOptions));

    /// <summary>Initializes a new instance of the <see cref="SampleHost"/> class.</summary>
    internal SampleHost()
    {
        Http = new();
        _ = Http.ToSettings(_settings);
        Client = new(Http) { BaseAddress = new("https://people.example") };
    }

    /// <summary>Gets the handler where scenarios add expectations and inspect requests.</summary>
    internal StubHttp Http { get; }

    /// <summary>Gets the client owned by this host.</summary>
    internal HttpClient Client { get; }

    /// <summary>Gets the shared settings with generated JSON metadata.</summary>
    internal RefitSettings Settings => _settings;

    /// <summary>Disposes the client and its handler.</summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public void Dispose() => Client.Dispose();
}
