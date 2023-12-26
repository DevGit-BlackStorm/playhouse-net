using PlayHouse.Production.Api.Aspectify;

namespace PlayHouse.Production.Api.Filter;

public static class GlobalAspectifyManager
{
    private static readonly List<AspectifyAttribute> _attibutes = new List<AspectifyAttribute>();

    public static void Add(AspectifyAttribute attribute)
    {
        _attibutes.Add(attribute);
    }

    public static IEnumerable<AspectifyAttribute> Get()
    {
        return _attibutes;
    }
}

