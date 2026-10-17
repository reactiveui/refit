// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
#if NET8_0_OR_GREATER
namespace Refit.Testing;

/// <summary>A local HTTP transport with a retry-handler factory for deterministic tests.</summary>
public sealed partial class StubHttp
{
    /// <summary>Wraps this stub in the real retry handler using default options and this stub's clock.</summary>
    /// <returns>A handler whose disposal also disposes this stub.</returns>
    public HttpRetryHandler CreateRetryHandler() => CreateRetryHandler(new() { TimeProvider = TimeProvider });

    /// <summary>Wraps this stub in the real retry handler using explicit retry options and their clock.</summary>
    /// <param name="options">The retry configuration.</param>
    /// <returns>A handler for an HttpClient or RefitSettings handler factory. Disposing it also disposes this stub.</returns>
    public HttpRetryHandler CreateRetryHandler(HttpRetryOptions options) => new(options) { InnerHandler = this };
}
#endif
