using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    public GameObject playerPrefab;
    public Transform[] spawnPoints;
    public NetworkVariable<int> currentLevel = new NetworkVariable<int>(0);

    private int nextSpawnIndex = 0;

    private void Awake() { if (Instance == null) Instance = this; else Destroy(gameObject); }

    public override void OnNetworkSpawn()
    {
        if (IsServer) NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;
        Vector3 spawnPos = GetNextSpawnPoint();
        GameObject player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
    }

    private Vector3 GetNextSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return Vector3.zero;
        Vector3 pos = spawnPoints[nextSpawnIndex].position;
        nextSpawnIndex = (nextSpawnIndex + 1) % spawnPoints.Length;
        return pos;
    }
}
