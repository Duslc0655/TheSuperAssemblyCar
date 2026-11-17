using System.Collections.Generic;
using System.IO;
using UnityEditor;

// Unity 載入時確認這些場景都在 Build Settings 裡；缺的才補，已有的設定（順序、勾選）不動。
[InitializeOnLoad]
static class BuildScenesSetup
{
    static readonly string[] Required =
    {
        "Assets/Scenes/SampleScene.unity",
        "Assets/MoveEvolution/Scenes/CarBuilder.unity",
        "Assets/MoveEvolution/Scenes/NPCDrivingTest.unity",
    };

    static BuildScenesSetup()
    {
        EditorApplication.delayCall += Ensure;
    }

    static void Ensure()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        var have = new HashSet<string>();
        foreach (var s in scenes) have.Add(s.path);

        bool changed = false;
        foreach (var path in Required)
        {
            if (have.Contains(path) || !File.Exists(path)) continue;
            scenes.Add(new EditorBuildSettingsScene(path, true));
            changed = true;
            UnityEngine.Debug.Log("[SuperAssemblyCar] 已把場景加進 Build Settings：" + path);
        }
        if (changed) EditorBuildSettings.scenes = scenes.ToArray();
    }
}
