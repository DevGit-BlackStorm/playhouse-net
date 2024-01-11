using PlayHouse.Production.Api;

namespace PlayHouse.Service.Api.Reflection;
internal class HandlerRegister : IHandlerRegister
{
    private readonly Dictionary<int, ApiHandler> _handles = new Dictionary<int, ApiHandler>();
    public Dictionary<int, ApiHandler> Handles => _handles;

    public void Add(int msgId, ApiHandler handler)
    {
        if (_handles.ContainsKey(msgId))
        {
            throw new InvalidOperationException($"Already exists message ID: {msgId}");
        }
        else
        {
            _handles[msgId] = handler;
        }
    }

    public ApiHandler GetHandler(int msgId)
    {
        if (_handles.TryGetValue(msgId, out var handler))
        {
            return handler;
        }
        else
        {
            throw new KeyNotFoundException($"Handler for message ID {msgId} not found");
        }
    }
}

internal class BackendHandlerRegister : IBackendHandlerRegister
{
    private readonly Dictionary<int, Delegate> _handles = new Dictionary<int, Delegate>();
    public Dictionary<int, Delegate> Handles => _handles;

    public void Add(int msgId, ApiBackendHandler handler)
    {
        if (Handles.ContainsKey(msgId))
        {
            throw new InvalidOperationException($"Already exists message ID: {msgId}");
        }
        else
        {
            _handles[msgId] = handler;
        }
    }

    public ApiBackendHandler GetHandler(int msgId)
    {
        if (_handles.TryGetValue(msgId, out var handler))
        {
            return (ApiBackendHandler)handler;
        }
        else
        {
            throw new KeyNotFoundException($"Handler for message ID {msgId} not found");
        }
    }
}
