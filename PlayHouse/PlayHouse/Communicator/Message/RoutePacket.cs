using CommonLib;
using Google.Protobuf;
using NetMQ;
using Playhouse.Protocol;
using PlayHouse.Production;
using PlayHouse.Service;
using System.Runtime.InteropServices;

namespace PlayHouse.Communicator.Message
{
    public class Header
    {
        public ushort ServiceId { get; set; } = 0;
        public int MsgId { get; set; } = 0;
        public ushort MsgSeq { get; set; } = 0;
        public ushort ErrorCode { get; set; } = 0;
        public byte  StageIndex { get; set; } = 0;

        public Header(ushort serviceId = 0,int msgId = 0, ushort msgSeq = 0,ushort errorCode = 0 ,byte stageIndex = 0)
        {
            ServiceId = serviceId;
            MsgId = msgId;
            ErrorCode = errorCode;
            MsgSeq = msgSeq;
            StageIndex = stageIndex;

        }

         public static Header Of(HeaderMsg headerMsg)
        {
            return new Header((ushort)headerMsg.ServiceId, headerMsg.MsgId, (ushort)headerMsg.MsgSeq, (ushort)headerMsg.ErrorCode,(byte)headerMsg.StageIndex);
        }

        public  HeaderMsg ToMsg()
        {
            return new HeaderMsg()
            {
                ServiceId = ServiceId,
                MsgId = MsgId,
                MsgSeq = MsgSeq,
                ErrorCode = ErrorCode,
                StageIndex = StageIndex
            };
        }
        public override string ToString()
        {
            return $"Header(ServiceId={ServiceId}, MsgId={MsgId}, MsgSeq={MsgSeq}, ErrorCode={ErrorCode}, StageIndex={StageIndex})";
        }
    }
    public class RouteHeader
    {
        public Header Header { get; }
        public int Sid { get; set; } = 0;
        public bool IsSystem { get; set; } = false;
        public bool IsBase { get; set; } = false;
        public bool IsBackend { get; set; } = false;
        public bool IsReply { get; set; } = false;
        public string AccountId { get; set; } = string.Empty;
        public string StageId { get; set; } = string.Empty;

        public string From { get; set; } = "";

        public bool IsToClient { get; set; } = false;

        public RouteHeader(Header header)
        {
            this.Header = header;
        }
       
        public RouteHeader(RouteHeaderMsg headerMsg)
            : this(Header.Of(headerMsg.HeaderMsg))
        {
            Sid = headerMsg.Sid;
            IsSystem = headerMsg.IsSystem;
            IsBase = headerMsg.IsBase;
            IsBackend = headerMsg.IsBackend;
            IsReply = headerMsg.IsReply;
            AccountId = headerMsg.AccountId;
            StageId = headerMsg.StageId;
        }

        public byte[] ToByteArray()
        {
            return ToMsg().ToByteArray();
        }

        public int MsgId => Header.MsgId;

        public RouteHeaderMsg ToMsg()
        {
            var message = new RouteHeaderMsg();
            message.HeaderMsg = Header.ToMsg();
            message.Sid = Sid;
            message.IsSystem = IsSystem;
            message.IsBase = IsBase;
            message.IsBackend = IsBackend;
            message.IsReply = IsReply;
            message.AccountId = AccountId;
            message.StageId = StageId;
            return message;
        }

        public static RouteHeader Of(HeaderMsg header)
        {
            return new RouteHeader(Header.Of(header));
        }

        public static RouteHeader Of(Header header)
        {
            return new RouteHeader(header);
        }

        public static RouteHeader TimerOf(string stageId, short msgId)
        {
            return new RouteHeader(new Header(msgId: msgId))
            {
                StageId = stageId
            };
        }
        public override string ToString()
        {
            return $"RouteHeader(Header={Header},AccountId={AccountId}, Sid={Sid}, IsSystem={IsSystem}, IsBase={IsBase}, IsBackend={IsBackend}, IsReply={IsReply},  StageId={StageId}, From={From}, ForClient={IsToClient})";
        }

        
    }



    internal class RoutePacket : IBasePacket {
        public RouteHeader RouteHeader;
        private IPayload _payload;

        public long TimerId = 0;
        public TimerCallbackTask? TimerCallback = null;

        protected RoutePacket(RouteHeader routeHeader, IPayload payload)
        {
            this.RouteHeader = routeHeader;
            this._payload = payload;
        }


        public int MsgId => RouteHeader.MsgId;
        public ushort ServiceId() { return RouteHeader.Header.ServiceId; }
        public bool IsBackend() { return RouteHeader.IsBackend; }

 
        public Header Header=>RouteHeader.Header;

        public ClientPacket ToClientPacket()
        {
            return  new ClientPacket(RouteHeader.Header, MovePayload());
        }

