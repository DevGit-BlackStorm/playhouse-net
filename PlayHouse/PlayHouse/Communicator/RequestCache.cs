using System.Collections.Specialized;
using System.Runtime.Caching;
using PlayHouse.Communicator.Message;
using Playhouse.Protocol;
using PlayHouse.Service.Shared;
using PlayHouse.Utils;

namespace PlayHouse.Communicator;

internal class ReplyObject(
    ReplyCallback? callback = null,
    TaskCompletionSource<RoutePacket>? taskCompletionSource = null)
{
    public void OnReceive(RoutePacket routePacket)
    {
        if (callback != null)
        {
            using (routePacket)
            {
                callback?.Invoke(routePacket.ErrorCode, CPacket.Of(routePacket));
            }
        }


        if (routePacket.ErrorCode == 0)
        {
            taskCompletionSource?.SetResult(routePacket);
        }
        else
        {
            Throw(routePacket.ErrorCode);
        }
    }

    public void Throw(ushort errorCode)
    {
        taskCompletionSource?.SetException(new PlayHouseException($"request has exception - errorCode:{errorCode}",
            errorCode));
    }
}

internal class RequestCache
{
    private readonly LOG<RequestCache> _log = new();
    private readonly CacheItemPolicy _policy;
    private readonly AtomicShort _sequence = new();
    private MemoryCache _cache;

    public RequestCache(int timeout)
    {
        _policy = new CacheItemPolicy();
        if (timeout > 0)
        {
            _policy.SlidingExpiration = TimeSpan.FromSeconds(timeout);
        }

        // Set a callback to be called when the cache item is removed
        _policy.RemovedCallback = args =>
        {
            if (args.RemovedReason == CacheEntryRemovedReason.Expired)
            {
                var replyObject = (ReplyObject)args.CacheItem.Value;
                replyObject.Throw((int)BaseErrorCode.RequestTimeout);
            }
        };

        var cacheSettings = new NameValueCollection
        {
            { "CacheMemoryLimitMegabytes", "10" },
            { "PhysicalMemoryLimitPercentage", "1" }
        };
        _cache = new MemoryCache("RequestCache", cacheSettings);
    }

    public ushort GetSequence()
    {
        return _sequence.IncrementAndGet();
    }

    public void Put(int seq, ReplyObject replyObject)
    {
        var cacheItem = new CacheItem(seq.ToString(), replyObject);
        MemoryCache.Default.Add(cacheItem, _policy);
    }

    public ReplyObject? Get(int seq)
    {
        return (ReplyObject?)MemoryCache.Default.Get(seq.ToString());
    }

    public void OnReply(RoutePacket routePacket)
    {
        try
        {
            int msgSeq = routePacket.Header.MsgSeq;
            var key = msgSeq.ToString();
            var replyObject = (ReplyObject?)MemoryCache.Default.Get(key);

            if (replyObject != null)
            {
                replyObject.OnReceive(routePacket);
                MemoryCache.Default.Remove(key);
            }
            else
            {
                _log.Error(() => $"request is not exist - [packetInfo:{routePacket.RouteHeader}]");
            }
        }
        catch (Exception ex)
        {
            _log.Error(() => $"{ex}");
        }
    }
}