﻿using PlayHouse.Production.Api;

namespace PlayHouse.Service.Api.Reflection;

internal class HandlerRegister : IHandlerRegister
{
    public Dictionary<string, ApiHandler> Handles { get; } = new();

    public void Add(string msgId, ApiHandler handler)
    {
        if (!Handles.TryAdd(msgId, handler))
        {
            throw new InvalidOperationException($"Already exists message ID: {msgId}");
        }
    }

    public ApiHandler GetHandler(string msgId)
    {
        if (Handles.TryGetValue(msgId, out var handler))
        {
            return handler;
        }

        throw new KeyNotFoundException($"Handler for message ID {msgId} not found");
    }
}

internal class BackendHandlerRegister : IBackendHandlerRegister
{
    public Dictionary<string, Delegate> Handles { get; } = new();

    public void Add(string msgId, ApiBackendHandler handler)
    {
        if (!Handles.TryAdd(msgId, handler))
        {
            throw new InvalidOperationException($"Already exists message ID: {msgId}");
        }
    }

    public ApiBackendHandler GetHandler(string msgId)
    {
        if (Handles.TryGetValue(msgId, out var handler))
        {
            return (ApiBackendHandler)handler;
        }

        throw new KeyNotFoundException($"Handler for message ID {msgId} not found");
    }
}