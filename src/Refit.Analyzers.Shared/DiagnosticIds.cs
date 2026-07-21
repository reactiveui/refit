// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Analyzers;

/// <summary>Diagnostic identifiers reported by Refit analyzers.</summary>
internal static class DiagnosticIds
{
    /// <summary>Diagnostic for a Refit member missing a valid HTTP method attribute.</summary>
    internal const string InvalidRefitMember = "RF001";

    /// <summary>Diagnostic reported when a Refit route contains a backslash.</summary>
    internal const string InvalidRouteBackslash = "RF003";

    /// <summary>Diagnostic reported when a Refit method has more than one cancellation token.</summary>
    internal const string MultipleCancellationTokens = "RF004";

    /// <summary>Diagnostic reported when a header collection parameter uses an unsupported type.</summary>
    internal const string InvalidHeaderCollectionParameter = "RF005";

    /// <summary>Diagnostic reported when a Refit method falls back to the reflection request builder and is not compatible with generated-only registration.</summary>
    internal const string GeneratedRequestBuildingFallback = "RF006";

    /// <summary>Diagnostic reported when a Refit method declares more than one <c>[HeaderCollection]</c> parameter.</summary>
    internal const string MultipleHeaderCollections = "RF008";

    /// <summary>Diagnostic reported when a Refit method declares more than one <c>[Authorize]</c> parameter.</summary>
    internal const string MultipleAuthorizeParameters = "RF009";

    /// <summary>Diagnostic reported when a Refit method declares more than one <c>[Body]</c> parameter.</summary>
    internal const string MultipleBodyParameters = "RF011";

    /// <summary>Diagnostic reported when a multipart Refit method also declares a <c>[Body]</c> parameter.</summary>
    internal const string MultipartBodyParameter = "RF012";
}
