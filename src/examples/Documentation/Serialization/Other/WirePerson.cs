// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace Refit.Documentation;

/// <summary>Provides a public model that XML serialization can construct.</summary>
[XmlRoot("person")]
[System.Diagnostics.DebuggerDisplay("{Id}: {Name}")]
public sealed class WirePerson
{
    /// <summary>Gets or sets the service's person identifier.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the person's name using explicit wire names.</summary>
    [JsonProperty("display_name")]
    [XmlElement("display_name")]
    public string Name { get; set; } = string.Empty;
}
