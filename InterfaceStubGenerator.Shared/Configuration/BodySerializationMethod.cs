#nullable enable
namespace Refit.Generator.Configuration;

/// <summary>
/// Defines methods to serialize HTTP requests' bodies.
/// </summary>
public enum BodySerializationMethod
{
    /// <summary>
    /// Encodes everything using the ContentSerializer in RefitSettings except for strings. Strings are set as-is
    /// </summary>
    Default = 0,

    /// <summary>
    /// Json encodes everything, including strings
    /// </summary>
    [Obsolete("Use BodySerializationMethod.Serialized instead", false)]
    Json = 1,

    /// <summary>
    /// Form-UrlEncode's the values
    /// </summary>
    UrlEncoded = 2,

    /// <summary>
    /// Encodes everything using the ContentSerializer in RefitSettings
    /// </summary>
    Serialized = 3
}
