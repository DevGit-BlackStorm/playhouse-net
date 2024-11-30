﻿using System.Collections.Concurrent;
using PlayHouse.Communicator.Message;
using PlayHouse.Communicator.PlaySocket;
using PlayHouse.Utils;

namespace PlayHouse.Communicator;

internal class XClientCommunicator(IPlaySocket playSocket) : IClientCommunicator
{
    private readonly HashSet<string> _connected = new();
    private readonly HashSet<string> _disconnected = new();
    private readonly LOG<XClientCommunicator> _log = new();
    private readonly BlockingCollection<Action> _queue = new();

    public void Connect(string endpoint)
    {
        if (!_connected.Add(endpoint))
        {
            return;
        }

        _queue.Add(() =>
        {
            try
            {
                playSocket.Connect(endpoint);
                //_connected.Add(endpoint);
                //_disconnected.Remove(endpoint);
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

        //_queue.Enqueue(() =>
        //{
        //    try
        //    {
        //        playSocket.Disconnect(endpoint);
        //        _log.Info(() => $"disconnected with {endpoint}");
        //    }
        //    catch (Exception ex)
        //    {
        //        _log.Error(() => $"disconnect error - endpoint:{endpoint}, error:{ex.Message}");
        //    }
        //    finally
        //    {
        //        _connected.Remove(endpoint);
        //        _disconnected.Add(endpoint);
        //    }
        //});
    }

    public void Stop()
    {
        _queue.CompleteAdding();
    }

    public void Send(int nid, RoutePacket routePacket)
    {
        _log.Trace(() => $"before send queue:{nid} - [accountId:{routePacket.AccountId.ToString():accountId},packetInfo:{routePacket.RouteHeader}]");

        //if (_connected.Contains(endpoint) == false)
        //{
        //    _log.Error(() => $"socket is not connected : [accountId:{routePacket.AccountId},target endpoint:{endpoint},target msgId:{routePacket.MsgId}]");
        //    return;
        //}

        _queue.Add(() =>
        {
            try
            {
                using (routePacket)
                {
                    _log.Trace(() => $"sendTo: nid:{nid} - [accountId:{routePacket.AccountId.ToString():accountId},packetInfo:{routePacket.RouteHeader}]");
                    playSocket.Send(nid, routePacket);
                }
            }
            catch (Exception e)
            {
                _log.Error(
                    () =>
                        $"socket send error : [target nid:{nid},target msgId:{routePacket.MsgId},accountId:{routePacket.AccountId.ToString():accountId}] - {e.Message}"
                );
            }
        });

    }

    public void Communicate()
    {
        //var action = _jobBucket.Get();

        // _queue.CompleteAdding() 가 호출되기 전까지 루프
        foreach (var action in _queue.GetConsumingEnumerable())
        {
            try
            {
                action.Invoke();
            }
            catch (Exception e)
            {
                _log.Error(
                    () => $"{playSocket.EndPoint()} Error during communication - {e.Message}"
                );
            }
        }

        //Thread.Yield();
        //Thread.Sleep(ConstOption.ThreadSleep);
    }
}