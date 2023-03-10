using PlayHouse.Communicator.Message;
using System.Collections.Concurrent;

namespace PlayHouse.Communicator
{
    public delegate void Action();

    public class JobBucket
    {
        private readonly Queue<Action> _queue = new();

        public void Add(Action job)
        {
            _queue.Enqueue(job);
        }

        public Action? Get()
        {
            return _queue.TryDequeue(out Action? job) ? job : null;
        }
    }

}
