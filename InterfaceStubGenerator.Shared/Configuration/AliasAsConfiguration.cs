namespace Refit.Generator.Configuration;

// TODO: I hate how I have to duplicate the attributes in this file.
// See if I can remove this. Iirc Mapperly didn't need to do this initially. Might not need all this
// Arguably cleaner doing the current system tho
public class AliasAsConfiguration(string name)
{
    public string Name { get; protected set; } = name;
}
