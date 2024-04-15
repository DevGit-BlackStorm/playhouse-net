using PlayHouse.Production.Api;

namespace PlayHouse.Service.Api.Reflection;
internal class HandlerRegister : IHandlerRegister
{
    private readonly Dictionary<string, ApiHandler> _handles = new Dictionary<string, ApiHandler>();
    public Dictionary<string, ApiHandler> Handles => _handles;

    public void Add(string msgId, ApiHandler handler)
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

    public ApiHandler GetHandler(string msgId)
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
    private readonly Dictionary<string, Delegate> _handles = new Dictionary<string, Delegate>();
    public Dictionary<string, Delegate> Handles => _handles;

    public void Add(string msgId, ApiBackendHandler handler)
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

    public ApiBackendHandler GetHandler(string msgId)
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
