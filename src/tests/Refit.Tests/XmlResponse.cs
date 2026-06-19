// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Xml.Serialization;

namespace Refit.Tests;

/// <summary>Response DTO used to exercise XML deserialization paths.</summary>
[XmlRoot("XmlResponse")]
public class XmlResponse
{
    /// <summary>Gets or sets the identifier carried by the XML response body.</summary>
    public string? Identifier { get; set; }
}
