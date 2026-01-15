using UnityEngine;
using System.Collections.Generic;

public class LevelRepository : MonoBehaviour
{
    public static LevelRepository Instance;
    
    // We will store levels here so they can be accessed without creating assets
    [HideInInspector] public List<LevelData> allLevels = new List<LevelData>();

    private void Awake()
    {
        if (Instance == null) 
        { 
            Instance = this; 
            DontDestroyOnLoad(gameObject);
            GenerateLevels();
        }
        else 
        { 
            Destroy(gameObject); 
        }
    }

    private void GenerateLevels()
    {
        // Level 1: Find Apples
        var l1 = ScriptableObject.CreateInstance<LevelData>();
        l1.levelNumber = 1;
        l1.titleArabic = "حصاد التفاح";
        l1.descriptionArabic = "مرحباً يا أصدقاء! هل يمكنكم العثور على 3 تفاحات حمراء؟";
        l1.missionType = MissionType.FindItem;
        l1.targetBlockName = "Apple"; // We use name matching
        l1.targetCount = 3;
        l1.starsToWin = 1;
        allLevels.Add(l1);

        // Level 2: The Animals
        var l2 = ScriptableObject.CreateInstance<LevelData>();
        l2.levelNumber = 2;
        l2.titleArabic = "أصدقاء المزرعة";
        l2.descriptionArabic = "الحيوانات جائعة! ابحث عن البقرة وقدم لها الطعام.";
        l2.missionType = MissionType.FindItem;
        l2.targetBlockName = "Cow";
        l2.targetCount = 1;
        l2.starsToWin = 2;
        allLevels.Add(l2);

        // Level 3: Numbers
        var l3 = ScriptableObject.CreateInstance<LevelData>();
        l3.levelNumber = 3;
        l3.titleArabic = "تعلم الأرقام";
        l3.descriptionArabic = "أين الرقم (1)؟ ابحث عنه واضغط عليه.";
        l3.missionType = MissionType.FindItem;
        l3.targetBlockName = "Number1";
        l3.targetCount = 1;
        l3.starsToWin = 3;
        allLevels.Add(l3);
        
        // Level 4: Building
        var l4 = ScriptableObject.CreateInstance<LevelData>();
        l4.levelNumber = 4;
        l4.titleArabic = "وقت البناء";
        l4.descriptionArabic = "استخدم المكعبات لتبني سوراً صغيراً (ضع 5 بلوكات خشب).";
        l4.missionType = MissionType.FindItem; // Reusing Find for "Interact/Place" logic simplified
        l4.targetBlockName = "Wood";
        l4.targetCount = 5;
        l4.starsToWin = 3;
        allLevels.Add(l4);
    }

    public LevelData GetLevel(int levelIndex)
    {
        if (levelIndex >= 0 && levelIndex < allLevels.Count)
            return allLevels[levelIndex];
        return null;
    }
}
