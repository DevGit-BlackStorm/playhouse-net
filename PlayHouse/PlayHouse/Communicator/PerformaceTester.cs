using System.Diagnostics;
using PlayHouse.Utils;

namespace PlayHouse.Communicator;

internal class PerformanceTester
{
    private readonly string _from;
    private readonly LOG<MessageLoop> _log = new();
    private readonly bool _showQps;
    private readonly Stopwatch _stopWatch = new();
    private readonly Timer _timer;
    private int _counter;

    public PerformanceTester(bool showQps, string from = "Server")
    {
        _showQps = showQps;
        _from = from;
        _timer = new Timer(obj => { Qps(); }, null, Timeout.Infinite, Timeout.Infinite);
    }

    public void IncCounter()
    {
        if (_showQps)
        {
            Interlocked.Increment(ref _counter);
        }
    }

    public void Stop()
    {
        if (_showQps)
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }
    }

    public void Start()
    {
        if (_showQps)
        {
            _stopWatch.Start();
            _timer.Change(1000, 1000);
        }
    }

    private void Qps()
    {
        try
        {
            _stopWatch.Stop();
            var messageCount = Interlocked.Exchange(ref _counter, 0);
            var seconds = _stopWatch.ElapsedMilliseconds / 1000L;

            var qps = messageCount == 0 || seconds == 0L ? 0 : messageCount / (int)seconds;

            _log.Info(() => $"{_from}, {messageCount}, qps: {qps}");
        }
        finally
        {
            _stopWatch.Reset();
            _stopWatch.Start();
        }
    }
}