// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Tests;

/// <summary>Fixture whose URL contains CR or LF characters, used to verify Refit rejects it.</summary>
public interface IUrlContainsCrlf
{
    /// <summary>Endpoint with a URL containing a carriage return.</summary>
    /// <returns>The response body.</returns>
    [Get("/\r")]
    Task<string> GetValue();
}
