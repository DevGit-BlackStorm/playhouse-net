namespace PlayHouse.Production.Api.Aspectify;

public class ApiControllAspectifyManager
{
    private  readonly List<AspectifyAttribute> _attibutes = new List<AspectifyAttribute>();
    private readonly List<AspectifyAttribute> _backendAttibutes = new List<AspectifyAttribute>();

    public void Add(AspectifyAttribute attribute)
    {
        _attibutes.Add(attribute);
    }

    public void AddBackend(AspectifyAttribute attribute)
    {
        _backendAttibutes.Add(attribute);
    }

    public  List<AspectifyAttribute> Get()
    {
        return _attibutes;
    }
    public List<AspectifyAttribute> GetBackend()
    {
        return _backendAttibutes;
    }
}

