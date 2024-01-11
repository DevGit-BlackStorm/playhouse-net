namespace PlayHouse.Production.Api.Aspectify;

public static class ApiControllAspectifyManager
{
    private static readonly List<AspectifyAttribute> _attibutes = new List<AspectifyAttribute>();

    public static void Add(AspectifyAttribute attribute)
    {
        _attibutes.Add(attribute);
    }

    public static List<AspectifyAttribute> Get()
    {
        return _attibutes;
    }
}

