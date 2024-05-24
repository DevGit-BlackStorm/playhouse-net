using PlayHouse.Production.Shared;

namespace PlayHouse.Production.Api;

public interface IDisconnectCallback
{
    Task OnDisconnectAsync(IApiSender apiSender);
}