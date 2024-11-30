using PlayHouse.Communicator.Message;
using Playhouse.Protocol;

namespace PlayHouse.Service.Play.Base;

public interface ISessionUpdater
{
    public Task UpdateStageInfo(int sessionNid, long sid);
}

internal class XSessionUpdater(int playEndpoint, XStageSender stageSender) : ISessionUpdater
{
    public async Task UpdateStageInfo(int sessionNid, long sid)
    {
        var joinStageInfoUpdateReq = new JoinStageInfoUpdateReq
        {
            StageId = stageSender.StageId,
            PlayNid = playEndpoint
        };

        using var res =
            await stageSender.RequestToBaseSession(sessionNid, sid, RoutePacket.Of(joinStageInfoUpdateReq));
        var result = JoinStageInfoUpdateRes.Parser.ParseFrom(res.Span);
        //return result.StageId;
    }
}