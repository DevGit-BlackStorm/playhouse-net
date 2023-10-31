using PlayHouse.Communicator.Message;
using PlayHouse.Production;

namespace PlayHouse.Service.Play.Base
{
    public class BaseStageCmdHandler
    {
        private readonly Dictionary<int, IBaseStageCmd> _maps = new();

        public void Register(int msgId, IBaseStageCmd baseStageCmd)
        {
            if (_maps.ContainsKey(msgId))
            {
                throw new InvalidOperationException($"Already exist command : {msgId}");
            }
            _maps[msgId] = baseStageCmd;
        }

        public async Task Dispatch(BaseStage baseStage, RoutePacket request)
        {
            int msgId = request.MsgId;
            if (request.IsBase())
            {
                if (_maps.TryGetValue(msgId, out var cmd))
                {
                    await cmd.Execute(baseStage, request);
                }
                else
                {
                    LOG.Error(()=>$"not registered message : {msgId}", this.GetType());
                }
            }
            else
            {
                LOG.Error(()=>$"Invalid packet : {msgId}", this.GetType());
            }
        }
    }

}
