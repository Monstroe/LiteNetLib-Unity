using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkTest : MonoBehaviour
{
    [SerializeField] bool connect = false;
    [SerializeField] bool sendName = false;
    [SerializeField] string playerName = "";
    [SerializeField] bool createRoom = false;
    [Space]
    [SerializeField] bool joinRoom = false;
    [SerializeField] int roomCode = 0;
    [Space]
    [SerializeField] bool leaveRoom = false;
    [SerializeField] bool closeRoom = false;
    [SerializeField] bool startGame = false;
    [SerializeField] bool testSFX = false;
    [Space]
    [SerializeField] bool spawnPlayer = false;
    [SerializeField] GameObject spawnPrefab;
    [SerializeField] bool destroyPlayer = false;
    [Space]
    [SerializeField] bool spawnOnHost = false;

    private GameObject playerObj;

    // Update is called once per frame
    void Update()
    {
        if (connect)
        {
            connect = false;
            NetManager.Instance.Connect();
        }

        if (sendName)
        {
            sendName = false;
            NetPacket packet = new NetPacket();
            packet.Write((byte)ServiceType.Room);
            packet.Write((byte)RoomService.RoomServiceSendType.Name);
            packet.Write(playerName);
            NetManager.Instance.Send(packet, LiteNetLib.DeliveryMethod.ReliableOrdered);
        }

        if (createRoom)
        {
            createRoom = false;
            NetPacket packet = new NetPacket();
            packet.Write((byte)ServiceType.Room);
            packet.Write((byte)RoomService.RoomServiceSendType.CreateRoom);
            NetManager.Instance.Send(packet, LiteNetLib.DeliveryMethod.ReliableOrdered);
        }

        if (joinRoom)
        {
            joinRoom = false;
            NetPacket packet = new NetPacket();
            packet.Write((byte)ServiceType.Room);
            packet.Write((byte)RoomService.RoomServiceSendType.JoinRoom);
            packet.Write(roomCode);
            NetManager.Instance.Send(packet, LiteNetLib.DeliveryMethod.ReliableOrdered);
        }

        if (leaveRoom)
        {
            leaveRoom = false;
            NetPacket packet = new NetPacket();
            packet.Write((byte)ServiceType.Room);
            packet.Write((byte)RoomService.RoomServiceSendType.LeaveRoom);
            NetManager.Instance.Send(packet, LiteNetLib.DeliveryMethod.ReliableOrdered);
        }

        if (closeRoom)
        {
            closeRoom = false;
            NetPacket packet = new NetPacket();
            packet.Write((byte)ServiceType.Room);
            packet.Write((byte)RoomService.RoomServiceSendType.CloseRoom);
            NetManager.Instance.Send(packet, LiteNetLib.DeliveryMethod.ReliableOrdered);
        }

        if (startGame)
        {
            startGame = false;
            NetPacket packet = new NetPacket();
            packet.Write((byte)ServiceType.Room);
            packet.Write((byte)RoomService.RoomServiceSendType.StartRoom);
            NetManager.Instance.Send(packet, LiteNetLib.DeliveryMethod.ReliableOrdered);
        }

        if (testSFX)
        {
            testSFX = false;
            NetFXManager.Instance.PlaySFX("horror", 1f, new Vector3(10, 10, 10));
        }

        if (spawnPlayer)
        {
            spawnPlayer = false;
            playerObj = Instantiate(spawnPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            playerObj.GetComponent<NetObj>().Initialize(NetManager.Instance.GenerateNetID(), NetManager.Instance.ID);
        }

        if (destroyPlayer)
        {
            destroyPlayer = false;
            Destroy(playerObj);
        }

        if (spawnOnHost)
        {
            spawnOnHost = false;
            NetObj.SpawnOnHost("player", new Vector3(0, 5, 0), Quaternion.identity);
        }
    }
}
