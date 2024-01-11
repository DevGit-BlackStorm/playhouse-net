using Google.Protobuf;
using Playhouse.Protocol;
using PlayHouse.Production.Shared;

namespace PlayHouse.Communicator
{
    internal class XServerInfo : IServerInfo
    {
        public string BindEndpoint {get;set;}
        public ServiceType ServiceType {get;set;}
        public ushort ServiceId {get;set;}
        public ServerState State {get;set;}
        public long LastUpdate {get;set;}
        public int ActorCount { get; set; }

        public XServerInfo(
            string bindEndpoint, 
            ServiceType serviceType, 
            ushort serviceId, 
            ServerState state, 
            int actorCount, 
            long lastUpdate)
        {
            BindEndpoint = bindEndpoint;
            ServiceType = serviceType;
            ServiceId = serviceId;
            State = state;
            ActorCount = actorCount;
            LastUpdate = lastUpdate;
        }

        public static XServerInfo Of(string bindEndpoint, IService service)
        {
            return new XServerInfo(
                bindEndpoint,
                service.GetServiceType(),
                service.ServiceId,
                service.GetServerState(),
                service.GetActorCount(),
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
                infoMsg.ActorCount,
                infoMsg.Timestamp
            );
        }

        public static XServerInfo Of(
            string bindEndpoint, 
            ServiceType serviceType, 
            ushort serviceId, 
            ServerState state, 
            int actorCount, 
            long timeStamp)
        {
            return new XServerInfo(
                bindEndpoint, 
                serviceType, 
                serviceId, 
                state, 
                actorCount, 
                timeStamp);
        }

        public static XServerInfo Of(IServerInfo serverInfo)
        {
            return new XServerInfo(
                serverInfo.BindEndpoint, 
                serverInfo.ServiceType, 
                serverInfo.ServiceId, 
                serverInfo.State, 
                serverInfo.ActorCount, 
                serverInfo.LastUpdate);
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
                ActorCount = ActorCount
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

            ActorCount = serverInfo.ActorCount;

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

     
      
        public override string ToString()
        {
            return $"[endpoint: {BindEndpoint}, service type: {ServiceType}, serviceId: {ServiceId}, state: {State}, actor count: {ActorCount}, LastUpdate: {LastUpdate}]";
        }

        
    }

}
