// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Xml.Serialization;

namespace Refit;

/// <summary>Settings that control how <see cref="XmlContentSerializer"/> serializes and deserializes XML content.</summary>
public class XmlContentSerializerSettings
{
    /// <summary>Initializes a new instance of the <see cref="XmlContentSerializerSettings"/> class.</summary>
    public XmlContentSerializerSettings()
    {
        XmlDefaultNamespace = null;
        XmlReaderWriterSettings = new();
        XmlNamespaces = new(
            [new(string.Empty, string.Empty)]);

        XmlAttributeOverrides = new();
    }

    /// <summary>Gets or sets the XML default namespace.</summary>
    /// <value>
    /// The XML default namespace.
    /// </value>
    public string? XmlDefaultNamespace { get; set; }

    /// <summary>Gets or sets the XML reader writer settings.</summary>
    /// <value>
    /// The XML reader writer settings.
    /// </value>
    public XmlReaderWriterSettings XmlReaderWriterSettings { get; set; }

    /// <summary>Gets or sets the XML namespaces.</summary>
    /// <value>
    /// The XML namespaces.
    /// </value>
    public XmlSerializerNamespaces XmlNamespaces { get; set; }

    /// <summary>Gets or sets the XML attribute overrides.</summary>
    /// <value>
    /// The XML attribute overrides.
    /// </value>
    public XmlAttributeOverrides XmlAttributeOverrides { get; set; }
}
