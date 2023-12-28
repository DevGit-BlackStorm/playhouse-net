namespace PlayHouse.Production.Api
{
    public interface IApiCallBack
    {
        Task OnDisconnect(IApiSender apiSender);
    }
}
