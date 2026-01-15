using System;
using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace RazanYamanFarm.Network
{
    /// <summary>
    /// Central network manager that handles Host/Client startup and integrates with LAN Discovery.
    /// Provides a simple API for starting multiplayer sessions.
    /// </summary>
    public class GameNetManager : MonoBehaviour
    {
        public static GameNetManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private LanDiscovery lanDiscovery;
        
        [Header("Network Settings")]
        [SerializeField] private ushort defaultPort = 7777;
        [SerializeField] private float connectionTimeout = 10f;
        
        [Header("Events")]
        public UnityEvent OnHostStarted;
        public UnityEvent OnClientStarted;
        public UnityEvent OnClientConnected;
        public UnityEvent OnClientDisconnected;
        public UnityEvent<string> OnConnectionFailed;

        private UnityTransport _transport;
        private bool _isConnecting;
        private float _connectionTimer;

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Cache transport reference
            if (NetworkManager.Singleton != null)
            {
                _transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            }
            else
            {
                Debug.LogError("[GameNetManager] NetworkManager.Singleton is null! Make sure NetworkManager exists in the scene.");
            }

            // Auto-find LanDiscovery if not assigned
            if (lanDiscovery == null)
            {
                lanDiscovery = FindObjectOfType<LanDiscovery>();
            }

            // Subscribe to LAN discovery events
            if (lanDiscovery != null)
            {
                lanDiscovery.OnServerDiscovered.AddListener(OnServerDiscovered);
            }

            // Subscribe to NetworkManager events
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
            }
        }

        private void Update()
        {
            // Handle connection timeout
            if (_isConnecting)
            {
                _connectionTimer -= Time.deltaTime;
                if (_connectionTimer <= 0)
                {
                    _isConnecting = false;
                    OnConnectionFailed?.Invoke("Connection timed out");
                    Disconnect();
                }
            }
        }

        private void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Start as Host (Server + Client).
        /// Also starts LAN broadcasting for auto-discovery.
        /// </summary>
        public bool StartHost()
        {
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("[GameNetManager] Cannot start host - NetworkManager is null");
                return false;
            }

            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                Debug.LogWarning("[GameNetManager] Already hosting");
                return true;
            }

            try
            {
                // Configure transport
                if (_transport != null)
                {
                    _transport.ConnectionData.Port = defaultPort;
                    _transport.ConnectionData.Address = "0.0.0.0"; // Listen on all interfaces
                }

                bool success = NetworkManager.Singleton.StartHost();
                
                if (success)
                {
                    Debug.Log($"🏠 [GameNetManager] Host started on port {defaultPort}");
                    
                    // Start LAN broadcasting
                    lanDiscovery?.StartHostBroadcast();
                    
                    OnHostStarted?.Invoke();
                }
                else
                {
                    Debug.LogError("[GameNetManager] Failed to start host");
                    OnConnectionFailed?.Invoke("Failed to start host");
                }

                return success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameNetManager] Host start exception: {ex.Message}");
                OnConnectionFailed?.Invoke(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Start as Client and begin searching for host via LAN discovery.
        /// </summary>
        public void StartClientWithDiscovery()
        {
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("[GameNetManager] Cannot start client - NetworkManager is null");
                return;
            }

            if (NetworkManager.Singleton.IsClient)
            {
                Debug.LogWarning("[GameNetManager] Already connected as client");
                return;
            }

            Debug.Log("🔍 [GameNetManager] Starting client with LAN discovery...");
            
            _isConnecting = true;
            _connectionTimer = connectionTimeout;
            
            lanDiscovery?.StartClientListener();
        }

        /// <summary>
        /// Connect directly to a known IP address.
        /// </summary>
        public bool ConnectToHost(string ipAddress, ushort port = 0)
        {
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("[GameNetManager] Cannot connect - NetworkManager is null");
                return false;
            }

            if (port == 0) port = defaultPort;

            try
            {
                // Stop discovery
                lanDiscovery?.StopDiscovery();

                // Configure transport
                if (_transport != null)
                {
                    _transport.ConnectionData.Address = ipAddress;
                    _transport.ConnectionData.Port = port;
                }

                Debug.Log($"🔗 [GameNetManager] Connecting to {ipAddress}:{port}...");
                
                _isConnecting = true;
                _connectionTimer = connectionTimeout;
                
                bool success = NetworkManager.Singleton.StartClient();
                
                if (success)
                {
                    OnClientStarted?.Invoke();
                }
                else
                {
                    _isConnecting = false;
                    OnConnectionFailed?.Invoke("Failed to start client");
                }

                return success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameNetManager] Connect exception: {ex.Message}");
                _isConnecting = false;
                OnConnectionFailed?.Invoke(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Disconnect from the current session.
        /// </summary>
        public void Disconnect()
        {
            lanDiscovery?.StopDiscovery();
            _isConnecting = false;

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
                Debug.Log("[GameNetManager] Disconnected");
            }
        }

        /// <summary>
        /// Check if we're currently in a networked session.
        /// </summary>
        public bool IsConnected => NetworkManager.Singleton != null && 
                                   (NetworkManager.Singleton.IsHost || 
                                    NetworkManager.Singleton.IsClient || 
                                    NetworkManager.Singleton.IsServer);

        /// <summary>
        /// Check if we're the host.
        /// </summary>
        public bool IsHost => NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;

        /// <summary>
        /// Get the number of connected players.
        /// </summary>
        public int ConnectedPlayersCount => NetworkManager.Singleton?.ConnectedClientsIds?.Count ?? 0;

        #endregion

        #region Event Handlers

        private void OnServerDiscovered(string serverIP)
        {
            Debug.Log($"[GameNetManager] Server discovered at {serverIP}");
            ConnectToHost(serverIP);
        }

        private void HandleClientConnected(ulong clientId)
        {
            _isConnecting = false;
            
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                Debug.Log($"✅ [GameNetManager] Connected! Client ID: {clientId}");
                OnClientConnected?.Invoke();
            }
            else
            {
                Debug.Log($"👤 [GameNetManager] Another player joined: {clientId}");
            }
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            if (clientId == NetworkManager.Singleton?.LocalClientId)
            {
                Debug.Log("[GameNetManager] We disconnected from the server");
                OnClientDisconnected?.Invoke();
            }
            else
            {
                Debug.Log($"👋 [GameNetManager] Player left: {clientId}");
            }
        }

        #endregion
    }
}
