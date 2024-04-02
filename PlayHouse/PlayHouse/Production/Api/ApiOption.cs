using PlayHouse.Production.Api.Aspectify;

namespace PlayHouse.Production.Api;

public class ApiOption
{
    private ApiControllAspectifyManager _apiControllAspectifyManager = new();
    public ApiControllAspectifyManager AspectifyManager => _apiControllAspectifyManager;
}
