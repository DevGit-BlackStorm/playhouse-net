using PlayHouse.Communicator.Message;
using Playhouse.Protocol;
using System.Collections.Concurrent;
using PlayHouse.Communicator;

namespace PlayHouse.Service
{
    public class BaseSystem
    {
        private readonly IServerSystem _serverSystem;
        private readonly BaseSender _baseSender;

        private readonly Thread _thread;
        private readonly ConcurrentQueue<RoutePacket> _msgQueue;
        private volatile bool _running;

        private const short START = -100;
        private const short PAUSE = -101;
        private const short RESUME = -102;
        private const short STOP = -103;

        public BaseSystem(IServerSystem serverSystem, BaseSender baseSender)
        {
            _serverSystem = serverSystem;
            _baseSender = baseSender;
            _thread = new Thread(MessageLoop) { Name = "system:message-loop" };
            _msgQueue = new ConcurrentQueue<RoutePacket>();
            _running = true;
        }

        public void Start()
        {
            _msgQueue.Enqueue(RoutePacket.SystemOf(new Packet(START), isBase: true));
            _thread.Start();
        }

        public void OnReceive(RoutePacket packet)
        {
            _msgQueue.Enqueue(packet);
        }

        public void Stop()
        {
            _msgQueue.Enqueue(RoutePacket.SystemOf(new Packet(STOP), isBase: true));
            _running = false;
        }

        private void MessageLoop()
        {
            while (_running)
            {
                if (_msgQueue.TryDequeue(out RoutePacket? routePacket))
                {
                    do
                    {
                        using (routePacket)
                        {
                            try
                            {
                                if (routePacket.IsBase())
                                {
                                    switch (routePacket.GetMsgId())
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
                                            LOG.Error($"Invalid baseSystem packet {routePacket.GetMsgId()}", this.GetType());
                                            break;
                                    }
                                }
                                else
                                {
                                    _baseSender.SetCurrentPacketHeader(routePacket.RouteHeader);
                                    _serverSystem.OnDispatch(new Packet(routePacket.GetMsgId(), routePacket.MovePayload()));
                                }
                            }
                            catch (Exception e)
                            {
                                LOG.Error(e.StackTrace, this.GetType());
                                _baseSender.ErrorReply(routePacket.RouteHeader, (int)BaseErrorCode.SystemError);
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
            _msgQueue.Enqueue(RoutePacket.SystemOf(new Packet(PAUSE), isBase: true));
        }

        public void Resume()
        {
            _msgQueue.Enqueue(RoutePacket.SystemOf(new Packet(RESUME), isBase: true));
        }
    }
}
