namespace PlayHouse.Communicator;
public interface IServerCommunicator 
{ 
    void Bind(ICommunicateListener listener);
    void Communicate();
    void Stop();
}
