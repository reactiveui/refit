namespace Refit.Generator.Configuration;

public class HttpMethodConfiguration(string path)
{
    /// <summary>
    /// Gets the method.
    /// </summary>
    /// <value>
    /// The method.
    /// </value>
    public HttpMethod Method { get; set; }

    /// <summary>
    /// Gets or sets the path.
    /// </summary>
    /// <value>
    /// The path.
    /// </value>
    public virtual string Path { get; protected set; } = path;
}
