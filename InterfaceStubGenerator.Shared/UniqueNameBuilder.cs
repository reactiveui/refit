namespace Refit.Generator;

// <Summary>
// UniqueNameBuilder.
// </Summary>
public class UniqueNameBuilder()
{
    private readonly HashSet<string> _usedNames = new(StringComparer.Ordinal);
    private readonly UniqueNameBuilder? _parentScope;

    private UniqueNameBuilder(UniqueNameBuilder parentScope)
        : this()
    {
        _parentScope = parentScope;
    }

    /// <summary>
    /// Reserve a name.
    /// </summary>
    /// <param name="name"></param>
    public void Reserve(string name) => _usedNames.Add(name);

    /// <summary>
    /// Create a new scope.
    /// </summary>
    /// <returns>Unique Name Builder.</returns>
    public UniqueNameBuilder NewScope() => new(this);

    /// <summary>
    /// Generate a unique name.
    /// </summary>
    /// <param name="name">THe name.</param>
    /// <returns></returns>
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

    /// <summary>
    /// Reserve names.
    /// </summary>
    /// <param name="names">The name.</param>
    public void Reserve(IEnumerable<string> names)
    {
        if (names == null)
        {
            return;
        }

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
