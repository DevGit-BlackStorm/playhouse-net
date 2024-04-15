using PlayHouse.Communicator.Message;
using PlayHouse.Utils;

namespace PlayHouse.Service.Play.Base
{
    internal class BaseStageCmdHandler
    {
        private readonly LOG<BaseStageCmdHandler> _log = new ();
        private readonly Dictionary<string, IBaseStageCmd> _maps = new();

        public void Register(string msgId, IBaseStageCmd baseStageCmd)
        {
            if (_maps.ContainsKey(msgId))
            {
                throw new InvalidOperationException($"Already exist command - [msgId:{msgId}]");
            }
            _maps[msgId] = baseStageCmd;
        }

        public async Task Dispatch(BaseStage baseStage, RoutePacket request)
        {
            string msgId = request.MsgId;
            if (request.IsBase())
            {
                if (_maps.TryGetValue(msgId, out var cmd))
                {
                    await cmd.Execute(baseStage, request);
                }
                else
                {
                    _log.Error(()=>$"not registered message - [msgId:{msgId}]");
                }
            }
            else
            {
                _log.Error(()=>$"Invalid packet - [msgId:{msgId}]");
            }
        }
    }
}
