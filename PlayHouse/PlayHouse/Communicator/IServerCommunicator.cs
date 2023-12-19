namespace PlayHouse.Communicator;
internal interface IServerCommunicator 
{ 
    void Bind(ICommunicateListener listener);
    void Communicate();
    void Stop();
}
