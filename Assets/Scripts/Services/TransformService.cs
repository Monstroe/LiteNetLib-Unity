using System;
using System.Collections;
using System.Collections.Generic;
using LiteNetLib;
using UnityEngine;

public class TransformService : MonoBehaviour, NetService
{
    void Awake()
    {
        RegisterService((int)ServiceType.Transform);
    }

    void OnDisable()
    {
        UnregisterService((int)ServiceType.Transform);
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
        Guid ownerID = Guid.Parse(packet.ReadString());
        string prefabName = packet.ReadString();
        Vector3 pos = packet.ReadVector3();
        Quaternion rot = packet.ReadQuaternion();

        DeliveryMethod method = DeliveryMethod.Unreliable;

        // Object is not yet spawned (or maybe is already destroyed)
        if (!NetManager.Instance.NetObjects.ContainsKey(netID))
        {
            // The prefabName is not empty AKA the object isn't being destroyed
            if (prefabName != "")
            {
                Debug.Log("<color=red><b>ManaNet</b></color>: TransformService found new object with ID " + netID);

                // If the object is not owned by the client, the host will assign an ID (hendce the -1 netID)
                if (netID == -1)
                {
                    if (!NetManager.Instance.IsHost)
                    {
                        Debug.LogError("<color=red><b>ManaNet</b></color>: TransformService found object with ID -1 but is not the host");
                        return;
                    }
                    netID = NetManager.Instance.GenerateNetID();
                    ownerID = NetManager.Instance.ID;
                }

                var prefab = NetManager.Instance.NetPrefabs[prefabName];
                NetObj newObj = Instantiate(prefab).GetComponent<NetObj>();
                newObj.Initialize(netID, ownerID);
                method = DeliveryMethod.ReliableOrdered;
            }
            else if (!NetManager.Instance.IsHost) // The object was already destroyed
            {
                NetManager.Instance.DeadNetIDs.Add(netID);
                Debug.LogWarning("<color=red><b>ManaNet</b></color>: TransformService found object already destroyed with ID " + netID);
                return;
            }

        }

        NetObj syncedObj = NetManager.Instance.NetObjects[netID].GetComponent<NetObj>();

        // Object is already owned by the client (this get's called when one of the guests is the owner of an object)
        if (!NetManager.Instance.IsHost && syncedObj.OwnerID == NetManager.Instance.ID)
        {
            Debug.LogWarning("<color=red><b>ManaNet</b></color>: TransformService found object already owned by client with ID " + netID);
            return;
        }

        // Object is switched to a new owner
        if (syncedObj.OwnerID != ownerID)
        {
            Debug.Log("<color=red><b>ManaNet</b></color>: TransformService swapped owner of object with ID " + netID);
            syncedObj.OwnerID = ownerID;
        }

        // Object should be Destroyed (with the empty prefabName)
        if (prefabName == "" || NetManager.Instance.DeadNetIDs.Contains(netID))
        {
            Debug.Log("<color=red><b>ManaNet</b></color>: TransformService found object to destroy with ID " + netID);
            NetManager.Instance.NetObjects.Remove(netID);
            NetManager.Instance.DeadNetIDs.Add(netID);
            Destroy(syncedObj.gameObject);
            syncedObj = null;
            method = DeliveryMethod.ReliableOrdered;
        }

        if (NetManager.Instance.IsHost)
        {
            NetObj.SendTransformPacket(netID, ownerID, prefabName, pos, rot, method);
        }

        if (syncedObj != null)
        {
            syncedObj.PosPivot.position = Vector3.Lerp(syncedObj.PosPivot.position, pos, syncedObj.InterpolationSpeed * Time.deltaTime);
            syncedObj.RotPivot.rotation = Quaternion.Lerp(syncedObj.RotPivot.rotation, rot, syncedObj.InterpolationSpeed * Time.deltaTime);
        }
    }
}
