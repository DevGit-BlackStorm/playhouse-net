using PlayHouse.Production.Shared;

namespace PlayHouse.Service.Shared.Reflection;

internal class SystemHandlerRegister : ISystemHandlerRegister
{
    public Dictionary<int, SystemHandler> Handles { get; } = new();

    public void Add(int msgId, SystemHandler handler)
    {
        if (!Handles.TryAdd(msgId, handler))
        {
            throw new InvalidOperationException($"Already exists message ID: {msgId}");
        }
    }

    public SystemHandler GetHandler(int msgId)
    {
        if (Handles.TryGetValue(msgId, out var handler))
        {
            return handler;
        }

        throw new KeyNotFoundException($"Handler for message ID {msgId} not found");
    }
}