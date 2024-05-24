using PlayHouse.Production.Api.Aspectify;

namespace PlayHouse.Production.Api;

public class ApiOption
{
    public ApiControllAspectifyManager AspectifyManager { get; } = new();
}