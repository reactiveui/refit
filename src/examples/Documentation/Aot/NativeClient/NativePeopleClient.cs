// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json;

namespace Refit.Documentation;

/// <summary>Owns generated person clients with local HTTP replies: one from a JSON context and one from settings the app builds.</summary>
internal sealed class NativePeopleClient : IDisposable
{
    /// <summary>The local HTTP client owned by this generated-client wrapper.</summary>
    private readonly HttpClient _client = new(new LocalHandler()) { BaseAddress = new("https://people.example") };

    /// <summary>Initializes a new instance of the <see cref="NativePeopleClient"/> class.</summary>
    internal NativePeopleClient()
    {
        Api = RestService.ForGenerated<IPeopleApi>(_client, SampleJsonContext.Default);

        JsonSerializerOptions options = new(SampleJsonContext.Default.Options) { TypeInfoResolver = SampleJsonContext.Default };
        RefitSettings settings = new(new SystemTextJsonContentSerializer(options));
        SettingsApi = RestService.ForGenerated<IPeopleApi>(_client, settings);
    }

    /// <summary>Gets the generated interface implementation that runs on the JSON context.</summary>
    internal IPeopleApi Api { get; }

    /// <summary>Gets the generated interface implementation that runs on settings built from the JSON context's options.</summary>
    internal IPeopleApi SettingsApi { get; }

    /// <summary>Disposes the HTTP client and its local handler.</summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public void Dispose() => _client.Dispose();
}
