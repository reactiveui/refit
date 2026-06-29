// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System;
using System.Diagnostics.CodeAnalysis;
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
        set
        {
            ArgumentExceptionHelper.ThrowIfNull(value);
            _readerSettings = value;
        }
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
        set
        {
            ArgumentExceptionHelper.ThrowIfNull(value);
            _writerSettings = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether DTD processing is left to the caller-supplied reader settings
    /// instead of being forced off (defaults to <see langword="false"/>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// By default Refit forces <see cref="DtdProcessing.Prohibit"/> and clears the <see cref="XmlReaderSettings.XmlResolver"/>
    /// on every access so a hostile XML response cannot trigger an XML External Entity (XXE) or entity-expansion
    /// ("billion laughs") attack. Setting this to <see langword="true"/> disables that safeguard and honors whatever
    /// <see cref="DtdProcessing"/>/<see cref="XmlReaderSettings.XmlResolver"/> you configured on <see cref="ReaderSettings"/>.
    /// </para>
    /// <para>
    /// This is intentionally marked obsolete: only enable it for XML from a fully trusted source, and prefer
    /// pre-processing untrusted documents instead.
    /// </para>
    /// </remarks>
    [Obsolete(
        "Enabling DTD processing re-opens XML External Entity (XXE) and entity-expansion attacks and is strongly "
        + "discouraged. Only set this for XML from a fully trusted source.")]
    [SuppressMessage(
        "Major Code Smell",
        "S1133:Do not forget to remove this deprecated code someday",
        Justification = "The attribute is a permanent, intentional warning on a security opt-out, not pending-removal deprecation.")]
    public bool AllowDtdProcessing { get; set; }

    /// <summary>
    /// The writer and reader settings are set by the caller, but certain properties
    /// should remain set to meet the demands of the XmlContentSerializer. Those properties
    /// are always set here.
    /// </summary>
    /// <remarks>
    /// Unless <see cref="AllowDtdProcessing"/> is enabled, DTD processing is forced off and the resolver is cleared
    /// on every access so that hostile XML responses cannot trigger XML External Entity (XXE) or entity-expansion
    /// ("billion laughs") attacks, regardless of any caller-supplied reader settings.
    /// </remarks>
    private void ApplyOverrideSettings()
    {
        _writerSettings.Async = true;
        _readerSettings.Async = true;

#pragma warning disable CS0618 // Reading our own intentionally-obsolete XXE opt-out.
        if (AllowDtdProcessing)
#pragma warning restore CS0618
        {
            return;
        }

        _readerSettings.DtdProcessing = DtdProcessing.Prohibit;
        _readerSettings.XmlResolver = null;
    }
}
