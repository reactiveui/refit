namespace Refit.Generator.Configuration;

public class AuthorizeConfiguration(string scheme = "Bearer") : Attribute
{
    /// <summary>
    /// Gets the scheme.
    /// </summary>
    /// <value>
    /// The scheme.
    /// </value>
    public string Scheme { get; } = scheme;
}
