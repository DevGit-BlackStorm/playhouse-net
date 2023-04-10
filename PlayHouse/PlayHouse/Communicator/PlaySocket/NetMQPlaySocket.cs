﻿using CommonLib;
using Google.Protobuf;
using NetMQ;
using NetMQ.Sockets;
using Playhouse.Protocol;
using PlayHouse.Communicator.Message;
using System.Text;

namespace PlayHouse.Communicator.PlaySocket
{

    public class NetMQPlaySocket : IPlaySocket
    {
        private readonly RouterSocket _socket = new();
        private readonly String _bindEndpoint = "";
        private RingBuffer _buffer = new RingBuffer(ConstOption.MAX_PACKET_SIZE);

        public NetMQPlaySocket(SocketConfig socketConfig,String bindEndpoint)
        {
            _socket.Options.Identity = Encoding.UTF8.GetBytes(bindEndpoint);
            _socket.Options.DelayAttachOnConnect = true; // immediate
            _socket.Options.RouterHandover = true;
            _socket.Options.Backlog = socketConfig.BackLog;
            _socket.Options.Linger = TimeSpan.FromMilliseconds(socketConfig.Linger);
            _socket.Options.TcpKeepalive = true;
            _socket.Options.SendBuffer = socketConfig.SendBufferSize;
            _socket.Options.ReceiveBuffer = socketConfig.ReceiveBufferSize;
            _socket.Options.ReceiveHighWatermark = socketConfig.ReceiveHighWatermark;
            _socket.Options.SendHighWatermark = socketConfig.SendHighWatermark;
            _socket.Options.RouterMandatory = true;

            this._bindEndpoint = bindEndpoint;
        }

        public void Bind()
        {
            _socket.Bind(_bindEndpoint);
            LOG.Info($"socket bind {_bindEndpoint}",this.GetType());
        }

        public void Close()
        {
            _socket.Close();
        }

        public void Connect(string endpoint)
        {
            _socket.Connect(endpoint);
        }

        public void Disconnect(string endpoint)
        {
            _socket.Disconnect(endpoint);
        }

        public string GetBindEndpoint()
        {
            return _bindEndpoint;
        }

        public string Id()
        {
            return _bindEndpoint;
        }

        public RoutePacket? Receive()
        {
            NetMQMessage? message = new NetMQMessage();
            if(_socket.TryReceiveMultipartMessage(ref message))
            {
                if(message.Count() < 3)
                {
                    LOG.Error($"message size is invalid : {message.Count()}", this.GetType());
                    return null;
                }

                String target = Encoding.UTF8.GetString(message[0].Buffer);
                var header = RouteHeaderMsg.Parser.ParseFrom(message[1].Buffer);
                PooledBufferPayload payload = new(new (message[2].Buffer));

                var routePacket = RoutePacket.Of(new (header),payload);
                routePacket.RouteHeader.From = target;
                return routePacket;
            }
            return null;
        }

        public void Send(string endpoint, RoutePacket routerPacket)
        {
            using (routerPacket)
            {
                NetMQMessage message = new NetMQMessage();
                IPayload payload = routerPacket.GetPayload();

                _buffer.Clear();
                if (routerPacket.ForClient())
                {
                    routerPacket.WriteClientPacketBytes(_buffer);
                }
                else
                {
                    _buffer.Write(payload.Data);
                }
                                
                message.Append(new NetMQFrame(Encoding.UTF8.GetBytes(endpoint)));
                var routerHeaderMsg = routerPacket.RouteHeader.ToMsg();

                var headerSize = routerHeaderMsg.CalculateSize();
                var headerFrame = new NetMQFrame(headerSize);
                routerHeaderMsg.WriteTo(new MemoryStream(headerFrame.Buffer));

                message.Append(headerFrame);
                message.Append(new NetMQFrame(_buffer.Buffer(),_buffer.Count));

                if (!_socket.TrySendMultipartMessage(message))
                {
                    LOG.Error($"Send fail to {endpoint}, MsgName:{routerPacket.GetMsgId}", this.GetType());
                }
            }
            
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}