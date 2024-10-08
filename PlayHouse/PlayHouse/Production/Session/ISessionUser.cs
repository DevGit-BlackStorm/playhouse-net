using PlayHouse.Production.Shared;

namespace PlayHouse.Production.Session;

public interface ISessionUser
{
    Task OnDispatch(IPacket packet);
}