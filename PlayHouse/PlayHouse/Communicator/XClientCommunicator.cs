﻿using System.Collections.Concurrent;
using PlayHouse.Communicator.Message;
using PlayHouse.Communicator.PlaySocket;
using Playhouse.Protocol;
using PlayHouse.Utils;

namespace PlayHouse.Communicator;

internal class XClientCommunicator(IPlaySocket playSocket) : IClientCommunicator
{
    private readonly HashSet<string> _connected = new();
    private readonly HashSet<string> _disconnected = new();
    private readonly LOG<XClientCommunicator> _log = new();
    private readonly ConcurrentQueue<Action> _queue = new();
    private bool _running = true;

    public void Connect(string endpoint)
    {
        if (_connected.Contains(endpoint))
        {
            return;
        }

        _queue.Enqueue(() =>
        {
            try
            {
                playSocket.Connect(endpoint);
                _connected.Add(endpoint);
                _disconnected.Remove(endpoint);
                _log.Info(() => $"connected with {endpoint}");
            }
            catch (Exception ex)
            {
                _log.Error(() => $"connect error - endpoint:{endpoint}, error:{ex.Message}");
            }
        });
    }

    public void Disconnect(string endpoint)
    {
        if (_disconnected.Contains(endpoint))
        {
            return;
        }

        _queue.Enqueue(() =>
        {
            try
            {
                playSocket.Disconnect(endpoint);
                _log.Info(() => $"disconnected with {endpoint}");
            }
            catch (Exception ex)
            {
                _log.Error(() => $"disconnect error - endpoint:{endpoint}, error:{ex.Message}");
            }
            finally
            {
                _connected.Remove(endpoint);
                _disconnected.Add(endpoint);
            }
        });
    }

    public void Stop()
    {
        _running = false;
    }

    public void Send(string endpoint, RoutePacket routePacket)
    {
        _queue.Enqueue(() =>
        {
            try
            {
                using (routePacket)
                {
                    if (routePacket.MsgId != UpdateServerInfoReq.Descriptor.Name &&
                        routePacket.MsgId != UpdateServerInfoRes.Descriptor.Name)
                    {
                        _log.Trace(() => $"sendTo:{endpoint} - [packetInfo:{routePacket.RouteHeader}]");
                    }

                    playSocket.Send(endpoint, routePacket);
                }
            }
            catch (Exception e)
            {
                _log.Error(
                    () =>
                        $"socket send error : [target endpoint:{endpoint},target msgId:{routePacket.MsgId}] - {e.Message}"
                );
            }
        });
    }

    public void Communicate()
    {
        while (_running)
        {
            //var action = _jobBucket.Get();
            while (_queue.TryDequeue(out var action))
            {
                try
                {
                    action.Invoke();
                }
                catch (Exception e)
                {
                    _log.Error(
                        () => $"{playSocket.Id()} Error during communication - {e.Message}"
                    );
                }
            }

            Thread.Sleep(ConstOption.ThreadSleep);
        }
    }
}