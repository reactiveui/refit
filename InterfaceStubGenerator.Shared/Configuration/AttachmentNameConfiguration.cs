namespace Refit.Generator.Configuration;

public class AttachmentNameConfiguration(string name)
{
    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    /// <value>
    /// The name.
    /// </value>
    public string Name { get; protected set; } = name;
}
