using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System.IO;

public class GameBuilder
{
    [MenuItem("مزرعة رزان ويمان/📱 استخراج ملف اللعبة (Build APK)")]
    public static void BuildAndroidAPK()
    {
        // 1. Scene Setup
        string scenePath = "Assets/Scenes/MainScene.unity";
        
        // Ensure the current scene is saved if it's the right one, or warn the user
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MainScene")
        {
            bool save = EditorUtility.DisplayDialog("تنبيه", "يجب حفظ المشهد الحالي باسم MainScene قبل البناء. هل تريد حفظه الآن؟", "حفظ", "إلغاء");
            if (save)
            {
                if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), scenePath);
            }
            else return;
        }
        else
        {
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), scenePath);
        }

        // 2. Build Player Options
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { scenePath };
        buildPlayerOptions.locationPathName = "Builds/RazanYamanFarm.apk";
        buildPlayerOptions.target = BuildTarget.Android;
        buildPlayerOptions.options = BuildOptions.None;

        // 3. Perform BuildUnity
        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("Build succeeded: " + summary.totalSize + " bytes");
            EditorUtility.RevealInFinder(buildPlayerOptions.locationPathName);
            EditorUtility.DisplayDialog("نجاح!", "تم استخراج ملف اللعبة بنجاح.\nالمسار: " + buildPlayerOptions.locationPathName, "ممتاز");
        }
        else if (summary.result == BuildResult.Failed)
        {
            Debug.Log("Build failed");
            EditorUtility.DisplayDialog("خطأ", "فشلت عملية البناء. راجع الـ Console للتفاصيل.", "حسناً");
        }
    }
}
