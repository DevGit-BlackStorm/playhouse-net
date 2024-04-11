
using PlayHouse.Production.Shared;

namespace PlayHouse.Production.Api;

public delegate Task ApiHandler(IPacket packet, IApiSender apiSender);
public delegate Task ApiBackendHandler(IPacket packet, IApiBackendSender apiSender);


public interface IHandlerRegister 
{
    void Add(int msgId, ApiHandler handler);
}

public interface IBackendHandlerRegister 
{
    void Add(int msgId, ApiBackendHandler handler);
}

public interface IApiController
{
    void Handles(IHandlerRegister handlerRegister);
}

public interface IBackendApiController
{
    void Handles(IBackendHandlerRegister backendHandlerRegister);
}
