using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class LevelController : NetworkBehaviour
{
    public static LevelController Instance;

    public NetworkVariable<int> currentLevelIndex = new NetworkVariable<int>(0);
    public NetworkVariable<int> currentProgress = new NetworkVariable<int>(0);
    
    private LevelData currentLevelData;
    private bool isLevelActive = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartLevel(0);
        }
        
        currentLevelIndex.OnValueChanged += (oldVal, newVal) => LoadLevelLocal(newVal);
        currentProgress.OnValueChanged += (oldVal, newVal) => UpdateUIProgress();
    }

    public void StartLevel(int index)
    {
        if (!IsServer) return;

        currentLevelIndex.Value = index;
        currentProgress.Value = 0;
        isLevelActive = true;
        LoadLevelLocal(index);
    }

    private void LoadLevelLocal(int index)
    {
        // Get data from Repository
        if (LevelRepository.Instance == null) return;
        
        currentLevelData = LevelRepository.Instance.GetLevel(index);
        if (currentLevelData != null)
        {
            Debug.Log($"Starting Level: {currentLevelData.titleArabic}");
            GameUI.Instance.ShowLevelInfo(currentLevelData);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SubmitActionServerRpc(string blockName)
    {
        if (!isLevelActive || currentLevelData == null) return;

        if (blockName == currentLevelData.targetBlockName)
        {
            currentProgress.Value++;
            
            // Play success sound
            AudioManager.Instance.PlayCollect();

            if (currentProgress.Value >= currentLevelData.targetCount)
            {
                CompleteLevel();
            }
        }
    }

    private void CompleteLevel()
    {
        isLevelActive = false;
        LevelCompleteClientRpc();
        
        // Wait and start next level
        StartCoroutine(WaitAndNextLevel());
    }

    [ClientRpc]
    private void LevelCompleteClientRpc()
    {
        AudioManager.Instance.PlayLevelComplete();
        GameUI.Instance.ShowLevelComplete(currentLevelData.starsToWin);
    }

    private IEnumerator WaitAndNextLevel()
    {
        yield return new WaitForSeconds(4.0f);
        int next = currentLevelIndex.Value + 1;
        if (LevelRepository.Instance.GetLevel(next) != null)
        {
            StartLevel(next);
        }
        else
        {
            // Game Over or Restart
            StartLevel(0);
        }
    }

    private void UpdateUIProgress()
    {
        if (currentLevelData != null)
        {
            GameUI.Instance.UpdateProgress(currentProgress.Value, currentLevelData.targetCount);
        }
    }
}
