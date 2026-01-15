using UnityEngine;
using UnityEditor;
using Unity.Netcode;
using System.Collections.Generic;
using Unity.Netcode.Transports.UTP;
using System.IO;

public class AutoSetup : EditorWindow
{
    [MenuItem("مزرعة رزان ويمان/🛠️ تجهيز اللعبة تلقائياً (Setup Game)")]
    public static void ShowWindow()
    {
        GetWindow<AutoSetup>("Game Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("إعداد المشهد والملفات", EditorStyles.boldLabel);

        if (GUILayout.Button("1. إنشاء الكائنات الأساسية (Create Managers)"))
        {
            SetupScene();
        }

        if (GUILayout.Button("2. توليد البلوكات والموارد (Generate Assets)"))
        {
            GenerateAssets();
        }

        if (GUILayout.Button("3. ربط كل شيء (Link Everything)"))
        {
            LinkEverything();
        }
    }

    private void SetupScene()
    {
        // 1. Network Manager
        var netObj = GameObject.Find("NetworkManager");
        if (!netObj)
        {
            netObj = new GameObject("NetworkManager");
            netObj.AddComponent<NetworkManager>();
            netObj.AddComponent<UnityTransport>();
            netObj.AddComponent<NetworkDiscovery>();
            netObj.AddComponent<GameManager>();
        }

        // 2. Managers
        var managers = GameObject.Find("Managers");
        if (!managers)
        {
            managers = new GameObject("Managers");
            managers.AddComponent<AudioManager>();
            managers.AddComponent<GameProgressManager>();
            managers.AddComponent<LevelRepository>();
            var lc = managers.AddComponent<LevelController>();
            lc.gameObject.AddComponent<NetworkObject>();
        }

        // 3. UI
        var canvas = GameObject.FindObjectOfType<Canvas>();
        if (!canvas)
        {
            var go = new GameObject("UI_Canvas");
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<UnityEngine.UI.CanvasScaler>();
            go.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            go.AddComponent<MainMenuUI>();
            go.AddComponent<GameUI>();
        }

        EditorUtility.DisplayDialog("نجاح", "تم إنشاء الكائنات الأساسية!", "ok");
    }

    private void GenerateAssets()
    {
        string prefabPath = "Assets/Prefabs/Generated";
        if (!Directory.Exists(prefabPath)) Directory.CreateDirectory(prefabPath);

        CreateBlockPrefab(prefabPath, "Apple", Color.red);
        CreateBlockPrefab(prefabPath, "Cow", Color.white); // Imagine it's a cow
        CreateBlockPrefab(prefabPath, "Sheep", Color.gray);
        CreateBlockPrefab(prefabPath, "Number1", Color.blue);
        CreateBlockPrefab(prefabPath, "Wood", new Color(0.6f, 0.4f, 0.2f));

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("نجاح", "تم توليد البريفاب (Prefabs)!", "ok");
    }

    private void CreateBlockPrefab(string path, string name, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Standard"));
        go.GetComponent<Renderer>().sharedMaterial.color = color;
        
        // Add components
        if (!go.GetComponent<NetworkObject>()) go.AddComponent<NetworkObject>();
        // BlockInteract is added at runtime or here? BlockPlacer adds it, but finding items needs interaction too.
        // For pickup items, we add BlockInteract
        var bi = go.AddComponent<BlockInteract>();
        
        string localPath = $"{path}/{name}.prefab";
        localPath = AssetDatabase.GenerateUniqueAssetPath(localPath);
        PrefabUtility.SaveAsPrefabAsset(go, localPath);
        DestroyImmediate(go);
    }

    private void LinkEverything()
    {
        // Link blocks to AudioManager
        AudioManager audioMgr = GameObject.FindObjectOfType<AudioManager>();
        if (audioMgr)
        {
            audioMgr.allBlocks = new List<EducationalBlock>();
            
            string[] names = { "Apple", "Cow", "Sheep", "Number1", "Wood" };
            foreach (var n in names)
            {
                EducationalBlock block = ScriptableObject.CreateInstance<EducationalBlock>();
                block.blockName = n;
                block.pointsValue = 10;
                
                // Load prefab
                string path = $"Assets/Prefabs/Generated/{n}.prefab";
                block.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                
                audioMgr.allBlocks.Add(block);
            }
        }

        EditorUtility.DisplayDialog("نجاح", "تم ربط الموارد!", "ok");
    }
}
