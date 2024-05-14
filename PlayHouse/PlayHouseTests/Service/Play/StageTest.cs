using Docker.DotNet.Models;
using Moq.Protected;
using Moq;
using Org.Ulalax.Playhouse.Protocol;
using PlayHouse.Communicator.Message;
using PlayHouse.Service.Play.Base;
using PlayHouse.Service.Play;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using PlayHouse.Communicator;
using Google.Protobuf;
using Playhouse.Protocol;
using FluentAssertions;
using System.Runtime.CompilerServices;
using System.Reflection;
using PlayHouse.Production.Play;
using PlayHouse.Service.Shared;
using PlayHouse.Production.Shared;

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
namespace PlayHouseTests.Service.Play
{

    public class StageTest
    {
        private readonly List<RoutePacket> _resultList = new();
        private readonly string _stageType = "dungeon";
        //private readonly long _testStageId = 0;
        private readonly string _sessionEndpoint = "tcp://127.0.0.1:5555";
        private readonly string _bindEndpoint = "tcp://127.0.0.1:8777";
        private BaseStage _stage;
        private XStageSender _xStageSender;
        private IStage _contentStage = Mock.Of<IStage>();
        private long _stageId  =0;
        Mock<IClientCommunicator> _clientCommunicator;
        private long _accountId = 0;

     
        public StageTest()
        {
            _clientCommunicator = new Mock<IClientCommunicator>();
            var reqCache = new RequestCache(0);
            var playOption = new PlayOption();

            playOption.PlayProducer.Register(
                _stageType,
                stageSender => _contentStage,
                actorSender => Mock.Of<IActor>()
            );
            var serverInfoCenter = Mock.Of<IServerInfoCenter>();

            //playProcessor = new PlayService(
            //    2,
            //    _bindEndpoint,
            //    playOption,
            //    _clientCommunicator.Object,
            //    reqCache,
            //    Mock.Of<IServerInfoCenter>()
            //);
            var playDispacher = new PlayDispatcher(2, _clientCommunicator.Object, reqCache, serverInfoCenter, _bindEndpoint, playOption);
            playDispacher.Start();
            _xStageSender = new XStageSender(2, _stageId, playDispacher, _clientCommunicator.Object, reqCache);

            Mock<ISessionUpdater> sessionUpdator = new Mock<ISessionUpdater>();

            sessionUpdator.Setup(updator=>updator.UpdateStageInfo(It.IsAny<string>(),It.IsAny<int>())).Returns(Task.FromResult(1));


            _stage = new BaseStage(
                _stageId,
                playDispacher,
                _clientCommunicator.Object,
                reqCache,
                serverInfoCenter,
                sessionUpdator.Object,
                _xStageSender
            );



            Mock.Get(_contentStage)
                .Setup(stage => stage.OnCreate(It.IsAny<IPacket>()))
                .Returns(Task.FromResult(((ushort)0, (IPacket)CPacket.Of(new TestMsg { TestMsg_ = "onCreate" })))); 

            Mock.Get(_contentStage)
                .Setup(stage => stage.OnJoinStage(It.IsAny<IActor>(), It.IsAny<IPacket>()))
                .Returns(Task.FromResult(((ushort)0, (IPacket)CPacket.Of(new TestMsg { TestMsg_ = "onJoinStage" })))); 
        }

        [Fact]
        public void CreateRoom_ShouldSucceed()
        {
            // given
            PacketContext.AsyncCore.Init();
            PacketProducer.Init((int msgId, IPayload payload, ushort msgSeq) => new TestPacket(msgId, payload, msgSeq));

            List<RoutePacket> result = new List<RoutePacket>();
            _clientCommunicator.Setup(x => x.Send(It.IsAny<string>(), It.IsAny<RoutePacket>()))
            .Callback<string,RoutePacket>((sid,packet) => result.Add(packet));

            // when
            _stage.Post(CreateRoomPacket(_stageType));

            // then
            result[0].RouteHeader.Header.ErrorCode.Should().Be((ushort)BaseErrorCode.Success);

            result[0].MsgId.Should().Be(CreateStageRes.Descriptor.Index);
            var createStageRes = CreateStageRes.Parser.ParseFrom(result[0].Span);

            createStageRes.PayloadId.Should().Be(TestMsg.Descriptor.Index);

            TestMsg.Parser.ParseFrom(createStageRes.Payload).TestMsg_.Should().Be("onCreate");
        }

        [Fact]
        public async Task CreateRoom_WithInvalidType_ShouldReturnInvalidError()
        {
            // given
            PacketContext.AsyncCore.Init();

            List<RoutePacket> result = new List<RoutePacket>();
            _clientCommunicator.Setup(x => x.Send(It.IsAny<string>(), It.IsAny<RoutePacket>()))
            .Callback<string, RoutePacket>((sid, packet) => result.Add(packet));

            // when
             _stage.Post(CreateRoomPacket("invalid type"));

            // then
            result[0].RouteHeader.Header.ErrorCode.Should().Be((ushort)BaseErrorCode.StageTypeIsInvalid);
            await Task.CompletedTask;
        }

