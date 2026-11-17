using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

// 開始畫面：灰色布幕 + 標題 + 「遊戲開始」按鈕。UI 在執行時自動生成，不需要手動拉 Canvas。
// 按下開始後布幕淡出，並呼叫 CameraIntro 播放鏡頭（全景 → 車廠正面）。
[DisallowMultipleComponent]
public class TitleScreen : MonoBehaviour
{
    [Header("文字")]
    public string title = "移動演化";
    public string subtitle = "Super Assembly Car";
    public string startLabel = "遊戲開始";

    [Header("外觀")]
    public Color clothColor = new Color(0.45f, 0.45f, 0.45f, 1f);
    public Color buttonColor = new Color(0.22f, 0.22f, 0.22f, 1f);
    public float fadeSeconds = 0.8f;

    [Header("開始後")]
    public CameraIntro cameraIntro;

    CanvasGroup group;
    Font font;

    void Awake()
    {
        if (cameraIntro == null) cameraIntro = FindFirstObjectByType<CameraIntro>();
        EnsureEventSystem();
        Build();
    }

    static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        es.AddComponent<InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
    }

    static Font LoadFont()
    {
        string[] names = {
            "Microsoft JhengHei UI", "Microsoft JhengHei", "Microsoft YaHei UI", "Microsoft YaHei",
            "PingFang TC", "Noto Sans CJK TC", "Noto Sans TC", "Arial"
        };
        Font f = Font.CreateDynamicFontFromOSFont(names, 64);
        return f != null ? f : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    void Build()
    {
        font = LoadFont();

        var canvasGO = new GameObject("TitleCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        group = canvasGO.GetComponent<CanvasGroup>();

        // 灰色布幕
        var cloth = NewUI("Cloth", canvasGO.transform);
        Stretch(cloth);
        cloth.gameObject.AddComponent<Image>().color = clothColor;

        // 標題
        Place(NewText("Title", canvasGO.transform, title, 150, FontStyle.Bold, Color.white),
              new Vector2(0, 140), new Vector2(1600, 230));
        Place(NewText("Subtitle", canvasGO.transform, subtitle, 40, FontStyle.Normal, new Color(1f, 1f, 1f, 0.75f)),
              new Vector2(0, 30), new Vector2(1200, 70));

        // 遊戲開始按鈕
        var btnRect = NewUI("StartButton", canvasGO.transform);
        Place(btnRect, new Vector2(0, -190), new Vector2(440, 115));
        var img = btnRect.gameObject.AddComponent<Image>();
        img.color = Color.white;
        var btn = btnRect.gameObject.AddComponent<Button>();
        var cb = btn.colors;
        cb.normalColor = buttonColor;
        cb.highlightedColor = Color.Lerp(buttonColor, Color.white, 0.18f);
        cb.selectedColor = cb.highlightedColor;
        cb.pressedColor = Color.Lerp(buttonColor, Color.black, 0.35f);
        cb.fadeDuration = 0.08f;
        btn.colors = cb;
        Stretch(NewText("Label", btnRect, startLabel, 54, FontStyle.Bold, Color.white));
        btn.onClick.AddListener(OnStartPressed);
    }

    void OnStartPressed()
    {
        group.interactable = false;
        StartCoroutine(FadeOutAndPlay());
    }

    IEnumerator FadeOutAndPlay()
    {
        if (cameraIntro != null) cameraIntro.Play();
        float t = 0f;
        while (t < fadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = 1f - Mathf.Clamp01(t / fadeSeconds);
            yield return null;
        }
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.gameObject.SetActive(false);
    }

    // ---- UI helpers ----
    static RectTransform NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }

    static void Place(RectTransform r, Vector2 pos, Vector2 size)
    {
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.sizeDelta = size;
        r.anchoredPosition = pos;
    }

    RectTransform NewText(string name, Transform parent, string s, int size, FontStyle style, Color color)
    {
        var r = NewUI(name, parent);
        var t = r.gameObject.AddComponent<Text>();
        t.font = font;
        t.text = s;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return r;
    }
}
