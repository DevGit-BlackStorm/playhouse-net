﻿using PlayHouse.Communicator.PlaySocket;
using Playhouse.Protocol;
using PlayHouse.Utils;

namespace PlayHouse.Communicator;

internal class XServerCommunicator(IPlaySocket playSocket) : IServerCommunicator
{
    private readonly LOG<XServerCommunicator> _log = new();

    private ICommunicateListener? _listener;
    private bool _running = true;

    public void Bind(ICommunicateListener listener)
    {
        _listener = listener;
        playSocket.Bind();
    }


    public void Communicate()
    {
        while (_running)
        {
            var packet = playSocket.Receive();
            while (packet != null)
            {
                try
                {
                    if (packet.MsgId != UpdateServerInfoReq.Descriptor.Name &&
                        packet.MsgId != UpdateServerInfoRes.Descriptor.Name)
                    {
                        _log.Trace(() => $"recvFrom:{packet.RouteHeader.From} - [packetInfo:${packet.RouteHeader}]");
                    }

                    _listener!.OnReceive(packet);
                }
                catch (Exception e)
                {
                    _log.Error(() => $"{playSocket.Id()} Error during communication - {e.Message}");
                }

                packet = playSocket.Receive();
            }

            Thread.Sleep(ConstOption.ThreadSleep);
        }
    }

    public void Stop()
    {
        _running = false;
    }
}