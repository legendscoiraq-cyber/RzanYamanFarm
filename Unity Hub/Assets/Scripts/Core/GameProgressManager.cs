using UnityEngine;
using System.Collections.Generic;

public class GameProgressManager : MonoBehaviour
{
    public static GameProgressManager Instance;
    public int totalScore = 0;
    
    private void Awake() { if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); } else Destroy(gameObject); }

    public void AddPoints(int points)
    {
        totalScore += points;
        GameUI.Instance?.UpdateScore(totalScore);
    }

    public void LearnItem(string itemName, BlockCategory category)
    {
        Debug.Log($"🎓 تعلمت: {itemName}");
    }
}
