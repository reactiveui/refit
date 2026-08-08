// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Generator;

/// <summary>Classifies how a method parameter participates in generated request construction.</summary>
internal enum RequestParameterKind
{
    /// <summary>The parameter is not yet supported by generated request construction.</summary>
    Unsupported = 0,

    /// <summary>The parameter supplies the request body.</summary>
    Body = 1,

    /// <summary>The parameter supplies one dynamic request header.</summary>
    Header = 2,

    /// <summary>The parameter supplies a collection of dynamic request headers.</summary>
    HeaderCollection = 3,

    /// <summary>The parameter supplies one request property/option value.</summary>
    Property = 4,

    /// <summary>The parameter supplies the request cancellation token.</summary>
    CancellationToken = 5,

    /// <summary>The parameter supplies a value for a placeholder in the path.</summary>
    Path = 6,

    /// <summary>The parameter supplies the complete absolute request URI, bypassing the client base address.</summary>
    Url = 7,

    /// <summary>The parameter supplies one or more query string values or flags.</summary>
    Query = 8,

    /// <summary>The parameter supplies one (or, for an enumerable, each) part of a multipart form body.</summary>
    MultipartPart = 9,
}
