namespace Refit.Generator.Configuration;

public class QueryConfiguration
{
    CollectionFormat? collectionFormat;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryAttribute"/> class.
    /// </summary>
    public QueryConfiguration() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryAttribute"/> class.
    /// </summary>
    /// <param name="delimiter">The delimiter.</param>
    public QueryConfiguration(string delimiter)
    {
        Delimiter = delimiter;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryAttribute"/> class.
    /// </summary>
    /// <param name="delimiter">The delimiter.</param>
    /// <param name="prefix">The prefix.</param>
    public QueryConfiguration(string delimiter, string prefix)
    {
        Delimiter = delimiter;
        Prefix = prefix;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryAttribute"/> class.
    /// </summary>
    /// <param name="delimiter">The delimiter.</param>
    /// <param name="prefix">The prefix.</param>
    /// <param name="format">The format.</param>
    public QueryConfiguration(string delimiter, string prefix, string format)
    {
        Delimiter = delimiter;
        Prefix = prefix;
        Format = format;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryAttribute"/> class.
    /// </summary>
    /// <param name="collectionFormat">The collection format.</param>
    public QueryConfiguration(CollectionFormat collectionFormat)
    {
        CollectionFormat = collectionFormat;
    }

    /// <summary>
    /// Used to customize the name of either the query parameter pair or of the form field when form encoding.
    /// </summary>
    /// <seealso cref="Prefix"/>
    public string Delimiter { get; protected set; } = ".";

    /// <summary>
    /// Used to customize the name of the encoded value.
    /// </summary>
    /// <remarks>
    /// Gets combined with <see cref="Delimiter"/> in the format <code>var name = $"{Prefix}{Delimiter}{originalFieldName}"</code>
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
    public string? Prefix { get; protected set; }

#pragma warning disable CA1019 // Define accessors for attribute arguments

    /// <summary>
    /// Used to customize the formatting of the encoded value.
    /// </summary>
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
    public string? Format { get; set; }

    /// <summary>
    /// Specifies how the collection should be encoded.
    /// </summary>
    public CollectionFormat CollectionFormat
    {
        // Cannot make property nullable due to Attribute restrictions
        get => collectionFormat.GetValueOrDefault();
        set => collectionFormat = value;
    }

#pragma warning restore CA1019 // Define accessors for attribute arguments

    /// <summary>
    /// Gets a value indicating whether this instance is collection format specified.
    /// </summary>
    /// <value>
    ///   <c>true</c> if this instance is collection format specified; otherwise, <c>false</c>.
    /// </value>
    public bool IsCollectionFormatSpecified => collectionFormat.HasValue;
}
