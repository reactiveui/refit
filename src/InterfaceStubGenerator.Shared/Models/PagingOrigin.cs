// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Generator;

/// <summary>Classifies which origins a followed next-page link may point at.</summary>
internal enum PagingOrigin
{
    /// <summary>The method follows no links.</summary>
    None = 0,

    /// <summary>Only the listed origins are allowed.</summary>
    Listed = 1,

    /// <summary>Only the origin of the client's base address is allowed.</summary>
    SameAsClient = 2,

    /// <summary>Any http or https origin is allowed.</summary>
    Any = 3,
}
