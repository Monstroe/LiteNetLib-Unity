public interface NetService
{
    public void RegisterService(int serviceID);
    public void UnregisterService(int serviceID);
    public void ReceiveData(NetPacket packet);
}