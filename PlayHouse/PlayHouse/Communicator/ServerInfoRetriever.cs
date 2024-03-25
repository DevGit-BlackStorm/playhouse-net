using Playhouse.Protocol;
using PlayHouse.Communicator.Message;
using PlayHouse.Service.Shared;
using PlayHouse.Utils;

namespace PlayHouse.Communicator;

internal interface IServerInfoRetriever
{
    Task<List<XServerInfo>> UpdateServerListAsync(XServerInfo serverInfo);
}


internal class ServerInfoRetriever : IServerInfoRetriever
{
    private LOG<ServerInfoRetriever> _log = new();
    private readonly ushort _apiServiceId;
    private List<string> _apiEndpoints;
    private readonly XSender _xSender;
    private int _index;
    public ServerInfoRetriever(
        ushort apiServiceId,
        List<string> apiEndpoints, 
        XSender sender)
    {
        _apiServiceId = apiServiceId;
        _apiEndpoints = apiEndpoints;
        _xSender = sender;
    }

    public async Task<List<XServerInfo>> UpdateServerListAsync(XServerInfo serverInfo)
    {
        if(_index <= _apiEndpoints.Count) 
        {
            _index = 0;
        }

        var endpoint = _apiEndpoints[_index++];



        var res = await _xSender.RequestToBaseApi(endpoint, RoutePacket.Of(new UpdateServerInfoReq() { ServerInfo = serverInfo.ToMsg()}));

        UpdateServerInfoRes updateRes = UpdateServerInfoRes.Parser.ParseFrom(res.Payload.DataSpan);

        //_log.Info(() => $"update - [target endpoint:{endpoint},server count:{updateRes.ServerInfos.Count}]");

        var endpoints = (updateRes.ServerInfos.Where(e => e.ServiceId == _apiServiceId).Select(e => e.Endpoint).ToList());
        if(endpoints.Count > 0)
        {
            _apiEndpoints  = endpoints;
        }
        return updateRes.ServerInfos.Select(e => XServerInfo.Of(e)).ToList();
        //return updateRes.ServerInfos.Where(e=>e.Endpoint != serverInfo.GetBindEndpoint()).Select(e=>XServerInfo.Of(e)).ToList();
    }
}