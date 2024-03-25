﻿using PlayHouse.Communicator.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Production.Shared
{
    public interface IPacket : IDisposable
    {
        public int MsgId { get; }
        public IPayload Payload { get; }
        public IPacket Copy();
        public T Parse<T>();

    }

}
