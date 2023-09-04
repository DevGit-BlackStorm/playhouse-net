using Moq;
using PlayHouse.Communicator;
using PlayHouse.Communicator.Message;
using PlayHouse.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace PlayHouseTests.Service
{
    public class TimerManagerTest
    {
        [Fact]
        public void RegisterRepeatTimer_Should_Invoke_Callback_Three_Times()
        {
            var processor = new Mock<IProcessor>();
            var timerManager = new TimerManager(processor.Object);

            timerManager.RegisterRepeatTimer(Guid.Empty, 1, 100, 100, async () =>  { await Task.CompletedTask; });

            Thread.Sleep(350);

            processor.Verify(p => p.OnReceive(It.IsAny<RoutePacket>()), Times.AtLeast(3));
        }

        [Fact]
        public void RegisterCountTimer_Should_Invoke_Callback_Three_Times()
        {
            var processor = new Mock<IProcessor>();
            var timerManager = new TimerManager(processor.Object);

            timerManager.RegisterCountTimer(Guid.Empty, 2, 0, 3, 100, async () => { await Task.CompletedTask; });

            Thread.Sleep(500);

            processor.Verify(p => p.OnReceive(It.IsAny<RoutePacket>()), Times.Exactly(3));
        }

        [Fact]
        public void CancelTimer_Should_Not_Invoke_Callback_After_Cancel()
        {
            var processor = new Mock<IProcessor>();
            var timerManager = new TimerManager(processor.Object);

            var timerId = timerManager.RegisterCountTimer(Guid.NewGuid(), 1, 50, 3, 10, async () => { await Task.CompletedTask; });

            timerManager.CancelTimer(timerId);

            Thread.Sleep(300);

            processor.Verify(p => p.OnReceive(It.IsAny<RoutePacket>()), Times.Never);
        }
    }
}
