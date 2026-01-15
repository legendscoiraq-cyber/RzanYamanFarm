using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

namespace RazanYamanFarm.Network
{
    /// <summary>
    /// UDP-based LAN Discovery system for local multiplayer.
    /// Host broadcasts presence, Client auto-discovers and connects.
    /// </summary>
    public class LanDiscovery : MonoBehaviour
    {
        [Header("Discovery Settings")]
        [SerializeField] private int broadcastPort = 47777;
        [SerializeField] private float broadcastIntervalSeconds = 1.0f;
        [SerializeField] private string serverSignature = "SERVER_HERE";
        
        [Header("Events")]
        public UnityEvent<string> OnServerDiscovered;
        public UnityEvent OnBroadcastStarted;
        public UnityEvent OnListeningStarted;
        public UnityEvent OnDiscoveryStopped;

        private UdpClient _udpClient;
        private IPEndPoint _broadcastEndpoint;
        private Thread _listenerThread;
        private CancellationTokenSource _cancellationTokenSource;
        
        private bool _isBroadcasting;
        private bool _isListening;
        private float _nextBroadcastTime;
        private string _discoveredServerIP;
        private readonly object _lockObject = new object();

        public bool IsBroadcasting => _isBroadcasting;
        public bool IsListening => _isListening;
        public string DiscoveredServerIP => _discoveredServerIP;

        #region Unity Lifecycle

        private void OnDestroy()
        {
            StopDiscovery();
        }

        private void OnApplicationQuit()
        {
            StopDiscovery();
        }

        private void Update()
        {
            // Host: broadcast server presence periodically
            if (_isBroadcasting && Time.time >= _nextBroadcastTime)
            {
                BroadcastServerPresence();
                _nextBroadcastTime = Time.time + broadcastIntervalSeconds;
            }

            // Check if server was discovered (thread-safe)
            lock (_lockObject)
            {
                if (!string.IsNullOrEmpty(_discoveredServerIP))
                {
                    string ip = _discoveredServerIP;
                    _discoveredServerIP = null;
                    OnServerDiscovered?.Invoke(ip);
                }
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Start broadcasting as host (server).
        /// Sends UDP broadcast packets so clients can discover this server.
        /// </summary>
        public void StartHostBroadcast()
        {
            if (_isBroadcasting)
            {
                Debug.LogWarning("[LanDiscovery] Already broadcasting.");
                return;
            }

            try
            {
                _udpClient = new UdpClient();
                _udpClient.EnableBroadcast = true;
                _broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, broadcastPort);
                
                _isBroadcasting = true;
                _nextBroadcastTime = 0f; // Broadcast immediately
                
                Debug.Log($"🌾 [LanDiscovery] Host broadcast started on port {broadcastPort}");
                OnBroadcastStarted?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LanDiscovery] Failed to start broadcast: {ex.Message}");
                CleanupUdpClient();
            }
        }

        /// <summary>
        /// Start listening as client.
        /// Listens for UDP broadcast packets from the host.
        /// </summary>
        public void StartClientListener()
        {
            if (_isListening)
            {
                Debug.LogWarning("[LanDiscovery] Already listening.");
                return;
            }

            try
            {
                _udpClient = new UdpClient(broadcastPort);
                _udpClient.EnableBroadcast = true;
                
                _cancellationTokenSource = new CancellationTokenSource();
                _listenerThread = new Thread(ListenerThreadWork)
                {
                    IsBackground = true,
                    Name = "LanDiscoveryListener"
                };
                _listenerThread.Start();
                
                _isListening = true;
                Debug.Log($"🔍 [LanDiscovery] Client listening on port {broadcastPort}");
                OnListeningStarted?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LanDiscovery] Failed to start listener: {ex.Message}");
                CleanupUdpClient();
            }
        }

        /// <summary>
        /// Stop all discovery activities.
        /// </summary>
        public void StopDiscovery()
        {
            _isBroadcasting = false;
            _isListening = false;

            // Cancel listener thread
            _cancellationTokenSource?.Cancel();
            
            CleanupUdpClient();

            // Wait for thread to finish
            if (_listenerThread != null && _listenerThread.IsAlive)
            {
                _listenerThread.Join(1000);
                _listenerThread = null;
            }

            Debug.Log("[LanDiscovery] Discovery stopped.");
            OnDiscoveryStopped?.Invoke();
        }

        /// <summary>
        /// Get the local IP address of this device.
        /// </summary>
        public static string GetLocalIPAddress()
        {
            try
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                    return endPoint?.Address.ToString() ?? "127.0.0.1";
                }
            }
            catch
            {
                // Fallback method
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
                return "127.0.0.1";
            }
        }

        #endregion

        #region Private Methods

        private void BroadcastServerPresence()
        {
            if (_udpClient == null) return;

            try
            {
                string localIP = GetLocalIPAddress();
                string message = $"{serverSignature}|{localIP}";
                byte[] data = Encoding.UTF8.GetBytes(message);
                
                _udpClient.Send(data, data.Length, _broadcastEndpoint);
                Debug.Log($"📡 [LanDiscovery] Broadcast sent: {localIP}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LanDiscovery] Broadcast error: {ex.Message}");
            }
        }

        private void ListenerThreadWork()
        {
            try
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    if (_udpClient?.Available > 0)
                    {
                        IPEndPoint senderEndpoint = new IPEndPoint(IPAddress.Any, 0);
                        byte[] data = _udpClient.Receive(ref senderEndpoint);
                        string message = Encoding.UTF8.GetString(data);

                        if (message.StartsWith(serverSignature))
                        {
                            // Extract IP from message: "SERVER_HERE|192.168.1.100"
                            string[] parts = message.Split('|');
                            string serverIP = parts.Length > 1 ? parts[1] : senderEndpoint.Address.ToString();
                            
                            Debug.Log($"✅ [LanDiscovery] Server found: {serverIP}");
                            
                            lock (_lockObject)
                            {
                                _discoveredServerIP = serverIP;
                            }
                            
                            // Stop listening after finding server
                            break;
                        }
                    }
                    
                    Thread.Sleep(100); // Avoid busy-waiting
                }
            }
            catch (SocketException)
            {
                // Socket was closed, normal during shutdown
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LanDiscovery] Listener error: {ex.Message}");
            }
        }

        private void CleanupUdpClient()
        {
            try
            {
                _udpClient?.Close();
            }
            catch { }
            finally
            {
                _udpClient = null;
            }
        }

        #endregion
    }
}