        [Fact]
        public void CreateJoinRoomInCreateState_ShouldBeSuccess()
        {
            PacketContext.AsyncCore.Init();
            PacketProducer.Init((int msgId, IPayload payload, ushort msgSeq) => new TestPacket(msgId, payload, msgSeq));

            List<RoutePacket> result = new List<RoutePacket>();
            _clientCommunicator.Setup(x => x.Send(It.IsAny<string>(), It.IsAny<RoutePacket>()))
            .Callback<string, RoutePacket>((sid, packet) => result.Add(packet));

            var createJoinRoom = CreateJoinRoomPacket(_stageType, _stageId, _accountId);
            _stage.Post(createJoinRoom);


            result[0].MsgId.Should().Be(CreateJoinStageRes.Descriptor.Index);
            var createJoinStageRes = CreateJoinStageRes.Parser.ParseFrom(result[0].Span);

            createJoinStageRes.IsCreated.Should().BeTrue();
            createJoinStageRes.CreatePayloadId.Should().Be(TestMsg.Descriptor.Index);
            createJoinStageRes.JoinPayloadId.Should().Be(TestMsg.Descriptor.Index);

            TestMsg.Parser.ParseFrom(createJoinStageRes.CreatePayload).TestMsg_.Should().Be("onCreate");
            TestMsg.Parser.ParseFrom(createJoinStageRes.JoinPayload).TestMsg_.Should().Be("onJoinStage");
        }

        [Fact]
        public void TestCreateJoinRoomInJoinState()
        {
            // Arrange
            PacketContext.AsyncCore.Init();
            PacketProducer.Init((int msgId, IPayload payload, ushort msgSeq) => new TestPacket(msgId, payload, msgSeq));

            CreateRoomWithSuccess();

            List<RoutePacket> result = new List<RoutePacket>();
            _clientCommunicator.Setup(x => x.Send(It.IsAny<string>(), It.IsAny<RoutePacket>()))
            .Callback<string, RoutePacket>((sid, packet) => result.Add(packet));

            var createJoinRoom = CreateJoinRoomPacket(_stageType, _stageId, _accountId);
            // Act
            _stage.Post(createJoinRoom);

            // Assert
            CreateJoinStageRes.Descriptor.Index.Should().Be(result[0].MsgId);
            var createJoinStageRes = CreateJoinStageRes.Parser.ParseFrom(result[0].Span);

            createJoinStageRes.IsCreated.Should().BeFalse();
            createJoinStageRes.CreatePayloadId.Should().Be(0);
            createJoinStageRes.JoinPayloadId.Should().Be(TestMsg.Descriptor.Index);
        }

        [Fact]
        public void AsyncBlock_ShouldRunBlocking()
        {
            string result = "";
            _stage.Post(AsyncBlockPacket.Of(_stageId, async arg => { result = (string)arg; await Task.CompletedTask; }, "test async block"));
            Assert.Equal("test async block", result);
        }

        private RoutePacket CreateRoomPacket(string stageType)
        {
            var packet = RoutePacket.Of(new CreateStageReq
            {
                StageType = stageType
            });

            var result = RoutePacket.StageOf(0, 0, packet, true, true);
            result.SetMsgSeq(1);
            return result;
        }

        private RoutePacket JoinRoomPacket(long stageId, long accountId)
        {
            var packet = RoutePacket.Of(new JoinStageReq
            {
                SessionEndpoint = _sessionEndpoint,
                Sid = 1,
                PayloadId = 1,
                Payload = ByteString.Empty
            });
            var result = RoutePacket.StageOf(stageId, accountId, packet, true, true);
            result.SetMsgSeq(2);
            return result;
        }

        private RoutePacket CreateJoinRoomPacket(string stageType, long stageId, long accountId)
        {
            var req = new CreateJoinStageReq
            {
                StageType = stageType,
                SessionEndpoint = _sessionEndpoint,
                Sid = 1,
                CreatePayloadId = 1,
                CreatePayload = ByteString.Empty,
                JoinPayloadId = 2,
                JoinPayload = ByteString.Empty
            };
            var packet = RoutePacket.Of(req);
            var result = RoutePacket.StageOf(stageId, accountId, packet, true, true);
            result.SetMsgSeq(3);
            return result;
        }

        private void CreateRoomWithSuccess()
        {
            var result = new List<RoutePacket>();
            _clientCommunicator.Setup(c => c.Send(It.IsAny<string>(), It.IsAny<RoutePacket>()))
            .Callback<string, RoutePacket>((sid, packet) => result.Add(packet));

            _stage.Post(CreateRoomPacket(_stageType));


            result[0].RouteHeader.Header.ErrorCode.Should().Be((ushort)BaseErrorCode.Success);

            var createStageRes = CreateStageRes.Parser.ParseFrom(result[0].Span);
        }
    }
}
