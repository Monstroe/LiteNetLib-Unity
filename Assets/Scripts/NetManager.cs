using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using LiteNetLib;
using AYellowpaper.SerializedCollections;

public class NetMember
{
    public Guid ID { get; }
    public string Name { get; }

    public NetMember(Guid id, string name)
    {
        ID = id;
        Name = name;
    }
}

public class NetManager : MonoBehaviour
{
    public static NetManager Instance { get; private set; }

    public Guid ID { get; set; }
    public string Name { get; set; }
    public int RoomCode { get; set; }
    public List<NetMember> RoomMembers { get; set; }
    public NetPeer RemotePeer { get; private set; }
    public string Address { get => address; set => address = value; }
    public int Port { get => port; set => port = value; }
    public string ConnectionKey { get => connectionKey; set => connectionKey = value; }
    public bool IsConnected { get; private set; }
    public bool IsHost { get => RoomMembers != null && RoomMembers.Count > 0 && RoomMembers[0].ID == ID; }
    public bool InRoom { get => RoomMembers != null && RoomMembers.Count > 0; }
    public Dictionary<int, NetService> NetServices { get; private set; }
    public Dictionary<int, NetObj> NetObjects { get; private set; }
    //public List<GameObject> NetPrefabs { get => netPrefabs; }
    public Dictionary<string, NetObj> NetPrefabs { get => netPrefabs; }


    [Header("Network Settings")]
    [SerializeField] private string address = "127.0.0.1";
    [SerializeField] private int port = 7777;
    [SerializeField] private string connectionKey = "Bruh-Wizz-Arcgis";

    [Header("Network Objects")]
    //[SerializeField] private List<GameObject> netPrefabs;
    [SerializedDictionary("Damage Type", "Description")]
    [SerializeField] private SerializedDictionary<string, NetObj> netPrefabs;
    [Space]
    [SerializeField] private string mainMenuScene = "MainMenu";

    [Header("Network Events")]
    [Space]
    public UnityEvent<NetPeer> OnConnectedEvent;
    public UnityEvent<NetPeer, DisconnectInfo> OnDisconnectedEvent;
    public UnityEvent<NetPeer, NetPacket, DeliveryMethod> OnPacketReceivedEvent;
    public UnityEvent<IPEndPoint, SocketError> OnNetworkErrorEvent;

    private LiteNetLib.NetManager manager;
    private EventBasedNetListener listener;
    private bool isInitialized = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogWarning("<color=red><b>ManaNet</b></color>: There are multiple NetManager instances in the scene, destroying one.");
            Destroy(gameObject);
        }

        Initialize();
    }

    private void Initialize()
    {
        NetObjects = new Dictionary<int, NetObj>();
        NetServices = new Dictionary<int, NetService>();
        RoomMembers = new List<NetMember>();
        Reset();

        Debug.Log("<color=red><b>ManaNet</b></color>: NetManager initialized");
    }

    public void Connect()
    {
        Debug.Log("<color=red><b>ManaNet</b></color>: Starting NetManager...");
        listener = new EventBasedNetListener();

        manager = new LiteNetLib.NetManager(listener);
        manager.Start();

        listener.PeerConnectedEvent += OnPeerConnected;
        listener.PeerDisconnectedEvent += OnPeerDisconnected;
        listener.NetworkReceiveEvent += OnNetworkReceive;
        listener.NetworkErrorEvent += OnNetworkError;

        manager.Connect(Address, Port, ConnectionKey);

        isInitialized = true;
        Debug.Log("<color=red><b>ManaNet</b></color>: NetManager started");
    }

    public void Disconnect(bool loadMenu = true)
    {
        manager.DisconnectPeer(RemotePeer);
        manager.Stop();
        if (loadMenu)
        {
            SceneManager.LoadSceneAsync(mainMenuScene);
        }
    }

    internal void Reset()
    {
        isInitialized = false;
        IsConnected = false;

        RemotePeer = null;
        manager = null;
        RoomMembers.Clear();
    }

    // Update is called once per frame
    void Update()
    {
        if (isInitialized)
        {
            manager.PollEvents();
        }
    }

    void OnDisable()
    {
        Debug.Log("<color=red><b>ManaNet</b></color>: NetManager closing...");
        if (IsConnected)
        {
            Disconnect();
        }
    }

    public void OnPeerConnected(NetPeer peer)
    {
        Debug.Log("<color=red><b>ManaNet</b></color>: NetManager connected");
        RemotePeer = peer;
        IsConnected = true;
        OnConnectedEvent.Invoke(peer);
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Debug.Log("<color=red><b>ManaNet</b></color>: NetManager disconnected - " + disconnectInfo.Reason.ToString());
        Reset();
        OnDisconnectedEvent.Invoke(peer, disconnectInfo);
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        byte[] data = new byte[reader.AvailableBytes];
        reader.GetBytes(data, reader.AvailableBytes);
        NetPacket packet = new NetPacket(data);
        int serviceID = packet.ReadByte();

        if (NetServices.ContainsKey(serviceID))
        {
            NetServices[serviceID].ReceiveData(packet);
        }
        else
        {
            Debug.LogError("<color=red><b>ManaNet</b></color>: Service with ID " + serviceID + " not found");
        }

        OnPacketReceivedEvent.Invoke(peer, packet, deliveryMethod);
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        Debug.LogError("<color=red><b>ManaNet</b></color>: Network error - " + socketError.ToString());
        OnNetworkErrorEvent.Invoke(endPoint, socketError);
    }

    public void Send(NetPacket packet, DeliveryMethod method)
    {
        try
        {
            manager.SendToAll(packet.ByteArray, method);
        }
        catch (SocketException e)
        {
            Debug.LogError("<color=red><b>ManaNet</b></color>: Socket Exception While Sending: " + e.SocketErrorCode.ToString());
        }
    }

    public void RegisterService(int id, NetService service)
    {
        if (NetServices.ContainsKey(id))
        {
            Debug.LogWarning("<color=red><b>ManaNet</b></color>: Service with ID " + id + " already exists");
            return;
        }

        NetServices.Add(id, service);
    }

    public void UnregisterService(int id)
    {
        if (!NetServices.ContainsKey(id))
        {
            Debug.LogWarning("<color=red><b>ManaNet</b></color>: Service with ID " + id + " does not exist");
            return;
        }

        NetServices.Remove(id);
    }

    public int GenerateNetID()
    {
        int id = UnityEngine.Random.Range(0, int.MaxValue);
        if (NetObjects.ContainsKey(id))
        {
            Debug.LogError("<color=red><b>ManaNet</b></color>: Congratulations, you just hit a 1 in 2.1 billion chance. Generating new ID...");
            return GenerateNetID();
        }

        return id;
    }
}
