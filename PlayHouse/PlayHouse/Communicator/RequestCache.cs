using Playhouse.Protocol;
using PlayHouse.Communicator.Message;
using System.Runtime.Caching;

namespace PlayHouse.Communicator
{
    public class ReplyObject
    {
        private Action<ReplyPacket>? _replyCallback = null;
        private TaskCompletionSource<ReplyPacket>? _taskCompletionSource= null;
        public ReplyObject(Action<ReplyPacket>? callback = null, TaskCompletionSource<ReplyPacket>? taskCompletionSource = null)  
        { 
            this._replyCallback = callback;
            this._taskCompletionSource = taskCompletionSource;
        }

        public void OnReceive(RoutePacket routePacket)
        {
            using (routePacket) { 
                _replyCallback?.Invoke(routePacket.ToReplyPacket());
                _taskCompletionSource?.SetResult(routePacket.ToReplyPacket());
            }
        }

        public void Throw(int errorCode)
        {
            _replyCallback?.Invoke(new ReplyPacket(errorCode,""));
            _taskCompletionSource?.SetResult(new ReplyPacket(errorCode, ""));
            
        }
    }
    public class RequestCache
    {
        private ILogger _log ;
        private int _atomicInt;
        private CacheItemPolicy _policy;

        public RequestCache(int timeout,ILogger logger) 
        {
            _log = logger;
            _policy = new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(timeout) };

            // Set a callback to be called when the cache item is removed
            _policy.RemovedCallback = new CacheEntryRemovedCallback((args) => {
                if (args.RemovedReason == CacheEntryRemovedReason.Expired)
                {
                    var replyObject = (ReplyObject)args.CacheItem.Value;
                    replyObject.Throw((int)BaseErrorCode.RequestTimeout);
                }
            });
        }

        public int GetSequence()
        {
            return Interlocked.Increment(ref _atomicInt);
        }

        public void Put(int seq,ReplyObject replyObject)
        {
            var cacheItem = new CacheItem(seq.ToString(), replyObject);
            MemoryCache.Default.Add(cacheItem, _policy);
        }

        public ReplyObject? Get(int seq)
        {
            return (ReplyObject)MemoryCache.Default.Get(seq.ToString());
        }

        public void OnReply(RoutePacket routePacket)
        {
            try
            {
                int msgSeq = routePacket.Header().MsgSeq;
                string msgName = routePacket.Header().MsgName;
                ReplyObject replyObject = (ReplyObject)MemoryCache.Default.Get(msgSeq.ToString());

                if (replyObject != null)
                {
                    replyObject.OnReceive(routePacket);
                    MemoryCache.Default.Remove(msgSeq.ToString());
                }
                else
                {
                    _log.Error($"{msgSeq},${msgName} request is not exist", nameof(RequestCache));
                }
            }catch (Exception e)
            {
                _log.Error($"{e.StackTrace}",nameof(RequestCache), e);
            }
            
        }
    }

}
