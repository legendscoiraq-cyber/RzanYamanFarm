using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class NetworkDiscovery : MonoBehaviour
{
    [SerializeField] private int broadcastPort = 47777;
    [SerializeField] private float broadcastInterval = 1.0f;
    [SerializeField] private string discoveryMessage = "RazanYamanFarm_Server";

    private UdpClient udpClient;
    private IPEndPoint activeServerEndpoint;
    private float nextBroadcastTime;
    private bool isBroadcasting = false;
    private bool isListening = false;

    private void Start()
    {
        if (NetworkManager.Singleton == null)
            Debug.LogError("NetworkManager is missing!");
    }

    private void OnDestroy() => StopDiscovery();

    public void StartHostBroadcast()
    {
        if (isBroadcasting) return;

        bool success = NetworkManager.Singleton.StartHost();
        if (!success)
        {
            Debug.LogError("Failed to start Host.");
            return;
        }

        try
        {
            udpClient = new UdpClient();
            udpClient.EnableBroadcast = true;
            isBroadcasting = true;
            Debug.Log("🌾 مزرعة رزان ويمان - بدأ البث...");
        }
        catch (Exception e)
        {
            Debug.LogError($"Broadcast init failed: {e.Message}");
        }
    }

    public void StartClientListener()
    {
        if (isListening) return;

        try
        {
            udpClient = new UdpClient(broadcastPort);
            udpClient.EnableBroadcast = true;
            udpClient.BeginReceive(new AsyncCallback(OnReceivedBroadcast), null);
            isListening = true;
            Debug.Log("🔍 جاري البحث عن المزرعة...");
        }
        catch (Exception e)
        {
            Debug.LogError($"Listener init failed: {e.Message}");
        }
    }

    public void StopDiscovery()
    {
        isBroadcasting = false;
        isListening = false;
        if (udpClient != null)
        {
            udpClient.Close();
            udpClient = null;
        }
    }

    private void Update()
    {
        if (isBroadcasting && Time.time >= nextBroadcastTime)
        {
            BroadcastServerIP();
            nextBroadcastTime = Time.time + broadcastInterval;
        }
    }

    private void BroadcastServerIP()
    {
        if (udpClient == null) return;
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(discoveryMessage);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, broadcastPort);
            udpClient.Send(data, data.Length, endPoint);
        }
        catch (Exception e) { Debug.LogError($"Broadcast error: {e.Message}"); }
    }

    private void OnReceivedBroadcast(IAsyncResult result)
    {
        if (udpClient == null) return;
        try
        {
            IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
            byte[] data = udpClient.EndReceive(result, ref sender);
            string message = Encoding.UTF8.GetString(data);

            if (message == discoveryMessage)
            {
                Debug.Log($"✅ تم العثور على المزرعة: {sender.Address}");
                activeServerEndpoint = sender;
                UnityMainThreadDispatcher.Instance().Enqueue(() => ConnectToServer(sender.Address.ToString()));
            }
            else
            {
                udpClient.BeginReceive(new AsyncCallback(OnReceivedBroadcast), null);
            }
        }
        catch (Exception e) { Debug.LogError($"Receive error: {e.Message}"); }
    }

    private void ConnectToServer(string ipAddress)
    {
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer) return;

        StopDiscovery();
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null) transport.ConnectionData.Address = ipAddress;

        Debug.Log($"🔗 جاري الاتصال بـ {ipAddress}...");
        NetworkManager.Singleton.StartClient();
    }
}

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher _instance;
    private readonly System.Collections.Concurrent.ConcurrentQueue<Action> _executionQueue = new System.Collections.Concurrent.ConcurrentQueue<Action>();

    public static UnityMainThreadDispatcher Instance()
    {
        if (_instance == null)
        {
            GameObject go = new GameObject("UnityMainThreadDispatcher");
            _instance = go.AddComponent<UnityMainThreadDispatcher>();
            DontDestroyOnLoad(go);
        }
        return _instance;
    }

    public void Enqueue(Action action) => _executionQueue.Enqueue(action);

    private void Update()
    {
        while (_executionQueue.TryDequeue(out Action action)) action.Invoke();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize() { if (_instance == null) Instance(); }
}
