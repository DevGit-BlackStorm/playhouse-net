namespace PlayHouse.Production.Api
{
    public interface IApiCallBack
    {
        void OnDisconnect(Guid accountId);
    }
}
