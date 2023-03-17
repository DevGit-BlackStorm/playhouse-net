using Google.Protobuf;
using NetMQ.Sockets;
using Playhouse.Protocol;

namespace PlayHouse.Communicator.Message
{
     public class Header
    {
        public string MsgName { get; set; } = "";
        public int ErrorCode { get; set; } = 0;
        public int MsgSeq { get; set; } = 0;
        public string ServiceId { get; set; } = "";

        public Header(String msgName = "", int errorCode = 0, int msgSeq = 0, String serviceId = "")
        {
            MsgName = msgName;
            ErrorCode = errorCode;
            MsgSeq = msgSeq;
            ServiceId = serviceId;

        }

        public static Header Of(HeaderMsg headerMsg)
        {
            return new Header(headerMsg.MsgName, headerMsg.ErrorCode, headerMsg.MsgSeq, headerMsg.ServiceId);

        }

        public HeaderMsg ToMsg()
        {
            return new HeaderMsg
            {
                ServiceId = this.ServiceId,
                MsgSeq = this.MsgSeq,
                MsgName = this.MsgName,
                ErrorCode = this.ErrorCode,
            };
        }
    }
    public class RouteHeader
    {
        public Header Header { get; }
        public int Sid { get; set; } = -1;
        public string SessionInfo { get; set; } = "";
        public bool IsSystem { get; set; } = false;
        public bool IsBase { get; set; } = false;
        public bool IsBackend { get; set; } = false;
        public bool IsReply { get; set; } = false;
        public long AccountId { get; set; } = 0;
        public long StageId { get; set; } = 0;

        public string From { get; set; } = "";

        public RouteHeader(Header header)
        {
            this.Header = header;
        }

        public RouteHeader(HeaderMsg headerMsg)
            : this(Header.Of(headerMsg))
        {
        }

