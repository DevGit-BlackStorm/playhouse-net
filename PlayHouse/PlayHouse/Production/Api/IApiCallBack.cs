namespace PlayHouse.Production.Api
{
    public interface IApiCallBack
    {
        void OnDisconnect(string accountId);
    }
}
