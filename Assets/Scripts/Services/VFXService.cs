using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VFXService : MonoBehaviour, NetService
{
    void Awake()
    {
        RegisterService((int)ServiceType.VFX);
    }

    void OnDisable()
    {
        UnregisterService((int)ServiceType.VFX);
    }

    public void RegisterService(int serviceID)
    {
        NetManager.Instance.RegisterService(serviceID, this);
    }

    public void UnregisterService(int serviceID)
    {
        NetManager.Instance.UnregisterService(serviceID);
    }

    public void ReceiveData(NetPacket packet)
    {
        string name = packet.ReadString();
        Vector3 position = packet.ReadVector3();
        float scale = packet.ReadFloat();

        NetFXManager.Instance.PlayVFX(name, position, scale, false);
    }
}
