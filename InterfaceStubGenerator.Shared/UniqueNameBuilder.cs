namespace Refit.Generator;

public class UniqueNameBuilder()
{
    private readonly HashSet<string> _usedNames = new(StringComparer.Ordinal);
    private readonly UniqueNameBuilder? _parentScope;

    private UniqueNameBuilder(UniqueNameBuilder parentScope)
        : this()
    {
        _parentScope = parentScope;
    }

    public void Reserve(string name) => _usedNames.Add(name);

    public UniqueNameBuilder NewScope() => new(this);

    public string New(string name)
    {
        var i = 0;
        var uniqueName = name;
        while (Contains(uniqueName))
        {
            uniqueName = name + i;
            i++;
        }

        _usedNames.Add(uniqueName);

        return uniqueName;
    }

    public void Reserve(IEnumerable<string> names)
    {
        foreach (var name in names)
        {
            _usedNames.Add(name);
        }
    }

    private bool Contains(string name)
    {
        if (_usedNames.Contains(name))
            return true;

        if (_parentScope != null)
            return _parentScope.Contains(name);

        return false;
    }
}