        public RouteHeader(RouteHeaderMsg headerMsg)
            : this(Header.Of(headerMsg.HeaderMsg))
        {
            Sid = headerMsg.Sid;
            SessionInfo = headerMsg.SessionInfo;
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

        public string MsgName()
        {
            return Header.MsgName;
        }

        public RouteHeaderMsg ToMsg()
        {
            var message = new RouteHeaderMsg();
            message.HeaderMsg = Header.ToMsg();
            message.Sid = Sid;
            message.SessionInfo = SessionInfo;
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

        public static RouteHeader TimerOf(long stageId, string msgName)
        {
            return new RouteHeader(new Header(msgName))
            {
                StageId = stageId
            };
        }
    }



    public class RoutePacket : IBasePacket {
        public RouteHeader RouteHeader;
        private IPayload _payload;

        public long TimerId = 0;
        public TimerCallback? TimerCallback = null;


        protected RoutePacket(RouteHeader routeHeader, IPayload payload)
        {
            this.RouteHeader = routeHeader;
            this._payload = payload;
        }

        public string MsgName() { return RouteHeader.MsgName(); }
        public string ServiceId() { return RouteHeader.Header.ServiceId; }
        public bool IsBackend() { return RouteHeader.IsBackend; }

        
 
        public Header Header()
        {
            return RouteHeader.Header;
        }

        public ClientPacket ToClientPacket()
        {
            return ClientPacket.Of(RouteHeader.Header.ToMsg(), MovePayload());
        }

        public Packet ToPacket()
        {
            return new Packet(MsgName(), MovePayload());
        }

        public bool IsBase()
        {
            return RouteHeader.IsBase;
        }

        public long AccountId()
        {
            return RouteHeader.AccountId;
        }

        public void SetMsgSeq(int msgSeq)
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

        public long StageId()
        {
            return RouteHeader.StageId;
        }

        public bool IsSystem()
        {
            return RouteHeader.IsSystem;
        }

        public static RoutePacket MoveOf(RoutePacket routePacket)
        {
            //if (routePacket is AsyncBlockPacket)
            //{
            //    return routePacket;
            //}
            RoutePacket movePacket = Of(routePacket.RouteHeader, routePacket.MovePayload());
            movePacket.TimerId = routePacket.TimerId;
            movePacket.TimerCallback = routePacket.TimerCallback;
            return movePacket;
        }

        public static RoutePacket Of(RoutePacketMsg routePacketMsg)
        {
            return new RoutePacket(new RouteHeader(routePacketMsg.RouteHeaderMsg), new ByteStringPayload(routePacketMsg.Message));
        }

        public static RoutePacket Of(RouteHeader routeHeader, IPayload payload)
        {
            return new RoutePacket(routeHeader, payload);
        }

        public static RoutePacket Of(RouteHeader routeHeader, IMessage message)
        {
            return new RoutePacket(routeHeader, new IMessagePayload(message));
        }

        public static RoutePacket SystemOf(Packet packet, bool isBase)
        {
            Header header = new Header(packet.MsgName);
            RouteHeader routeHeader = RouteHeader.Of(header);
            routeHeader.IsSystem = true;
            routeHeader.IsBase = isBase;
            return new RoutePacket(routeHeader, packet.MovePayload());
        }

        public static RoutePacket ApiOf(string sessionInfo, Packet packet, bool isBase, bool isBackend)
        {
            Header header = new Header(packet.MsgName);
            RouteHeader routeHeader = RouteHeader.Of(header);
            routeHeader.SessionInfo = sessionInfo;
            routeHeader.IsBase = isBase;
            routeHeader.IsBackend = isBackend;
            return new RoutePacket(routeHeader, packet.MovePayload());
        }

        public static RoutePacket SessionOf(int sid, Packet packet, bool isBase, bool isBackend)
        {
            Header header = new Header(packet.MsgName);
            RouteHeader routeHeader = RouteHeader.Of(header);
            routeHeader.Sid = sid;
            routeHeader.IsBase = isBase;    
            routeHeader.IsBackend = isBackend;
            
            return new RoutePacket(routeHeader, packet.MovePayload());
        }

        public static RoutePacket AddTimerOf(TimerMsg.Types.Type type, long stageId, long timerId, TimerCallback timerCallback, TimeSpan initialDelay, TimeSpan period, int count = 0)
        {
            Header header = new Header(TimerMsg.Descriptor.Name);
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

            return new RoutePacket(routeHeader, new IMessagePayload(message))
            {
                TimerCallback = timerCallback,
                TimerId = timerId
            };
        }

        public static RoutePacket StageTimerOf(long stageId, long timerId, TimerCallback timerCallback)
        {
            Header header = new Header(StageTimer.Descriptor.Name);
            RouteHeader routeHeader = RouteHeader.Of(header);

            return new RoutePacket(routeHeader, XPayload.Empty())
            {
                RouteHeader = { StageId = stageId, IsBase = true },
                TimerId = timerId,
                TimerCallback = timerCallback
            };
        }

        public static RoutePacket StageOf(long stageId, long accountId, Packet packet, bool isBase, bool isBackend)
        {
            Header header = new Header(packet.MsgName);
            RouteHeader routeHeader = RouteHeader.Of(header);
            routeHeader.StageId = stageId;
            routeHeader.AccountId = accountId;
            routeHeader.IsBase = isBase;
            routeHeader.IsBackend = isBackend;
            return new RoutePacket(routeHeader, packet.MovePayload());
        }

        public static RoutePacket ReplyOf(string serviceId, int msgSeq, ReplyPacket reply)
        {
            Header header = new(reply.MsgName)
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

        public static RoutePacket ClientOf(string serviceId, int sid, Packet packet)
        {
            Header header = new(packet.MsgName)
            {
                ServiceId = serviceId
            };

            RouteHeader routeHeader = RouteHeader.Of(header);
            routeHeader.Sid = sid;

            return new RoutePacket(routeHeader, packet.MovePayload());
        }

        public IPayload MovePayload()
        {
            IPayload temp = _payload;
            _payload =  XPayload.Empty();
            return temp;
        }

        public IPayload GetPayload()
        {
            return _payload;
        }

        public byte[] Data()
        {
            return _payload.Data();
        }

        public void Dispose()
        {
            _payload.Dispose();
        }

        public  ReplyPacket ToReplyPacket()
        {
            return new  ReplyPacket(RouteHeader.Header.ErrorCode,RouteHeader.MsgName(),MovePayload()); 
        }
    }

 }
