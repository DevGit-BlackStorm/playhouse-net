using Microsoft.Extensions.Logging.Abstractions;
using PlayHouse.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Production;



public class AsyncCore : IAsyncCore
{
    private class ErrorCodeWrapper
    {
        public ushort Code { get; set; }
    }

    public AsyncCore() { }

    private readonly AsyncLocal<IApiSender?> _apiSender = new();
    private readonly AsyncLocal<ErrorCodeWrapper?> _errorCode = new();
    private readonly AsyncLocal<List<(SendTarget target, IPacket packet)>?> _sendPackets = new();

    public void Init(IApiSender? apiSender = null)
    {
        _apiSender.Value = apiSender;
        _errorCode.Value = new();
        _sendPackets.Value = new();
    }

    public IApiSender GetApiSender()
    {
        return _apiSender.Value ?? new NullApiSender();
    }

    public ushort GetErrorCode()
    {
        return _errorCode.Value != null ? _errorCode.Value.Code : (ushort)0;
    }

    public List<(SendTarget target, IPacket packet)> GetSendPackets()
    {
        return _sendPackets.Value != null ? _sendPackets.Value : new();
    }

    public void SetErrorCode(ushort errorCode)
    {
        if (_errorCode.Value != null)
        {
            _errorCode.Value.Code = errorCode;
        }
    }

    public void SetApiSender(IApiSender apiSender)
    {
        _apiSender.Value = apiSender;
    }

    public void Add(SendTarget target, IPacket packet)
    {
        if (_sendPackets.Value != null)
        {
            _sendPackets.Value.Add((target, packet));
        }
    }

    public void Clear()
    {
        _apiSender.Value = null;
        _errorCode.Value = null;
        _sendPackets.Value = null;
    }
}

internal class NullApiSender : IApiSender
{
    public string SessionEndpoint => string.Empty;

    public int Sid => 0;

    public string AccountId => string.Empty;

    public ushort ServiceId => 0;

    public void Authenticate(string accountId)
    {
    }

    public Task<CreateJoinStageResult> CreateJoinStage(string playEndpoint, string stageType, string stageId, IPacket createPacket, IPacket joinPacket)
    {
        throw new Exception("ApiSender is not set in AsyncLocal. If you are running unit tests, please use mocking.");
    }

    public Task<CreateStageResult> CreateStage(string playEndpoint, string stageType, string stageId, IPacket packet)
    {
        throw new Exception("ApiSender is not set in AsyncLocal. If you are running unit tests, please use mocking.");
    }

    public Task<JoinStageResult> JoinStage(string playEndpoint, string stageId, IPacket packet)
    {
        throw new Exception("ApiSender is not set in AsyncLocal. If you are running unit tests, please use mocking.");
    }

    public void Reply(ushort errorCode, IPacket? reply = null)
    {
    }

    public void Reply(IPacket reply)
    {
    }

    public void RequestToApi(string apiEndpoint, IPacket packet, ReplyCallback replyCallback)
    {

        throw new Exception("ApiSender is not set in AsyncLocal. If you are running unit tests, please use mocking.");
    }

    public Task<(ushort errorCode, IPacket reply)> RequestToApi(string apiEndpoint, IPacket packet)
    {
        throw new Exception("ApiSender is not set in AsyncLocal. If you are running unit tests, please use mocking.");
    }

    public void RequestToStage(string playEndpoint, string stageId, string accountId, IPacket packet, ReplyCallback replyCallback)
    {
        throw new Exception("ApiSender is not set in AsyncLocal. If you are running unit tests, please use mocking.");
    }

    public Task<(ushort errorCode, IPacket reply)> RequestToStage(string playEndpoint, string stageId, string accountId, IPacket packet)
    {
        throw new Exception("ApiSender is not set in AsyncLocal. If you are running unit tests, please use mocking.");
    }

    public Task<(ushort errorCode, IPacket reply)> RequestToSystem(string endpoint, IPacket packet)
    {
        throw new Exception("ApiSender is not set in AsyncLocal. If you are running unit tests, please use mocking.");
    }

    public void SendToApi(string apiEndpoint, IPacket packet)
    {

    }

    public void SendToClient(string sessionEndpoint, int sid, IPacket packet)
    {

    }

    public void SendToStage(string playEndpoint, string stageId, string accountId, IPacket packet)
    {

    }

    public void SendToSystem(string endpoint, IPacket packet)
    {
    }

    public void SessionClose(string sessionEndpoint, int sid)
    {
    }
}

public class AsyncStorage
{
    private IAsyncCore _core = new AsyncCore();
    public static IAsyncCore AsyncCore
    {
        get { return Instance._core; }
        set { Instance._core = value; }
    }
    public static AsyncStorage Instance { get; private set; } = new();

    //internal static IApiSender ApiSender
    //{
    //    get { return AsyncCore.GetApiSender(); }
    //    set { AsyncCore.SetApiSender(value); }
    //}
    //internal static ushort ErrorCode
    //{
    //    get { return AsyncCore.GetErrorCode(); }
    //    set { AsyncCore.SetErrorCode(value); }
    //}

    public static List<(SendTarget target, IPacket packet)> SendPackets => AsyncCore.GetSendPackets();

}