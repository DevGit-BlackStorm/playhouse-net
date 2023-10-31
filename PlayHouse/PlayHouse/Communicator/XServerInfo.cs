using Google.Protobuf;
using Playhouse.Protocol;
using PlayHouse.Production;

namespace PlayHouse.Communicator
{
    public class XServerInfo : IServerInfo
    {
        public string BindEndpoint { get; }
        public ServiceType ServiceType { get; }
        public ushort ServiceId { get; }
        public ServerState State { get; set; }
        public int WeightingPoint { get; set; }
        public long LastUpdate { get; set; }

        public XServerInfo(string bindEndpoint, ServiceType serviceType, ushort serviceId, ServerState state, int weightingPoint, long lastUpdate)
        {
            BindEndpoint = bindEndpoint;
            ServiceType = serviceType;
            ServiceId = serviceId;
            State = state;
            WeightingPoint = weightingPoint;
            LastUpdate = lastUpdate;
        }

        public static XServerInfo Of(string bindEndpoint, IProcessor service)
        {
            return new XServerInfo(
                bindEndpoint,
                service.GetServiceType(),
                service.ServiceId,
                service.GetServerState(),
                service.GetWeightPoint(),
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            );
        }

        public static XServerInfo Of(ServerInfoMsg infoMsg)
        {
            return new XServerInfo(
                infoMsg.Endpoint,
                Enum.Parse<ServiceType>(infoMsg.ServiceType),
                (ushort)infoMsg.ServiceId,
                Enum.Parse<ServerState>(infoMsg.ServerState),
                infoMsg.WeightingPoint,
                infoMsg.Timestamp
            );
        }

        public static XServerInfo Of(string bindEndpoint, ServiceType serviceType, ushort serviceId, ServerState state, int weightingPoint, long timeStamp)
        {
            return new XServerInfo(bindEndpoint, serviceType, serviceId, state, weightingPoint, timeStamp);
        }

        public ServerInfoMsg ToMsg()
        {
            return new ServerInfoMsg
            {
                ServiceType = ServiceType.ToString(),
                ServiceId = ServiceId,
                Endpoint = BindEndpoint,
                ServerState = State.ToString(),
                Timestamp = LastUpdate,
                WeightingPoint = WeightingPoint
            };
        }

        public bool TimeOver()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - LastUpdate > 60000;
        }

        public bool Update(XServerInfo serverInfo)
        {
            bool stateChanged = State != serverInfo.State;

            State = serverInfo.State;

            LastUpdate = serverInfo.LastUpdate;

            WeightingPoint = serverInfo.WeightingPoint;

            return stateChanged;
        }

        public bool IsValid()
        {
            return State == ServerState.RUNNING;
        }

        public byte[] ToByteArray()
        {
            return ToMsg().ToByteArray();
        }

        public bool CheckTimeout()
        {
            if (TimeOver())
            {
                State = ServerState.DISABLE;
                return true;
            }
            return false;
        }

        string IServerInfo.BindEndpoint()
        {
            return BindEndpoint;
        }

        ServiceType IServerInfo.ServiceType()
        {
            return ServiceType;
        }

        ushort IServerInfo.ServiceId()
        {
            return ServiceId;
        }

        ServerState IServerInfo.State()
        {
            return State;
        }

        long IServerInfo.TimeStamp()
        {
            return LastUpdate;
        }

        public override string ToString()
        {
            return $"[Endpoint: {BindEndpoint}, ServiceType: {ServiceType}, ServiceId: {ServiceId}, State: {State}, WeightingPoint: {WeightingPoint}, LastUpdate: {LastUpdate}]";
        }
    }

}
