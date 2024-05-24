namespace PlayHouse.Production.Api.Aspectify;

public class ApiControllAspectifyManager
{
    private readonly List<AspectifyAttribute> _attributes = new();
    private readonly List<AspectifyAttribute> _backendAttributes = new();

    public void Add(AspectifyAttribute attribute)
    {
        _attributes.Add(attribute);
    }

    public void AddBackend(AspectifyAttribute attribute)
    {
        _backendAttributes.Add(attribute);
    }

    public List<AspectifyAttribute> Get()
    {
        return _attributes;
    }

    public List<AspectifyAttribute> GetBackend()
    {
        return _backendAttributes;
    }
}