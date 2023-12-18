using NetMQ;
using PlayHouse.Production;
using System.Collections.Concurrent;

public class AsyncContext
{
    private class ErrorCodeWrapper
    {
        public ushort Code { get; set; } 
    }
    
    private static readonly AsyncLocal<IApiSender?> _apiSenderContext = new();
    private static readonly AsyncLocal<ErrorCodeWrapper?> _errorCode = new()  ;
    private static readonly ConcurrentDictionary<string, AsyncLocal<object?>> _storage = new();

    public static void InitErrorCode()
    {
        _errorCode.Value = new ErrorCodeWrapper();
    }

    public static IApiSender? ApiSender
    {
        get => _apiSenderContext.Value;
        set => _apiSenderContext.Value = value;
    }

    public static ushort ErrorCode
    {
        get
        {
            return _errorCode.Value!.Code;
        }
        set
        {
            _errorCode.Value!.Code = value;
        }
    }

    public static T? GetLocal<T>(string name)  where T : class
    {
        //string name = typeof(T).Name;
        var asyncLocal = _storage.GetOrAdd(name,new AsyncLocal<object?>());

        return asyncLocal.Value == null ? null : (T)asyncLocal.Value;
    }

    public static void SetLocal<T>(string name,T? value) where T : class
    {
        //string name = typeof(T).Name;
        var asyncLocal = _storage.GetOrAdd(name, new AsyncLocal<object?>());
        asyncLocal.Value = value;
    }

    internal static void Clear()
    {
        _apiSenderContext.Value = null;
        _errorCode.Value = null;

        foreach (var item in _storage)
        {
            item.Value.Value = null;
        }
    }
}