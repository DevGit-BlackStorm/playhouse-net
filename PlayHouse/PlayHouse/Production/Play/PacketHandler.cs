using NetMQ;
using PlayHouse.Production;
using PlayHouse.Service.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Production.Play
{
    public class PacketHandler<S, A> where A : IActor
    {
        private readonly Dictionary<int, IPacketCmd<S, A>> _messageMap = new();

        public async Task Dispatch(S stage, A actor, Packet packet)
        {
            if (_messageMap.TryGetValue(packet.MsgId, out var cmd))
            {
                await cmd.Execute(stage, actor, packet);
            }
            else
            {
                throw new ArgumentException($"msgId:{packet.MsgId} is already registered");
            }
        }

        public void Add(int msgId, IPacketCmd<S, A> cmd)
        {
            if (_messageMap.ContainsKey(msgId))
            {
                throw new ArgumentException($"msgId:{msgId} is already registered");
            }
            _messageMap[msgId] = cmd;
        }
    }
}
