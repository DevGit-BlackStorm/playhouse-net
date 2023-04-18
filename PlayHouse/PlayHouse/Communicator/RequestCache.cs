using Playhouse.Protocol;
using PlayHouse.Communicator.Message;
using System.Collections.Specialized;
using System.Runtime.Caching;

namespace PlayHouse.Communicator
{
    public class ReplyObject
    {
        private ReplyCallback? _replyCallback = null;
        private TaskCompletionSource<ReplyPacket>? _taskCompletionSource= null;
        public ReplyObject(ReplyCallback? callback = null, TaskCompletionSource<ReplyPacket>? taskCompletionSource = null)  
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

        public void Throw(short errorCode)
        {
            _replyCallback?.Invoke(new ReplyPacket(errorCode));
            _taskCompletionSource?.SetResult(new ReplyPacket(errorCode));
            
        }
    }
    public class RequestCache
    {
        private int _atomicInt;
        private CacheItemPolicy _policy;
        private MemoryCache _cache;

        public RequestCache(int timeout) 
        {
            _policy = new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(timeout) };

            // Set a callback to be called when the cache item is removed
            _policy.RemovedCallback = new CacheEntryRemovedCallback((args) => {
                if (args.RemovedReason == CacheEntryRemovedReason.Expired)
                {
                    var replyObject = (ReplyObject)args.CacheItem.Value;
                    replyObject.Throw((int)BaseErrorCode.RequestTimeout);
                }
            });

            var cacheSettings = new NameValueCollection();
            cacheSettings.Add("CacheMemoryLimitMegabytes", "10");
            cacheSettings.Add("PhysicalMemoryLimitPercentage", "1");
            _cache = new MemoryCache("RequestCache", cacheSettings);
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
                int msgId = routePacket.Header().MsgId;
                string key = msgSeq.ToString();
                ReplyObject replyObject = (ReplyObject)MemoryCache.Default.Get(key);

                if (replyObject != null)
                {
                    replyObject.OnReceive(routePacket);
                    MemoryCache.Default.Remove(key);
                }
                else
                {
                    LOG.Error($"{msgSeq},${msgId} request is not exist", this.GetType());
                }
            }catch (Exception e)
            {
                LOG.Error($"{e.StackTrace}",this.GetType(), e);
            }
            
        }
    }

}
