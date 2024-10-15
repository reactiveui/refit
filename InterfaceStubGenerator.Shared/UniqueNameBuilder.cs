namespace Refit.Generator;

public class UniqueNameBuilder()
{
    readonly HashSet<string> usedNames = new(StringComparer.Ordinal);
    readonly UniqueNameBuilder? parentScope;

    private UniqueNameBuilder(UniqueNameBuilder parentScope)
        : this()
    {
        this.parentScope = parentScope;
    }

    public void Reserve(string name) => usedNames.Add(name);

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

        usedNames.Add(uniqueName);

        return uniqueName;
    }

    public void Reserve(IEnumerable<string> names)
    {
        foreach (var name in names)
        {
            usedNames.Add(name);
        }
    }

    bool Contains(string name)
    {
        if (usedNames.Contains(name))
            return true;

        if (parentScope != null)
            return parentScope.Contains(name);

        return false;
    }
}
