using PlayHouse.Communicator.Message;
using Playhouse.Protocol;

namespace PlayHouse.Service.Play.Base;

public interface ISessionUpdater
{
    public Task UpdateStageInfo(string sessionEndpoint, long sid);
}

internal class XSessionUpdater(string playEndpoint, XStageSender stageSender) : ISessionUpdater
{
    public async Task UpdateStageInfo(string sessionEndpoint, long sid)
    {
        var joinStageInfoUpdateReq = new JoinStageInfoUpdateReq
        {
            StageId = stageSender.StageId,
            PlayEndpoint = playEndpoint
        };

        using var res =
            await stageSender.RequestToBaseSession(sessionEndpoint, sid, RoutePacket.Of(joinStageInfoUpdateReq));
        var result = JoinStageInfoUpdateRes.Parser.ParseFrom(res.Span);
        //return result.StageId;
    }
}