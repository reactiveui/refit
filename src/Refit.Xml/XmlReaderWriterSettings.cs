// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Xml;

namespace Refit;

/// <summary>Holds the <see cref="XmlReaderSettings"/> and <see cref="XmlWriterSettings"/> used by <see cref="XmlContentSerializer"/>.</summary>
public class XmlReaderWriterSettings
{
    /// <summary>The backing field for the reader settings.</summary>
    private XmlReaderSettings _readerSettings;

    /// <summary>The backing field for the writer settings.</summary>
    private XmlWriterSettings _writerSettings;

    /// <summary>Initializes a new instance of the <see cref="XmlReaderWriterSettings"/> class.</summary>
    public XmlReaderWriterSettings()
        : this(new(), new())
    {
    }

    /// <summary>Initializes a new instance of the <see cref="XmlReaderWriterSettings"/> class.</summary>
    /// <param name="readerSettings">The reader settings.</param>
    public XmlReaderWriterSettings(XmlReaderSettings readerSettings)
        : this(readerSettings, new())
    {
    }

    /// <summary>Initializes a new instance of the <see cref="XmlReaderWriterSettings"/> class.</summary>
    /// <param name="writerSettings">The writer settings.</param>
    public XmlReaderWriterSettings(XmlWriterSettings writerSettings)
        : this(new(), writerSettings)
    {
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    /// <summary>Initializes a new instance of the <see cref="XmlReaderWriterSettings"/> class.</summary>
    /// <param name="readerSettings">The reader settings.</param>
    /// <param name="writerSettings">The writer settings.</param>
    public XmlReaderWriterSettings(
        XmlReaderSettings readerSettings,
        XmlWriterSettings writerSettings)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
        ReaderSettings = readerSettings;
        WriterSettings = writerSettings;
    }

    /// <summary>Gets or sets the reader settings.</summary>
    /// <value>
    /// The reader settings.
    /// </value>
    /// <exception cref="System.ArgumentNullException">value</exception>
    public XmlReaderSettings ReaderSettings
    {
        get
        {
            ApplyOverrideSettings();
            return _readerSettings;
        }
        set => _readerSettings = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Gets or sets the writer settings.</summary>
    /// <value>
    /// The writer settings.
    /// </value>
    /// <exception cref="System.ArgumentNullException">value</exception>
    public XmlWriterSettings WriterSettings
    {
        get
        {
            ApplyOverrideSettings();
            return _writerSettings;
        }
        set => _writerSettings = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// The writer and reader settings are set by the caller, but certain properties
    /// should remain set to meet the demands of the XmlContentSerializer. Those properties
    /// are always set here.
    /// </summary>
    private void ApplyOverrideSettings()
    {
        _writerSettings.Async = true;
        _readerSettings.Async = true;
    }
}
