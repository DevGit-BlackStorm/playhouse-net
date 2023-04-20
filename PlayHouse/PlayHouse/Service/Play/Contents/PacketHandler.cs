using Microsoft.Extensions.Logging;
using PlayHouse.Communicator.Message;
using PlayHouse.Service.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Service.Play.Contents
{
    public class PacketHandler<S, A> where A : IActor
    {
        private readonly ILogger _log;
        private readonly Dictionary<int, IPacketCmd<S, A>> _messageMap = new();

        public PacketHandler(ILogger log)
        {
            _log = log;
        }

        public async Task Dispatch(S stage, A actor, Packet packet)
        {
            if (_messageMap.TryGetValue(packet.MsgId, out var cmd))
            {
                await cmd.Execute(stage, actor, packet);
            }
            else
            {
                _log.Error($"unregistered packet {packet.MsgId}", nameof(PacketHandler<S, A>));
            }
        }

        public void Add(int msgId, IPacketCmd<S, A> cmd)
        {
            if (_messageMap.ContainsKey(msgId))
            {
                throw new ApiException.DuplicatedMessageIndex($"msgId:{msgId} is already registered");
            }
            _messageMap[msgId] = cmd;
        }
    }
}
