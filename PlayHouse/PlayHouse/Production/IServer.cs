namespace PlayHouse.Production
{
    public interface IServer
    {
        void Start();
        void Stop();
        void AwaitTermination();
    }

}
