// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;

namespace Refit;

/// <summary>Request builder used by generated-only clients.</summary>
internal sealed class GeneratedOnlyRequestBuilder : IRequestBuilder
{
    /// <summary>Initializes a new instance of the <see cref="GeneratedOnlyRequestBuilder"/> class.</summary>
    /// <param name="settings">The settings used by the generated client.</param>
    public GeneratedOnlyRequestBuilder(RefitSettings settings) => Settings = settings;

    /// <inheritdoc/>
    public RefitSettings Settings { get; }

    /// <inheritdoc/>
    [RequiresUnreferencedCode("Building request delegates from reflected method metadata requires generic method metadata to be available at runtime.")]
    [RequiresDynamicCode("Building request delegates from reflected method metadata requires runtime generic method instantiation.")]
    public Func<HttpClient, object[], object?> BuildRestResultFuncForMethod(
        string methodName,
        Type[]? parameterTypes = null,
        Type[]? genericArgumentTypes = null)
    {
        var methodContext =
            $"This Refit client was created with the generated-only API, but the generated client needs the reflection request builder for '{methodName}'.";
        throw new NotSupportedException(
            string.Concat(
                methodContext,
                " Enable generated request building for this method or use RestService.For when reflection is acceptable."));
    }
}
