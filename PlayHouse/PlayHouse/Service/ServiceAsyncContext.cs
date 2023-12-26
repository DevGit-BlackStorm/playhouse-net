using PlayHouse.Communicator.Message;
using PlayHouse.Production;
using PlayHouse.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Service
{

    internal class ServiceAsyncContext
    {
        private static readonly AsyncLocal<ConcurrentQueue<ReplyPacket>> ReplyQueue = new();
        public static LOG<PacketContext> _log = new();

        public static void Init()
        {
            ReplyQueue.Value = new ConcurrentQueue<ReplyPacket>();
        }

        public static void Clear()
        {
            foreach (var replyPacket in ReplyQueue.Value!)
            {
                try
                {
                    replyPacket.Dispose();
                }
                catch (Exception ex)
                {
                    _log.Error(() => "PacketContext Error in Clear");
                    _log.Error(() => $"{ex.Message}");
                    _log.Error(() => $"{ex.StackTrace}");
                }
            }
        }

        public static void AddReply(ReplyPacket replyPacket)
        {
            ReplyQueue.Value!.Append(replyPacket);
        }
    }
}
