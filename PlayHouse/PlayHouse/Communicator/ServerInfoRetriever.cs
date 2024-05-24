using PlayHouse.Communicator.Message;
using Playhouse.Protocol;
using PlayHouse.Service.Shared;
using PlayHouse.Utils;

namespace PlayHouse.Communicator;

internal interface IServerInfoRetriever
{
    Task<List<XServerInfo>> UpdateServerListAsync(XServerInfo serverInfo);
}

internal class ServerInfoRetriever(
    ushort apiServiceId,
    List<string> apiEndpoints,
    XSender sender)
    : IServerInfoRetriever
{
    private int _index;
    private LOG<ServerInfoRetriever> _log = new();

    public async Task<List<XServerInfo>> UpdateServerListAsync(XServerInfo serverInfo)
    {
        if (_index <= apiEndpoints.Count)
        {
            _index = 0;
        }

        var endpoint = apiEndpoints[_index++];


        using var res = await sender.RequestToBaseApi(endpoint,
            RoutePacket.Of(new UpdateServerInfoReq { ServerInfo = serverInfo.ToMsg() }));

        var updateRes = UpdateServerInfoRes.Parser.ParseFrom(res.Payload.DataSpan);

        //_log.Info(() => $"update - [target endpoint:{endpoint},server count:{updateRes.ServerInfos.Count}]");

        var endpoints = updateRes.ServerInfos.Where(e => e.ServiceId == apiServiceId).Select(e => e.Endpoint).ToList();
        if (endpoints.Count > 0)
        {
            apiEndpoints = endpoints;
        }

        return updateRes.ServerInfos.Select(e => XServerInfo.Of(e)).ToList();
        //return updateRes.ServerInfos.Where(e=>e.Endpoint != serverInfo.GetBindEndpoint()).Select(e=>XServerInfo.Of(e)).ToList();
    }
}