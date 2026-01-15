using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class BlockSelectionUI : MonoBehaviour
{
    public BlockPlacer placer;
    public Transform content;
    public GameObject btnPrefab;

    void Start()
    {
        foreach (var block in AudioManager.Instance.allBlocks)
        {
            var go = Instantiate(btnPrefab, content);
            go.GetComponentInChildren<Text>().text = block.blockNameArabic;
            go.GetComponent<Button>().onClick.AddListener(() => placer.SelectBlock(block));
        }
    }
}
