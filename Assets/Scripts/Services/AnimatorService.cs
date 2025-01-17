using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorService : MonoBehaviour, NetService
{
    public enum AnimatorServiceType
    {
        Trigger = 0,
        Bool = 1
    }

    void Awake()
    {
        RegisterService((int)ServiceType.Animator);
    }

    void OnDisable()
    {
        UnregisterService((int)ServiceType.Animator);
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
        int netID = packet.ReadInt();
        byte type = packet.ReadByte();

        NetObj syncedObj = NetManager.Instance.NetObjects[netID];

        switch ((AnimatorServiceType)type)
        {
            case AnimatorServiceType.Trigger:
                string triggerName = packet.ReadString();
                syncedObj.GetComponentInChildren<Animator>().SetTrigger(triggerName);
                break;
            case AnimatorServiceType.Bool:
                string boolName = packet.ReadString();
                bool boolValue = packet.ReadBool();
                syncedObj.GetComponentInChildren<Animator>().SetBool(boolName, boolValue);
                break;
        }
    }
}