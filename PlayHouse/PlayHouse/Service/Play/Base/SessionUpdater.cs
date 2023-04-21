using Playhouse.Protocol;
using PlayHouse.Communicator.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Service.Play.Base
{
    public interface ISessionUpdater
    {
        public Task<int> UpdateStageInfo(string sessionEndpoint, int sid);
    }


    public class XSessionUpdater : ISessionUpdater
    {
        private XStageSender _stageSender;
        private string _playEndpoint;
        public XSessionUpdater(string playEndpoint,XStageSender stageSender)
        {
            _stageSender = stageSender;
            _playEndpoint = playEndpoint;
        }

        public async Task<int> UpdateStageInfo(string sessionEndpoint, int sid)
        {
            var joinStageInfoUpdateReq = new JoinStageInfoUpdateReq()
            {
                StageId = _stageSender.StageId,
                PlayEndpoint = _playEndpoint,
            };

            var res = await _stageSender.RequestToBaseSession(sessionEndpoint, sid, new Packet(joinStageInfoUpdateReq));
            var result = JoinStageInfoUpdateRes.Parser.ParseFrom(res.Data);
            return result.StageIdx;
        }
    }
}
