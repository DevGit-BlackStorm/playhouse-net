using Moq;
using PlayHouse.Communicator.Message;
using PlayHouse.Service.Play;
using PlayHouse.Service.Shared;
using Xunit;

namespace PlayHouseTests.Service;

public class TimerManagerTest
{
    [Fact]
    public void RegisterRepeatTimer_Should_Invoke_Callback_Three_Times()
    {
        var processor = new Mock<IPlayDispatcher>();
        var timerManager = new TimerManager(processor.Object);

        timerManager.RegisterRepeatTimer(0, 1, 100, 100, async () => { await Task.CompletedTask; });

        Thread.Sleep(350);

        processor.Verify(p => p.OnPost(It.IsAny<RoutePacket>()), Times.AtLeast(3));
    }

    [Fact]
    public void RegisterCountTimer_Should_Invoke_Callback_Three_Times()
    {
        var processor = new Mock<IPlayDispatcher>();
        var timerManager = new TimerManager(processor.Object);

        timerManager.RegisterCountTimer(0, 2, 0, 3, 100, async () => { await Task.CompletedTask; });

        Thread.Sleep(500);

        processor.Verify(p => p.OnPost(It.IsAny<RoutePacket>()), Times.Exactly(3));
    }

    [Fact]
    public void CancelTimer_Should_Not_Invoke_Callback_After_Cancel()
    {
        var processor = new Mock<IPlayDispatcher>();
        var timerManager = new TimerManager(processor.Object);

        var timerId = timerManager.RegisterCountTimer(0, 1, 50, 3, 10, async () => { await Task.CompletedTask; });

        timerManager.CancelTimer(timerId);

        Thread.Sleep(300);

        processor.Verify(p => p.OnPost(It.IsAny<RoutePacket>()), Times.Never);
    }
}