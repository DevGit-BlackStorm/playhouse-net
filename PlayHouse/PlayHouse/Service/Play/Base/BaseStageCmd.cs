using PlayHouse.Communicator.Message;


namespace PlayHouse.Service.Play.Base
{
    internal interface IBaseStageCmd
    {
        public PlayProcessor PlayProcessor { get; }
        public Task Execute(BaseStage baseStage, RoutePacket routePacket);
    }
}
