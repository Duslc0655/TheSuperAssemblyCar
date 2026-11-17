using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 選單：SuperAssemblyCar > Add Title Screen To Scene
// 在目前場景加入 GameFlow 物件（TitleScreen + CameraIntro），並接上 Main Camera。
public static class TitleScreenSetup
{
    [MenuItem("SuperAssemblyCar/Add Title Screen To Scene")]
    static void Add()
    {
        var existing = Object.FindFirstObjectByType<TitleScreen>();
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            EditorUtility.DisplayDialog("Title Screen", "場景裡已經有開始畫面了（GameFlow）。", "OK");
            return;
        }

        var go = new GameObject("GameFlow");
        Undo.RegisterCreatedObjectUndo(go, "Add Title Screen");
        var intro = go.AddComponent<CameraIntro>();
        intro.cam = Camera.main;
        var title = go.AddComponent<TitleScreen>();
        title.cameraIntro = intro;

        EditorSceneManager.MarkSceneDirty(go.scene);
        Selection.activeGameObject = go;
        if (intro.cam == null)
            EditorUtility.DisplayDialog("Title Screen", "場景裡沒有標記為 MainCamera 的攝影機，請把攝影機拖到 GameFlow 的 Camera Intro > Cam 欄位。", "OK");
    }
}
