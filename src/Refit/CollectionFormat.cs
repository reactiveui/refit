// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit;

/// <summary>Collection format defined in https://swagger.io/docs/specification/2-0/describing-parameters/ .</summary>
public enum CollectionFormat
{
    /// <summary>Values formatted with <see cref="RefitSettings.UrlParameterFormatter"/> or <see cref="RefitSettings.FormUrlEncodedParameterFormatter"/>.</summary>
    RefitParameterFormatter,

    /// <summary>Comma-separated values.</summary>
    Csv,

    /// <summary>Space-separated values.</summary>
    Ssv,

    /// <summary>Tab-separated values.</summary>
    Tsv,

    /// <summary>Pipe-separated values.</summary>
    Pipes,

    /// <summary>Multiple parameter instances.</summary>
    Multi
}
