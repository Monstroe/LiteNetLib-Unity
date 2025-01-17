using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class RoomService : MonoBehaviour, NetService
{
    public UnityEvent OnRoomCreate;
    public UnityEvent OnRoomJoin;
    public UnityEvent OnRoomLeft;
    public UnityEvent OnRoomStart;
    public UnityEvent OnRoomClose;
    public UnityEvent<NetMember> OnMemberJoin;
    public UnityEvent<NetMember> OnMemberLeft;
    public UnityEvent<string> OnInvalid;

    public enum RoomServiceSendType
    {
        Name = 0,
        CreateRoom = 1,
        JoinRoom = 2,
        LeaveRoom = 3,
        StartRoom = 4,
        CloseRoom = 5
    }

    public enum RoomServiceReceiveType
    {
        ID = 0,
        RoomCode = 1,
        RoomMembers = 2,
        MemberJoined = 3,
        MemberLeft = 4,
        RoomStart = 5,
        RoomClosed = 6,
        Invalid = 7
    }

    void Awake()
    {
        RegisterService((int)ServiceType.Room);
    }

    void OnDisable()
    {
        UnregisterService((int)ServiceType.Room);
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
        int commandID = packet.ReadByte();

        switch (commandID)
        {
            case (int)RoomServiceReceiveType.ID:
                {
                    NetManager.Instance.ID = Guid.Parse(packet.ReadString());
                    Debug.Log("<color=red><b>ManaNet</b></color>: ID received - " + NetManager.Instance.ID);
                    break;
                }
            case (int)RoomServiceReceiveType.RoomCode:
                {
                    if (NetManager.Instance.InRoom)
                    {
                        Debug.LogError("<color=red><b>ManaNet</b></color>: RoomService - Host is already in a room, cannot create another room without leaving the current one");
                        return;
                    }

                    NetManager.Instance.RoomCode = packet.ReadInt();
                    NetMember host = new NetMember(NetManager.Instance.ID, NetManager.Instance.Name);
                    NetManager.Instance.RoomMembers.Add(host);
                    Debug.Log("<color=red><b>ManaNet</b></color>: Room code received - " + NetManager.Instance.RoomCode);
                    OnRoomCreate.Invoke();
                    break;
                }
            case (int)RoomServiceReceiveType.RoomMembers:
                {
                    if (NetManager.Instance.IsHost)
                    {
                        Debug.LogError("<color=red><b>ManaNet</b></color>: RoomService - Only a guest can receive room members");
                        return;
                    }
                    NetManager.Instance.RoomCode = packet.ReadInt();
                    int count = packet.ReadInt();
                    List<NetMember> members = new List<NetMember>();
                    for (int i = 0; i < count; i++)
                    {
                        Guid id = Guid.Parse(packet.ReadString());
                        string name = packet.ReadString();
                        NetMember member = new NetMember(id, name);
                        members.Add(member);
                    }

                    NetManager.Instance.RoomMembers.AddRange(members);
                    Debug.Log("<color=red><b>ManaNet</b></color>: Room members total - " + NetManager.Instance.RoomMembers.Count);
                    OnRoomJoin.Invoke();
                    break;
                }
            case (int)RoomServiceReceiveType.MemberJoined:
                {
                    if (!NetManager.Instance.InRoom)
                    {
                        Debug.LogError("<color=red><b>ManaNet</b></color>: RoomService - Cannot receive member joined without being in a room");
                        return;
                    }

                    NetMember member = new NetMember(Guid.Parse(packet.ReadString()), packet.ReadString());
                    NetManager.Instance.RoomMembers.Add(member);

                    Debug.Log("<color=red><b>ManaNet</b></color>: Member joined - " + member.ID);
                    OnMemberJoin.Invoke(member);
                    break;
                }
            case (int)RoomServiceReceiveType.MemberLeft:
                {
                    if (!NetManager.Instance.InRoom)
                    {
                        Debug.LogError("<color=red><b>ManaNet</b></color>: RoomService - Cannot receive member left without being in a room");
                        return;
                    }

                    Guid memberID = Guid.Parse(packet.ReadString());
                    NetMember member = null;

                    foreach (NetMember m in NetManager.Instance.RoomMembers)
                    {
                        if (m.ID == memberID)
                        {
                            member = m;
                            NetManager.Instance.RoomMembers.Remove(member);
                            break;
                        }
                    }

                    if (memberID == NetManager.Instance.ID)
                    {
                        NetManager.Instance.Reset();
                        OnRoomLeft.Invoke();
                    }
                    else
                    {
                        OnMemberLeft.Invoke(member);
                    }

                    Debug.Log("<color=red><b>ManaNet</b></color>: Member left - " + memberID);
                    break;
                }
            case (int)RoomServiceReceiveType.RoomStart:
                {
                    if (!NetManager.Instance.InRoom)
                    {
                        Debug.LogError("<color=red><b>ManaNet</b></color>: RoomService - Cannot receive room start without being in a room");
                        return;
                    }

                    Debug.Log("<color=red><b>ManaNet</b></color>: Room started");
                    OnRoomStart.Invoke();
                    break;
                }
            case (int)RoomServiceReceiveType.RoomClosed:
                {
                    if (!NetManager.Instance.InRoom)
                    {
                        Debug.LogError("<color=red><b>ManaNet</b></color>: RoomService - Cannot receive room closed without being in a room");
                        return;
                    }

                    NetManager.Instance.Reset();
                    Debug.Log("<color=red><b>ManaNet</b></color>: Room closed");
                    OnRoomClose.Invoke();
                    break;
                }
            case (int)RoomServiceReceiveType.Invalid:
                {
                    string error = packet.ReadString();
                    Debug.LogError("<color=red><b>ManaNet</b></color>: RoomService - Invalid service type: " + error);
                    OnInvalid.Invoke(error);
                    break;
                }
        }
    }
}