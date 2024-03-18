using PlayHouse.Communicator;
using PlayHouse.Communicator.Message;
using PlayHouse.Utils;
using System.Collections.Concurrent;
namespace PlayHouse.Service.Shared;

internal class PacketWorkerQueue
{
    private readonly Thread dispatchThread;
    private readonly ConcurrentQueue<RoutePacket> _queue = new();
    private bool _running = true;
    private readonly LOG<PacketWorkerQueue> _log = new();
    private readonly Func<RoutePacket,Task> _action;

    public PacketWorkerQueue(Func<RoutePacket, Task> action)
    {
        dispatchThread = new Thread(Dispatch);
        _action = action;
    }
    public void Start()
    {
        _running = true;
        dispatchThread.Start();
    }


    public void Stop()
    {
        _running = false;
    }

    void Dispatch()
    {
        while (_running)
        {
            while (_queue.TryDequeue(out var routePacket))
            {
                try
                {
                    Task.Run(async () => {  await _action(routePacket); });
                }
                catch (Exception e)
                {
                    _log.Error(
                        () => $"Error during dispatch - {e.Message}"
                    );
                }
            }
            Thread.Sleep(ConstOption.ThreadSleep);
        }
    }


    public void Post(RoutePacket routePacket)
    {
        _queue.Enqueue(routePacket);
    }
}

