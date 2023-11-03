using PlayHouse.Communicator.Message;
using PlayHouse.Communicator.PlaySocket;
using PlayHouse.Production;
using PlayHouse.Utils;
using System.Collections.Concurrent;

namespace PlayHouse.Communicator;
public class XClientCommunicator : IClientCommunicator
{
    private readonly IPlaySocket _playSocket;

    private readonly HashSet<string> _connected = new();
    private readonly HashSet<string> _disconnected = new();
    private readonly ConcurrentQueue<Action> _queue = new();
    private bool _running = true;
    private readonly LOG<XClientCommunicator> _log = new ();

    public XClientCommunicator(IPlaySocket playSocket)
    {
        _playSocket = playSocket;
    }

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
                _playSocket.Connect(endpoint);
                _connected.Add(endpoint);
                _disconnected.Remove(endpoint);
                _log.Info(()=>$"connected with {endpoint}");
            }
            catch(Exception ex) 
            {
                _log.Error(()=>$"connect error - endpoint:{endpoint}, error:{ex.Message}");
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
                _playSocket.Disconnect(endpoint);
                _log.Info(()=>$"disconnected with {endpoint}");
            }
            catch(Exception ex)
            {
                _log.Error(()=>$"disconnect error - endpoint:{endpoint}, error:{ex.Message}");
                
            }finally {
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
                    _log.Trace(() => $"sendTo:{endpoint} - [packetInfo:{routePacket.RouteHeader}]");
                    _playSocket.Send(endpoint, routePacket);
                }
            }
            catch (Exception e)
            {
                _log.Error(
                    ()=>$"{_playSocket.Id()} socket send error : {endpoint},{routePacket.MsgId} - {e.Message}"
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
                        ()=>$"{_playSocket.Id()} Error during communication - {e.Message}"
                    );
                }
            }
            Thread.Sleep(ConstOption.ThreadSleep);
        }
    }
}
