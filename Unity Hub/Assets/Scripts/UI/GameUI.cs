using UnityEngine;
using TMPro;

public class GameUI : MonoBehaviour
{
    public static GameUI Instance;
    public TextMeshProUGUI scoreText;
    
    [Header("Level UI")]
    public TextMeshProUGUI levelTitleText;
    public TextMeshProUGUI missionText;
    public TextMeshProUGUI progressText;
    public GameObject levelCompletePanel;
    public GameObject[] stars;

    private void Awake() => Instance = this;
    public void UpdateScore(int s) => scoreText.text = "النقاط: " + s;

    public void ShowLevelInfo(LevelData data)
    {
        if (levelCompletePanel) levelCompletePanel.SetActive(false);
        if (levelTitleText) levelTitleText.text = data.titleArabic;
        if (missionText) missionText.text = data.descriptionArabic;
        UpdateProgress(0, data.targetCount);
    }

    public void UpdateProgress(int current, int target)
    {
        if (progressText) progressText.text = $"{current} / {target}";
    }

    public void ShowLevelComplete(int starCount)
    {
        if (levelCompletePanel)
        {
            levelCompletePanel.SetActive(true);
            // Show stars
            for (int i = 0; i < stars.Length; i++)
            {
                if (stars[i]) stars[i].SetActive(i < starCount);
            }
        }
    }
}
