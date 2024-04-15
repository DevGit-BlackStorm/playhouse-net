using PlayHouse.Production.Shared;

namespace PlayHouse.Service.Shared.Reflection;

internal class SystemHandlerRegister : ISystemHandlerRegister
{

    private readonly Dictionary<string, SystemHandler> _handles = new Dictionary<string, SystemHandler>();
    public Dictionary<string, SystemHandler> Handles => _handles;

    public void Add(string msgId, SystemHandler handler)
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

    public SystemHandler GetHandler(string msgId)
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
