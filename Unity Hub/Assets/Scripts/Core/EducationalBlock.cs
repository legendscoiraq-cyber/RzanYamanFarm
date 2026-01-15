using UnityEngine;

[CreateAssetMenu(fileName = "NewBlock", menuName = "مزرعة رزان ويمان/بلوك تعليمي")]
public class EducationalBlock : ScriptableObject
{
    public string blockName;
    public string blockNameArabic;
    public AudioClip audioClip;
    public GameObject prefab;
    public Sprite icon;
    public BlockCategory category;
    public int pointsValue = 10;
}

public enum BlockCategory
{
    ArabicLetters,
    Numbers,
    Animals,
    Fruits,
    Vegetables
}

public class BlockInteract : MonoBehaviour
{
    public EducationalBlock blockData;
    
    public void OnInteract()
    {
        if (blockData != null && blockData.audioClip != null)
        {
            AudioManager.Instance.PlayClip(blockData.audioClip, transform.position);
            if (GameProgressManager.Instance != null) GameProgressManager.Instance.AddPoints(blockData.pointsValue);
        }
    }
}
