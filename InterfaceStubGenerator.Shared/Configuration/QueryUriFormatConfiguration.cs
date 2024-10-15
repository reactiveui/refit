namespace Refit.Generator.Configuration;

public class QueryUriFormatConfiguration(UriFormat uriFormat)
{
    /// <summary>
    /// Specifies how the Query Params should be encoded.
    /// </summary>
    public UriFormat UriFormat { get; } = uriFormat;
}
