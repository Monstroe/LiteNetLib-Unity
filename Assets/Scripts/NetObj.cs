using System;
using System.Collections;
using System.Collections.Generic;
using LiteNetLib;
using Unity.VisualScripting;
using UnityEngine;

public class NetObj : MonoBehaviour
{
    // The ID of the object on the network
    public int NetID { get; set; }

    // The ID of the owner of the object (AKA who is responsible for syncing it)
    public Guid OwnerID { get; set; }

    public int SyncRate { get => syncRate; set => syncRate = value; }
    public float InterpolationSpeed { get => interpolationSpeed; set => interpolationSpeed = value; }
    public string ThisPrefabName { get => thisPrefabName; set => thisPrefabName = value; }
    public Transform PosPivot { get => posPivot; set => posPivot = value; }
    public Transform RotPivot { get => rotPivot; set => rotPivot = value; }

    [Header("Network Object Details")]
    [SerializeField] private int syncRate = 10;
    [SerializeField] private float interpolationSpeed = 30f;
    [SerializeField] private string thisPrefabName;
    [Space]
    [SerializeField] private Transform posPivot;
    [SerializeField] private Transform rotPivot;

    public static void SpawnOnHost(string prefabName, Vector3 pos, Quaternion rot)
    {
        // Sending -1 will indicate that the host should assign an ID and thus is the owner
        SendTransformPacket(-1, Guid.Empty, prefabName, pos, rot, DeliveryMethod.ReliableOrdered);
    }

    public static void DestroyOnHost(int netID)
    {
        // Send empty string for prefabID to indicate that the object should be destroyed (using -2 instead of -1 to avoid collisions with IndexOf method)
        SendTransformPacket(netID, Guid.Empty, "", Vector3.zero, Quaternion.identity, DeliveryMethod.ReliableOrdered);
    }

    public void Initialize(int netID, Guid ownerID)
    {
        if (NetManager.Instance == null)
        {
            return;
        }

        if (PosPivot == null)
        {
            PosPivot = transform;
        }

        if (RotPivot == null)
        {
            RotPivot = transform;
        }

        NetID = netID;
        OwnerID = ownerID;
        NetManager.Instance.NetObjects.Add(NetID, this);
        Debug.Log("<color=green><b>ManaNet</b></color>: Object with ID " + NetID + " initialized");
        StartCoroutine(SyncTransform());
    }

    private IEnumerator SyncTransform()
    {
        while (true)
        {
            if (OwnerID == NetManager.Instance.ID)
            {
                SendTransformPacket(NetID, OwnerID, ThisPrefabName, PosPivot.position, RotPivot.rotation, DeliveryMethod.Unreliable);
            }

            yield return new WaitForSeconds(1f / syncRate);
        }
    }

    private void OnDestroy()
    {
        if (NetManager.Instance == null)
        {
            return;
        }

        if (OwnerID == NetManager.Instance.ID)
        {
            // Send empty string for prefabID to indicate that the object should be destroyed (using -2 instead of -1 to avoid collisions with IndexOf method)
            SendTransformPacket(NetID, OwnerID, "", PosPivot.position, RotPivot.rotation, DeliveryMethod.ReliableOrdered);
            NetManager.Instance.NetObjects.Remove(NetID);
            NetManager.Instance.DeadNetIDs.Add(NetID);
            Debug.Log("<color=red><b>ManaNet</b></color>: Object with ID " + NetID + " destroyed");
        }
    }

    public void SetTrigger(string name, DeliveryMethod method)
    {
        if (NetManager.Instance == null)
        {
            return;
        }

        if (OwnerID == NetManager.Instance.ID)
        {
            NetPacket packet = new NetPacket();
            packet.Write((byte)ServiceType.Animator);
            packet.Write(NetID);
            packet.Write((byte)AnimatorService.AnimatorServiceType.Trigger);
            packet.Write(name);
            NetManager.Instance.Send(packet, method);
        }
    }

    public void SetBool(string name, bool value, DeliveryMethod method)
    {
        if (NetManager.Instance == null)
        {
            return;
        }

        if (OwnerID == NetManager.Instance.ID)
        {
            NetPacket packet = new NetPacket();
            packet.Write((byte)ServiceType.Animator);
            packet.Write(NetID);
            packet.Write((byte)AnimatorService.AnimatorServiceType.Bool);
            packet.Write(name);
            packet.Write(value);
            NetManager.Instance.Send(packet, method);
        }
    }

    public static void SendTransformPacket(int netID, Guid ownerID, string prefabName, Vector3 pos, Quaternion rot, DeliveryMethod method)
    {
        NetPacket packet = new NetPacket();
        packet.Write((byte)ServiceType.Transform);
        packet.Write(netID);
        packet.Write(ownerID.ToString());
        packet.Write(prefabName);
        packet.Write(pos);
        packet.Write(rot);
        NetManager.Instance.Send(packet, method);
    }
}