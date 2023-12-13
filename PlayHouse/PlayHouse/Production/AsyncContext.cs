using NetMQ;
using PlayHouse.Production;
using System.Collections.Concurrent;

public class AsyncContext
{
    private class ErrorCodeWrapper
    {
        public ushort Code { get; set; } 
    }
    
    //private static readonly AsyncLocal<IApiSender?> _apiSenderContext = new();
    //private static readonly AsyncLocal<ErrorCodeWrapper> _errorCode = new() ;
    private static readonly ConcurrentDictionary<string, AsyncLocal<object?>> _storage = new(); 

    //internal static void InitErrorCode()
    //{
    //    _errorCode.Value = new ErrorCodeWrapper();
    //}
    
    public static IApiSender? ApiSender
    {
        get => GetAsyncLocal<IApiSender>();
        set => SetAsyncLocal<IApiSender>(value);   
    }
    
    public static ushort ErrorCode
    {
        get
        {
            var errorCodeWrapper = GetAsyncLocal<ErrorCodeWrapper>();
            if (errorCodeWrapper != null)
            {
                return 0;
            }
            else
            {
                return errorCodeWrapper!.Code;
            }

        }
        set 
        {
            SetAsyncLocal<ErrorCodeWrapper>(new ErrorCodeWrapper() { Code = value });
        }
    }

    public static T? GetAsyncLocal<T>()  where T : class
    {
        string name = typeof(T).Name;
        var asyncLocal = _storage.GetOrAdd(name,new AsyncLocal<object?>());

        return asyncLocal.Value == null ? null : (T)asyncLocal.Value;
    }

    public static void SetAsyncLocal<T>(T? value) where T : class
    {
        string name = typeof(T).Name;
        var asyncLocal = _storage.GetOrAdd(name, new AsyncLocal<object?>());
        asyncLocal.Value = value;
    }

    internal static void Clear()
    {
        foreach (var item in _storage)
        {
            item.Value.Value = null;
        }
    }
}