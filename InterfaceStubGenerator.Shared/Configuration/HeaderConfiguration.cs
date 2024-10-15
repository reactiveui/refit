namespace Refit.Generator.Configuration;

public class HeaderConfiguration(string header) : Attribute
{
    public string Header { get; } = header;
}
