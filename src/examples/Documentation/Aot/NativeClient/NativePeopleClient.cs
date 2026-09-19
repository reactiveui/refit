// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json;

namespace Refit.Documentation;

/// <summary>Owns a generated person client with local HTTP replies and registered JSON metadata.</summary>
internal sealed class NativePeopleClient : IDisposable
{
    /// <summary>The local HTTP client owned by this generated-client wrapper.</summary>
    private readonly HttpClient _client = new(new LocalHandler()) { BaseAddress = new("https://people.example") };

    /// <summary>Initializes a new instance of the <see cref="NativePeopleClient"/> class.</summary>
    internal NativePeopleClient()
    {
        JsonSerializerOptions options = new(SampleJsonContext.Default.Options) { TypeInfoResolver = SampleJsonContext.Default };
        RefitSettings settings = new(new SystemTextJsonContentSerializer(options));
        Api = RestService.ForGenerated<IPeopleApi>(_client, settings);
    }

    /// <summary>Gets the generated interface implementation.</summary>
    internal IPeopleApi Api { get; }

    /// <summary>Disposes the HTTP client and its local handler.</summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public void Dispose() => _client.Dispose();
}
