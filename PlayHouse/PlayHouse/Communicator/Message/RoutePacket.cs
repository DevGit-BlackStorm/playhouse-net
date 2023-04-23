using CommonLib;
using Google.Protobuf;
using Playhouse.Protocol;
using PlayHouse.Service;
using System.Net;

namespace PlayHouse.Communicator.Message
{
     public class Header
    {
        public short ServiceId { get; set; } = 0;
        public int MsgId { get; set; } = 0;
        public short MsgSeq { get; set; } = 0;
        public short ErrorCode { get; set; } = 0;
        public byte  StageIndex { get; set; } = 0;

        public Header(short serviceId = 0,int msgId = 0, short msgSeq = 0,short errorCode = 0 ,byte stageIndex = 0)
        {
            MsgId = msgId;
            ErrorCode = errorCode;
            MsgSeq = msgSeq;
            ServiceId = serviceId;
            StageIndex = stageIndex;

        }

         static public Header Of(HeaderMsg headerMsg)
        {
            return new Header((short)headerMsg.ServiceId, headerMsg.MsgId, (short)headerMsg.MsgSeq, (short)headerMsg.ErrorCode);
        }

        public  HeaderMsg ToMsg()
        {
            return new HeaderMsg()
            {
                ServiceId = ServiceId,
                MsgId = MsgId,
                MsgSeq = MsgSeq,
                ErrorCode = ErrorCode,
            };
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
        public long AccountId { get; set; } = 0;
        public long StageId { get; set; } = 0;

        public string From { get; set; } = "";

        public bool ForClient { get; set; } = false;

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
            ForClient = headerMsg.ForClient;
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

        public static RouteHeader TimerOf(long stageId, short msgId)
        {
            return new RouteHeader(new Header(msgId: msgId))
            {
                StageId = stageId
            };
        }
    }



    public class RoutePacket : IBasePacket {
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
        public short ServiceId() { return RouteHeader.Header.ServiceId; }
        public bool IsBackend() { return RouteHeader.IsBackend; }

 
        public Header Header=>RouteHeader.Header;

        public ClientPacket ToClientPacket()
        {
            return  new ClientPacket(RouteHeader.Header, MovePayload());
        }

        public Packet ToPacket()
        {
            return new Packet(MsgId, MovePayload());
        }

        public bool IsBase()
        {
            return RouteHeader.IsBase;
        }

        public long AccountId => RouteHeader.AccountId;

        public void SetMsgSeq(short msgSeq)
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

        public long StageId => RouteHeader.StageId;

        public bool IsSystem()
        {
            return RouteHeader.IsSystem;
        }

        public bool ForClient()
        {
            return RouteHeader.ForClient;
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

        public static RoutePacket SystemOf(Packet packet, bool isBase)
        {
            Header header = new Header(msgId: packet.MsgId);
            RouteHeader routeHeader = RouteHeader.Of(header);
            routeHeader.IsSystem = true;
            routeHeader.IsBase = isBase;
            return new RoutePacket(routeHeader, packet.MovePayload());
        }

        public static RoutePacket ApiOf(Packet packet, bool isBase, bool isBackend)
        {
            Header header = new Header(msgId:packet.MsgId);
            RouteHeader routeHeader = RouteHeader.Of(header);
            routeHeader.IsBase = isBase;
            routeHeader.IsBackend = isBackend;
            return new RoutePacket(routeHeader, packet.MovePayload());
        }

        public static RoutePacket SessionOf(int sid, Packet packet, bool isBase, bool isBackend)
        {
            Header header = new Header(msgId:packet.MsgId);
            RouteHeader routeHeader = RouteHeader.Of(header);
            routeHeader.Sid = sid;
            routeHeader.IsBase = isBase;    
            routeHeader.IsBackend = isBackend;
            
            return new RoutePacket(routeHeader, packet.MovePayload());
        }

        public static RoutePacket AddTimerOf(TimerMsg.Types.Type type, long stageId, long timerId, TimerCallbackTask timerCallback, TimeSpan initialDelay, TimeSpan period, int count = 0)
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

        public static RoutePacket StageTimerOf(long stageId, long timerId, TimerCallbackTask timerCallback, object? timerState)
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

        public static RoutePacket StageOf(long stageId, long accountId, Packet packet, bool isBase, bool isBackend)
        {
            Header header = new Header(msgId: packet.MsgId);
            RouteHeader routeHeader = RouteHeader.Of(header);
            routeHeader.StageId = stageId;
            routeHeader.AccountId = accountId;
            routeHeader.IsBase = isBase;
            routeHeader.IsBackend = isBackend;
            return new RoutePacket(routeHeader, packet.MovePayload());
        }

        public static RoutePacket ReplyOf(short serviceId, short msgSeq, ReplyPacket reply)
        {
            Header header = new(msgId:reply.MsgId)
            {
                ServiceId = serviceId,
                MsgSeq = msgSeq
            };

            RouteHeader routeHeader = RouteHeader.Of(header);
            routeHeader.IsReply = true;

            var routePacket = new RoutePacket(routeHeader, reply.MovePayload());
            routePacket.RouteHeader.Header.ErrorCode = reply.ErrorCode;
            return routePacket;
        }

        public static RoutePacket ClientOf(short serviceId, int sid, Packet packet)
        {
            Header header = new(msgId:packet.MsgId)
            {
                ServiceId = serviceId
            };

            RouteHeader routeHeader = RouteHeader.Of(header);
            routeHeader.Sid = sid;

            return new RoutePacket(routeHeader, packet.MovePayload());
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

            if (bodySize > ConstOption.MAX_PACKET_SIZE)
            {
                throw new Exception($"body size is over : {bodySize}");
            }

            buffer.WriteInt16(XBitConverter.ToNetworkOrder((short)bodySize));
            buffer.WriteInt16(XBitConverter.ToNetworkOrder(clientPacket.ServiceId()));
            buffer.WriteInt32(XBitConverter.ToNetworkOrder(clientPacket.GetMsgId()));
            buffer.WriteInt16(XBitConverter.ToNetworkOrder(clientPacket.GetMsgSeq()));
            buffer.WriteInt16(XBitConverter.ToNetworkOrder(clientPacket.Header.ErrorCode));
            buffer.Write(clientPacket.Header.StageIndex);

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

        public void Dispose()
        {
            _payload.Dispose();
        }

        public  ReplyPacket ToReplyPacket()
        {
            return new  ReplyPacket(RouteHeader.Header.ErrorCode,RouteHeader.MsgId,MovePayload()); 
        }
    }

    public class AsyncBlockPacket : RoutePacket
    {
        public AsyncPostCallback? AsyncPostCallback { get; }
        public Object Result { get; }

        private AsyncBlockPacket(AsyncPostCallback? asyncPostCallback, Object result, RouteHeader routeHeader) : base(routeHeader, new EmptyPayload())
        {
            AsyncPostCallback = asyncPostCallback;
            Result = result;
        }

        public static RoutePacket Of(long stageId, AsyncPostCallback? asyncPostCallback, Object result)
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
