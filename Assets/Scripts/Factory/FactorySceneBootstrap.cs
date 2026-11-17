using UnityEngine;
using UnityEngine.SceneManagement;

// 組車場景（CarBuilder）一載入，就自動生成車廠內部和右側選單。
// 這樣不用改隊友的場景檔；直接在 CarBuilder 場景按 Play 也一樣會出現。
static class FactorySceneBootstrap
{
    public const string SceneName = "CarBuilder";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != SceneName) return;
        if (Object.FindFirstObjectByType<FactoryMenu>() != null) return;
        var root = new GameObject("Factory");
        SceneManager.MoveGameObjectToScene(root, scene);
        root.AddComponent<FactoryInterior>();
        root.AddComponent<FactoryMenu>();
    }
}
