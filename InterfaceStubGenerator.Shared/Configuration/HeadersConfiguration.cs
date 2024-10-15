#nullable enable
namespace Refit.Generator.Configuration;

public class HeadersConfiguration(params string[] headers) : Attribute
{
    /// <summary>
    /// Gets the headers.
    /// </summary>
    /// <value>
    /// The headers.
    /// </value>
    public string[] Headers { get; } = headers ?? [];
}
