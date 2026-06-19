// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Generator;

/// <summary>Classifies how a method parameter participates in generated request construction.</summary>
internal enum RequestParameterKind
{
    /// <summary>The parameter is not yet supported by generated request construction.</summary>
    Unsupported,

    /// <summary>The parameter supplies the request body.</summary>
    Body,

    /// <summary>The parameter supplies one dynamic request header.</summary>
    Header,

    /// <summary>The parameter supplies a collection of dynamic request headers.</summary>
    HeaderCollection,

    /// <summary>The parameter supplies one request property/option value.</summary>
    Property,

    /// <summary>The parameter supplies the request cancellation token.</summary>
    CancellationToken
}
