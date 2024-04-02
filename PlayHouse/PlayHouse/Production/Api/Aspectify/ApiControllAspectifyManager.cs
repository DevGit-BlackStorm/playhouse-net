namespace PlayHouse.Production.Api.Aspectify;

public class ApiControllAspectifyManager
{
    private  readonly List<AspectifyAttribute> _attibutes = new List<AspectifyAttribute>();

    public void Add(AspectifyAttribute attribute)
    {
        _attibutes.Add(attribute);
    }

    public  List<AspectifyAttribute> Get()
    {
        return _attibutes;
    }
}

