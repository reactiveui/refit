namespace DefaultNamespace;

public class PropertyConfiguration
{
    public PropertyConfiguration() { }

    public PropertyConfiguration(string key)
    {
        Key = key;
    }

    public string? Key { get; }
}
