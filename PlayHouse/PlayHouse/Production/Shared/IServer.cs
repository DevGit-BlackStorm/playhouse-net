namespace PlayHouse.Production.Shared
{
    public interface IServer
    {
        void Start();
        Task StopAsync();
        void AwaitTermination();
    }

}
