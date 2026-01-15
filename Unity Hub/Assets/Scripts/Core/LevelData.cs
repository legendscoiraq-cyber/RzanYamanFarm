using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewLevel", menuName = "مزرعة رزان ويمان/مرحلة جديدة")]
public class LevelData : ScriptableObject
{
    public int levelNumber;
    public string titleArabic;
    public string descriptionArabic;
    
    [Header("المهمة")]
    public MissionType missionType;
    public EducationalBlock targetBlock; // Deprecated reference
    public string targetBlockName; // String identifier for easier matching

    [Header("المكافأة")]
    public int starsToWin = 3;
}

public enum MissionType
{
    FindItem,   // ابحث عن (مثلاً: أين التفاحة؟)
    CollectSet, // اجمع كل الحروف
    FreePlay    // لعب حر
}

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;
    public List<LevelData> levels;
    
    private void Awake() => Instance = this;

    public LevelData GetLevel(int index)
    {
        if (index >= 0 && index < levels.Count) return levels[index];
        return null;
    }
}
