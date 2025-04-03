namespace Refit.Generator.Configuration;

public class BodyConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BodyAttribute"/> class.
    /// </summary>
    public BodyConfiguration() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="BodyAttribute"/> class.
    /// </summary>
    /// <param name="buffered">if set to <c>true</c> [buffered].</param>
    public BodyConfiguration(bool buffered) => Buffered = buffered;

    /// <summary>
    /// Initializes a new instance of the <see cref="BodyAttribute"/> class.
    /// </summary>
    /// <param name="serializationMethod">The serialization method.</param>
    /// <param name="buffered">if set to <c>true</c> [buffered].</param>
    public BodyConfiguration(BodySerializationMethod serializationMethod, bool buffered)
    {
        SerializationMethod = serializationMethod;
        Buffered = buffered;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BodyAttribute"/> class.
    /// </summary>
    /// <param name="serializationMethod">The serialization method.</param>
    public BodyConfiguration(
        BodySerializationMethod serializationMethod = BodySerializationMethod.Default
    )
    {
        SerializationMethod = serializationMethod;
    }

    /// <summary>
    /// Gets or sets the buffered.
    /// </summary>
    /// <value>
    /// The buffered.
    /// </value>
    public bool? Buffered { get; }

    /// <summary>
    /// Gets or sets the serialization method.
    /// </summary>
    /// <value>
    /// The serialization method.
    /// </value>
    public BodySerializationMethod SerializationMethod { get; } = BodySerializationMethod.Default;
}
