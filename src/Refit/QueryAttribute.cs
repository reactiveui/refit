// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>
/// Associated value will be added to the request Uri as query-string, using a delimiter to split the values. (default: '.').
/// </summary>
[AttributeUsage(AttributeTargets.Parameter |
                AttributeTargets.Property)] // Property is to allow for form url encoded data
public sealed class QueryAttribute : Attribute
{
    /// <summary>The collection format, or <see langword="null"/> when not explicitly specified.</summary>
    private CollectionFormat? _collectionFormat;

    /// <summary>Initializes a new instance of the <see cref="QueryAttribute"/> class.</summary>
    public QueryAttribute()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="QueryAttribute"/> class.</summary>
    /// <param name="delimiter">The delimiter.</param>
    public QueryAttribute(string delimiter) => Delimiter = delimiter;

    /// <summary>Initializes a new instance of the <see cref="QueryAttribute"/> class.</summary>
    /// <param name="delimiter">The delimiter.</param>
    /// <param name="prefix">The prefix.</param>
    public QueryAttribute(string delimiter, string prefix)
    {
        Delimiter = delimiter;
        Prefix = prefix;
    }

    /// <summary>Initializes a new instance of the <see cref="QueryAttribute"/> class.</summary>
    /// <param name="delimiter">The delimiter.</param>
    /// <param name="prefix">The prefix.</param>
    /// <param name="format">The format.</param>
    public QueryAttribute(string delimiter, string prefix, string format)
    {
        Delimiter = delimiter;
        Prefix = prefix;
        Format = format;
    }

    /// <summary>Initializes a new instance of the <see cref="QueryAttribute"/> class.</summary>
    /// <param name="collectionFormat">The collection format.</param>
    public QueryAttribute(CollectionFormat collectionFormat) => CollectionFormat = collectionFormat;

    /// <summary>
    /// Gets or sets a value indicating whether the value should be treated as a string.
    /// Set to true if you want to call ToString() on the object before adding it to the query string.
    /// </summary>
    public bool TreatAsString { get; set; }

    /// <summary>Gets the value used to customize the name of either the query parameter pair or of the form field when form encoding.</summary>
    /// <seealso cref="Prefix"/>
    public string Delimiter { get; } = ".";

    /// <summary>Gets the value used to customize the name of the encoded value.</summary>
    /// <remarks>
    /// Gets combined with <see cref="Delimiter"/> in the format <c>var name = $"{Prefix}{Delimiter}{originalFieldName}"</c>
    /// where <c>originalFieldName</c> is the name of the object property or method parameter.
    /// </remarks>
    /// <example>
    /// <code>
    /// class Form
    /// {
    ///   [Query("-", "dontlog")]
    ///   public string password { get; }
    /// }
    /// </code>
    /// will result in the encoded form having a field named <c>dontlog-password</c>.
    /// </example>
    public string? Prefix { get; }

    /// <summary>Gets or sets the value used to customize the formatting of the encoded value.</summary>
    /// <example>
    /// <code>
    /// interface IServerApi
    /// {
    ///   [Get("/expenses")]
    ///   Task addExpense([Query(Format="0.00")] double expense);
    /// }
    /// </code>
    /// Calling <c>serverApi.addExpense(5)</c> will result in a URI of <c>{baseUri}/expenses?expense=5.00</c>.
    /// </example>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA1019:Define accessors for attribute arguments",
        Justification = "The public setter is required so the value can also be supplied as a named attribute argument.")]
    public string? Format { get; set; }

    /// <summary>Gets or sets how the collection should be encoded.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA1019:Define accessors for attribute arguments",
        Justification = "The public setter is required so the value can also be supplied as a named attribute argument.")]
    public CollectionFormat CollectionFormat
    {
        // Cannot make property nullable due to Attribute restrictions
        get => _collectionFormat.GetValueOrDefault();
        set => _collectionFormat = value;
    }

    /// <summary>Gets a value indicating whether this instance is collection format specified.</summary>
    /// <value>
    ///   <c>true</c> if this instance is collection format specified; otherwise, <c>false</c>.
    /// </value>
    public bool IsCollectionFormatSpecified => _collectionFormat.HasValue;
}
