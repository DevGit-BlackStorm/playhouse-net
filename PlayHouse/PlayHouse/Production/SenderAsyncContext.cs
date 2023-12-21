using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Production
{
    public enum SendTarget
    {
        ErrorReply = 0,
        Reply = 1,
        Client = 2,
        Api = 3,
        Play = 4,
        System = 5,
        
    }
    public class SenderAsyncContext
    {
        private readonly static  AsyncLocal<List<(SendTarget target,IPacket packet)>?> _sendPackets = new();

        public static IList<(SendTarget target,IPacket packet)> SendPackets => _sendPackets.Value!;

        public static void Init()
        {
            _sendPackets.Value = new List<(SendTarget target, IPacket packet)>();
        }
        public static void Clear()
        {
            _sendPackets.Value!.Clear();
            _sendPackets.Value = null;
        }

        public static void Add(SendTarget target, IPacket packet)
        {
            if (_sendPackets.Value != null)
            {
                _sendPackets.Value.Add((target,packet));
            }
        }
    }
}
