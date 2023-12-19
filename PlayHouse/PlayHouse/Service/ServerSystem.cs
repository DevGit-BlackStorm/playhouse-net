using PlayHouse.Production;

namespace PlayHouse.Service
{
    public interface IServerSystem
    {
        
        Task OnStart();
        Task OnDispatch(IPacket packet);
        Task OnStop();
        Task OnPause();
        Task OnResume();
    }
}
