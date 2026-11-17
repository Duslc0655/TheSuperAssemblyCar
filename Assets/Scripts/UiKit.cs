using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

// 執行時生成 uGUI 的小工具：中文字型、Canvas、面板、文字、按鈕。
public static class UiKit
{
    static Font font;

    public static Font Font
    {
        get
        {
            if (font == null)
            {
                string[] names = {
                    "Microsoft JhengHei UI", "Microsoft JhengHei", "Microsoft YaHei UI", "Microsoft YaHei",
                    "PingFang TC", "Noto Sans CJK TC", "Noto Sans TC", "Arial"
                };
                font = Font.CreateDynamicFontFromOSFont(names, 64);
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            return font;
        }
    }

    public static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        es.AddComponent<InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
    }

    public static Canvas NewCanvas(string name, int sortingOrder)
    {
        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    public static RectTransform NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    public static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }

    public static RectTransform Anchor(RectTransform r, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        r.anchorMin = r.anchorMax = anchor;
        r.pivot = pivot;
        r.sizeDelta = size;
        r.anchoredPosition = pos;
        return r;
    }

    public static Image Panel(Transform parent, string name, Color color)
    {
        var r = NewUI(name, parent);
        var img = r.gameObject.AddComponent<Image>();
        img.color = color;
        return img;
    }

    public static Text Label(Transform parent, string name, string text, int size, FontStyle style, Color color,
                             TextAnchor align = TextAnchor.MiddleCenter)
    {
        var r = NewUI(name, parent);
        var t = r.gameObject.AddComponent<Text>();
        t.font = Font;
        t.text = text;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = align;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        t.lineSpacing = 1.15f;
        return t;
    }

    public static Button Button(Transform parent, string name, string label, int fontSize, Color baseColor)
    {
        var r = NewUI(name, parent);
        var img = r.gameObject.AddComponent<Image>();
        img.color = Color.white;
        var b = r.gameObject.AddComponent<Button>();
        SetButtonColor(b, baseColor);
        var t = Label(r, "Label", label, fontSize, FontStyle.Bold, Color.white);
        Stretch(t.rectTransform);
        return b;
    }

    public static void SetButtonColor(Button b, Color baseColor)
    {
        var cb = b.colors;
        cb.normalColor = baseColor;
        cb.highlightedColor = Color.Lerp(baseColor, Color.white, 0.18f);
        cb.selectedColor = baseColor;
        cb.pressedColor = Color.Lerp(baseColor, Color.black, 0.35f);
        cb.disabledColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.4f);
        cb.fadeDuration = 0.08f;
        b.colors = cb;
    }
}