        //public Packet ToPacket()
        //{
        //    return new Packet(MsgId, MovePayload());
        //}

        public bool IsBase()
        {
            return RouteHeader.IsBase;
        }

        public string AccountId => RouteHeader.AccountId;

        public void SetMsgSeq(ushort msgSeq)
        {
            RouteHeader.Header.MsgSeq = msgSeq;
        }

        public bool IsRequest()
        {
            return RouteHeader.Header.MsgSeq != 0;
        }

        public bool IsReply()
        {
            return RouteHeader.IsReply;
        }

        public string StageId => RouteHeader.StageId;

        public bool IsSystem()
        {
            return RouteHeader.IsSystem;
        }

        public bool IsToClient()
        {
            return RouteHeader.IsToClient;
        }

        public static RoutePacket MoveOf(RoutePacket routePacket)
        {
            RoutePacket movePacket = Of(routePacket.RouteHeader, routePacket.MovePayload());
            movePacket.TimerId = routePacket.TimerId;
            movePacket.TimerCallback = routePacket.TimerCallback;
            return movePacket;
        }


        public static RoutePacket Of(RouteHeader routeHeader, IPayload payload)
        {
            return new RoutePacket(routeHeader, payload);
        }

        internal static RoutePacket Of(int msgId, IPayload payload)
        {
            return new RoutePacket(new RouteHeader(new Header() { MsgId = msgId }), payload);
        }

        internal static RoutePacket Of(IMessage message)
        {
            return new RoutePacket(new RouteHeader(new Header() { MsgId = message.Descriptor.Index }), new ProtoPayload(message));
        }

        internal static RoutePacket Of(IPacket packet)
        {
            return new RoutePacket(new RouteHeader(new Header() { MsgId = packet.MsgId}),packet.Payload);
        }

        public static RoutePacket SystemOf(RoutePacket packet, bool isBase)
        {
            Header header = new Header(msgId: packet.MsgId);
            RouteHeader routeHeader = RouteHeader.Of(header);
            routeHeader.IsSystem = true;
            routeHeader.IsBase = isBase;
            return new RoutePacket(routeHeader, packet.MovePayload());
        }

        public static RoutePacket ApiOf(RoutePacket packet, bool isBase, bool isBackend)
        {
            Header header = new Header(msgId:packet.MsgId);
            RouteHeader routeHeader = RouteHeader.Of(header);
            routeHeader.IsBase = isBase;
            routeHeader.IsBackend = isBackend;
            return new RoutePacket(routeHeader, packet.MovePayload());
        }

        public static RoutePacket SessionOf(int sid, RoutePacket packet, bool isBase, bool isBackend)
        {
            Header header = new Header(msgId:packet.MsgId);
            RouteHeader routeHeader = RouteHeader.Of(header);
            routeHeader.Sid = sid;
            routeHeader.IsBase = isBase;    
            routeHeader.IsBackend = isBackend;
            
            return new RoutePacket(routeHeader, packet.MovePayload());
        }

        public static RoutePacket AddTimerOf(TimerMsg.Types.Type type, string stageId, long timerId, TimerCallbackTask timerCallback, TimeSpan initialDelay, TimeSpan period, int count = 0)
        {
            Header header = new Header(msgId:(short)TimerMsg.Descriptor.Index);
            RouteHeader routeHeader = RouteHeader.Of(header);
            routeHeader.StageId = stageId;
            routeHeader.IsBase = true;

            TimerMsg message = new TimerMsg
            {
                Type = type,
                Count = count,
                InitialDelay = (long)initialDelay.TotalMilliseconds,
                Period = (long)period.TotalMilliseconds
            };

            return new RoutePacket(routeHeader, new ProtoPayload(message))
            {
                TimerCallback = timerCallback,
                TimerId = timerId
            };
        }

        public static RoutePacket StageTimerOf(string stageId, long timerId, TimerCallbackTask timerCallback, object? timerState)
        {
            Header header = new Header(msgId: StageTimer.Descriptor.Index);
            RouteHeader routeHeader = RouteHeader.Of(header);

            return new RoutePacket(routeHeader, new EmptyPayload())
            {
                RouteHeader = { StageId = stageId, IsBase = true },
                TimerId = timerId,
                TimerCallback = timerCallback,
            };
        }

        public static RoutePacket StageOf(string stageId, string accountId, RoutePacket packet, bool isBase, bool isBackend)
        {
            Header header = new Header(msgId: packet.MsgId);
            RouteHeader routeHeader = RouteHeader.Of(header);
            routeHeader.StageId = stageId;
            routeHeader.AccountId = accountId;
            routeHeader.IsBase = isBase;
            routeHeader.IsBackend = isBackend;
            return new RoutePacket(routeHeader, packet.MovePayload());
        }

