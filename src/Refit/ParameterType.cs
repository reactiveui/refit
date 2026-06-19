// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Specifies how a REST method parameter is treated.</summary>
public enum ParameterType
{
    /// <summary>The parameter is treated normally.</summary>
    Normal,

    /// <summary>The parameter is round-tripped.</summary>
    RoundTripping
}
