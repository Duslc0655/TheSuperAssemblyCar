using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 開場運鏡停在車廠正面後，在車廠門口顯示「進入車廠」按鈕；點下去淡出並切到車廠內部（組車場景）。
// TitleScreen 會自動加上這個元件，不用手動放。
public class FactoryEntrance : MonoBehaviour
{
    public CameraIntro cameraIntro;
    public string factorySceneName = "CarBuilder";
    public string label = "進入車廠";
    public float fadeSeconds = 0.6f;

    Canvas canvas;
    RectTransform btnRect;
    Button button;
    Image fade;
    Vector3 door;
    bool shown;

    void Start()
    {
        if (cameraIntro == null) cameraIntro = FindFirstObjectByType<CameraIntro>();
        if (cameraIntro != null) cameraIntro.onFinished.AddListener(Show);
    }

    public void Show()
    {
        if (shown || cameraIntro == null || cameraIntro.cam == null) return;
        if (!cameraIntro.TryGetFactoryDoor(out door))
        {
            Debug.LogWarning("[FactoryEntrance] 找不到車廠門口的位置。");
            return;
        }
        Build();
        shown = true;
    }

    void Build()
    {
        UiKit.EnsureEventSystem();
        canvas = UiKit.NewCanvas("EntranceCanvas", 90);
        canvas.transform.SetParent(transform, false);

        button = UiKit.Button(canvas.transform, "EnterFactory", label, 40, new Color(0.12f, 0.12f, 0.12f, 0.92f));
        btnRect = UiKit.Anchor((RectTransform)button.transform, Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(300, 90));
        button.onClick.AddListener(Enter);

        // 按鈕下方的小箭頭，指向門口
        var arrow = UiKit.Label(btnRect, "Arrow", "▼", 34, FontStyle.Normal, new Color(1f, 1f, 1f, 0.9f));
        UiKit.Anchor(arrow.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0, -2), new Vector2(60, 40));

        fade = UiKit.Panel(canvas.transform, "Fade", new Color(0f, 0f, 0f, 0f));
        UiKit.Stretch(fade.rectTransform);
        fade.raycastTarget = false;
    }

    void LateUpdate()
    {
        if (!shown || btnRect == null) return;
        Vector3 sp = cameraIntro.cam.WorldToScreenPoint(door);
        bool visible = sp.z > 0f;
        if (btnRect.gameObject.activeSelf != visible) btnRect.gameObject.SetActive(visible);
        if (!visible) return;
        float bob = Mathf.Sin(Time.unscaledTime * 3f) * 6f;
        btnRect.anchoredPosition = new Vector2(sp.x, sp.y) / canvas.scaleFactor + new Vector2(0f, 40f + bob);
    }

    void Enter()
    {
        button.interactable = false;
        StartCoroutine(FadeAndLoad());
    }

    IEnumerator FadeAndLoad()
    {
        fade.raycastTarget = true;
        float t = 0f;
        while (t < fadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            fade.color = new Color(0f, 0f, 0f, Mathf.Clamp01(t / fadeSeconds));
            yield return null;
        }
        SceneManager.LoadScene(factorySceneName);
    }
}