        //public static RoutePacket ReplyOf(ushort serviceId, ushort msgSeq, int sid,bool forClient, ReplyPacket reply)
        //{
        //    Header header = new(msgId:reply.MsgId)
        //    {
        //        ServiceId = serviceId,
        //        MsgSeq = msgSeq
        //    };

        //    RouteHeader routeHeader = RouteHeader.Of(header);
        //    routeHeader.IsReply = true;
        //    routeHeader.IsToClient = forClient;
        //    routeHeader.Sid = sid;

        //    var routePacket = new RoutePacket(routeHeader, reply.MovePayload());
        //    routePacket.RouteHeader.Header.ErrorCode = reply.ErrorCode;
        //    return routePacket;
        //}
        public static RoutePacket ReplyOf(ushort serviceId, RouteHeader sourceHeader,ushort errorCode,IPacket? reply)
        {
            Header header = new(msgId: reply !=null ? reply.MsgId : 0)
            {
                ServiceId = serviceId,
                MsgSeq = sourceHeader.Header.MsgSeq
            };

            RouteHeader routeHeader = RouteHeader.Of(header);
            routeHeader.IsReply = true;
            routeHeader.IsToClient = !sourceHeader.IsBackend ;
            routeHeader.Sid = sourceHeader.Sid;
            routeHeader.IsBackend = sourceHeader.IsBackend;
            routeHeader.IsBase = sourceHeader.IsBase;
            routeHeader.AccountId = sourceHeader.AccountId;

            var routePacket = reply !=null ? new RoutePacket(routeHeader, reply.Payload) : new RoutePacket(routeHeader,new EmptyPayload());
            routePacket.RouteHeader.Header.ErrorCode = errorCode;
            return routePacket;
        }

        public static RoutePacket ClientOf(ushort serviceId, int sid, IPacket packet)
        {
            Header header = new(msgId:packet.MsgId)
            {
                ServiceId = serviceId
            };

            RouteHeader routeHeader = RouteHeader.Of(header);
            routeHeader.Sid = sid;
            routeHeader.IsToClient = true;

            return new RoutePacket(routeHeader, packet.Payload);
        }

        public void WriteClientPacketBytes(RingBuffer buffer)
        {
            ClientPacket clientPacket = ToClientPacket();
            WriteClientPacketBytes(clientPacket, buffer);
        }


        public static  void WriteClientPacketBytes(ClientPacket clientPacket, RingBuffer buffer)
        {
            var body = clientPacket.Payload.Data;

            int bodySize = body.Length;

            if (bodySize > ConstOption.MaxPacketSize)
            {
                throw new Exception($"body size is over : {bodySize}");
            }

            buffer.WriteInt16(XBitConverter.ToNetworkOrder((ushort)bodySize));
            buffer.WriteInt16(XBitConverter.ToNetworkOrder(clientPacket.ServiceId()));
            buffer.WriteInt32(XBitConverter.ToNetworkOrder(clientPacket.GetMsgId()));
            buffer.WriteInt16(XBitConverter.ToNetworkOrder(clientPacket.GetMsgSeq()));
            buffer.Write(clientPacket.Header.StageIndex);
            buffer.WriteInt16(XBitConverter.ToNetworkOrder(clientPacket.Header.ErrorCode));
            buffer.Write(clientPacket.Payload.Data);
        
        }


        public IPayload MovePayload()
        {
            IPayload temp = _payload;
            _payload =  new EmptyPayload();
            return temp;
        }

        public IPayload Payload => _payload;

        public ReadOnlySpan<byte> Data => _payload.Data;

        public object? TimerObject { get; private set; }
        public ushort MsgSeq => Header.MsgSeq;

        public void Dispose()
        {
            _payload.Dispose();
        }

        public  ReplyPacket ToReplyPacket()
        {
            return new  ReplyPacket(RouteHeader.Header.ErrorCode,RouteHeader.MsgId,MovePayload()); 
        }

        internal IPacket ToContentsPacket(ushort msgSeq)
        {
            return PacketProducer.CreatePacket(MsgId, _payload,msgSeq);
        }

        
    }

    internal class AsyncBlockPacket : RoutePacket
    {
        public AsyncPostCallback AsyncPostCallback { get; }
        public object Result { get; }

        private AsyncBlockPacket(AsyncPostCallback asyncPostCallback, object result, RouteHeader routeHeader) : base(routeHeader, new EmptyPayload())
        {
            AsyncPostCallback = asyncPostCallback;
            Result = result;
        }

        public static RoutePacket Of(string stageId, AsyncPostCallback asyncPostCallback, Object result)
        {
            var header = new Header(msgId: AsyncBlock.Descriptor.Index);
            var routeHeader = RouteHeader.Of(header);
            var packet = new AsyncBlockPacket(asyncPostCallback, result, routeHeader);
            packet.RouteHeader.StageId = stageId;
            packet.RouteHeader.IsBase = true;
            return packet;
        }
    }

}
