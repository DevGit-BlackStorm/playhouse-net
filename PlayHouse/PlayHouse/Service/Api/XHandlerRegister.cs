using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayHouse.Production.Api;

namespace PlayHouse.Service.Api
{
    public class XHandlerRegister : IHandlerRegister
    {
        public readonly Dictionary<int, Delegate> Handles = new Dictionary<int, Delegate>();

        public void Add(int msgId, ApiHandler handler)
        {
            if (Handles.ContainsKey(msgId))
            {
                throw new InvalidOperationException($"Already exists message ID: {msgId}");
            }
            else
            {
                Handles[msgId] = handler;
            }
        }

        public ApiHandler GetHandler(int msgId)
        {
            if (Handles.TryGetValue(msgId, out var handler))
            {
                return (ApiHandler)handler;
            }
            else
            {
                throw new KeyNotFoundException($"Handler for message ID {msgId} not found");
            }
        }
    }

    public class XBackendHandlerRegister : IBackendHandlerRegister
    {
        public  readonly Dictionary<int, Delegate> Handles = new Dictionary<int, Delegate>();

        public void Add(int msgId, ApiBackendHandler handler)
        {
            if (Handles.ContainsKey(msgId))
            {
                throw new InvalidOperationException($"Already exists message ID: {msgId}");
            }
            else
            {
                Handles[msgId] = handler;
            }
        }

        public ApiBackendHandler GetHandler(int msgId)
        {
            if (Handles.TryGetValue(msgId, out var handler))
            {
                return (ApiBackendHandler)handler;
            }
            else
            {
                throw new KeyNotFoundException($"Handler for message ID {msgId} not found");
            }
        }
    }
}
