using PlayHouse.Production.Shared;

namespace PlayHouse.Service.Shared.Reflection;

internal class SystemHandlerRegister : ISystemHandlerRegister
{
    public Dictionary<string, SystemHandler> Handles { get; } = new();

    public void Add(string msgId, SystemHandler handler)
    {
        if (!Handles.TryAdd(msgId, handler))
        {
            throw new InvalidOperationException($"Already exists message ID: {msgId}");
        }
    }

    public SystemHandler GetHandler(string msgId)
    {
        if (Handles.TryGetValue(msgId, out var handler))
        {
            return handler;
        }

        throw new KeyNotFoundException($"Handler for message ID {msgId} not found");
    }
}