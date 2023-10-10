using PlayHouse.Production;

namespace PlayHouse;

public class AsyncContext
{
    private class ErrorCodeWrapper
    {
        public ushort Code { get; set; } 
    }
    
    private static readonly AsyncLocal<IApiSender?> _apiSenderContext = new();
    private static readonly AsyncLocal<ErrorCodeWrapper> _errorCode = new() ;

    internal static void InitErrorCode()
    {
        _errorCode.Value = new ErrorCodeWrapper();
    }
    
    public static IApiSender? ApiSender
    {
        get =>  _apiSenderContext.Value;
        set => _apiSenderContext.Value = value;   
    }
    
    public static ushort ErrorCode
    {
        get =>  _errorCode.Value!.Code;
        set => _errorCode.Value!.Code = value;   
    }
}