using PlayHouse.Communicator.Message;
using Playhouse.Protocol;
using System.Collections.Concurrent;
using PlayHouse.Communicator;
using PlayHouse.Utils;

namespace PlayHouse.Service
{
    internal class BaseSystem
    {
        private readonly LOG<BaseSystem> _log = new ();
        private readonly IServerSystem _serverSystem;
        private readonly XSender _baseSender;

        private readonly Thread _thread;
        private readonly ConcurrentQueue<RoutePacket> _msgQueue;
        private volatile bool _running;

        private const short START = -100;
        private const short PAUSE = -101;
        private const short RESUME = -102;
        private const short STOP = -103;

        public BaseSystem(IServerSystem serverSystem, XSender baseSender)
        {
            _serverSystem = serverSystem;
            _baseSender = baseSender;
            _thread = new Thread(MessageLoop) { Name = "system:message-loop" };
            _msgQueue = new ConcurrentQueue<RoutePacket>();
            _running = true;
        }

        public void Start()
        {

            _msgQueue.Enqueue(RoutePacket.SystemOf(RoutePacket.Of(START,new EmptyPayload()), isBase: true));
            _thread.Start();
        }

        public void OnReceive(RoutePacket packet)
        {
            _msgQueue.Enqueue(packet);
        }

        public void Stop()
        {
            _msgQueue.Enqueue(RoutePacket.SystemOf(RoutePacket.Of(STOP, new EmptyPayload()), isBase: true));
            _running = false;
        }

        private void MessageLoop()
        {
            while (_running)
            {
                while (_msgQueue.TryDequeue(out RoutePacket? routePacket))
                {
                    do
                    {
                        using (routePacket)
                        {
                            try
                            {
                                if (routePacket.IsBase())
                                {
                                    switch (routePacket.MsgId)
                                    {
                                        case START:
                                            _serverSystem.OnStart();
                                            break;
                                        case PAUSE:
                                            _serverSystem.OnPause();
                                            break;
                                        case RESUME:
                                            _serverSystem.OnResume();
                                            break;
                                        case STOP:
                                            _serverSystem.OnStop();
                                            break;
                                        default:
                                            _log.Error(()=>$"Invalid baseSystem packet - [packetInfo:{routePacket.RouteHeader}");
                                            break;
                                    }
                                }
                                else
                                {
                                    _baseSender.SetCurrentPacketHeader(routePacket.RouteHeader);
                                    _serverSystem.OnDispatch(routePacket.ToContentsPacket(routePacket.MsgSeq));
                                }
                            }
                            catch (Exception e)
                            {
                                _log.Error(()=>e.ToString());
                                _baseSender.Reply((int)BaseErrorCode.SystemError);
                            }
                            finally
                            {
                                _baseSender.ClearCurrentPacketHeader();
                            }
                        }
                        
                    } while (_msgQueue.TryDequeue(out routePacket));
                }
                Thread.Sleep(ConstOption.ThreadSleep);
            }
        }

        public void Pause()
        {
            _msgQueue.Enqueue(RoutePacket.SystemOf(RoutePacket.Of(PAUSE, new EmptyPayload()), isBase: true));
        }

        public void Resume()
        {
            _msgQueue.Enqueue(RoutePacket.SystemOf(RoutePacket.Of(RESUME, new EmptyPayload()), isBase: true));
        }
    }
}
