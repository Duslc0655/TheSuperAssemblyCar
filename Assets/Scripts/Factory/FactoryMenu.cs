using UnityEngine;
using UnityEngine.UI;

// 車廠內部右側選單（暫定）：顧客訂單、組裝。
// 「組裝」會打開隊友的 GridBuilder（左側零件面板＋滑鼠放方塊）；其他時候 GridBuilder 關閉。
public class FactoryMenu : MonoBehaviour
{
    public float cameraMaxDistance = 14f;   // 讓攝影機不要拉出廠房外
    [Tooltip("左邊組車零件面板的右緣（螢幕像素），操作指南不會超過這裡")]
    public float leftPanelRightPx = 250f;
    public float guideMaxWidth = 900f;      // 操作指南最寬（畫布單位）

    enum Mode { None, Orders, Assembly }

    readonly Color itemColor = new Color(0.16f, 0.17f, 0.19f, 0.95f);
    readonly Color activeColor = new Color(0.80f, 0.20f, 0.18f, 1f);

    GridBuilder builder;
    Button ordersBtn, assemblyBtn;
    GameObject ordersPanel;
    Text hint;
    Image hintBg;
    Canvas canvas;
    RectTransform menuRect;
    Vector2 lastScreen;
    string lastText;
    Mode mode = Mode.None;

    void Awake()
    {
        builder = FindFirstObjectByType<GridBuilder>();
        if (builder != null) builder.enabled = false;

        var cam = Camera.main;
        var orbit = cam != null ? cam.GetComponent<OrbitCamera>() : null;
        if (orbit != null) orbit.maxDistance = Mathf.Min(orbit.maxDistance, cameraMaxDistance);

        UiKit.EnsureEventSystem();
        Build();
        SetMode(Mode.None);
    }

    void Build()
    {
        canvas = UiKit.NewCanvas("FactoryMenuCanvas", 50);
        canvas.transform.SetParent(transform, false);

        // 右側選單
        var panel = UiKit.Panel(canvas.transform, "Menu", new Color(0.07f, 0.07f, 0.08f, 0.82f));
        var pr = panel.rectTransform;
        pr.anchorMin = new Vector2(1f, 0f);
        pr.anchorMax = new Vector2(1f, 1f);
        pr.pivot = new Vector2(1f, 0.5f);
        pr.sizeDelta = new Vector2(340f, -60f);
        pr.anchoredPosition = new Vector2(-30f, 0f);
        menuRect = pr;

        var title = UiKit.Label(pr, "Title", "車廠", 56, FontStyle.Bold, Color.white);
        UiKit.Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -30), new Vector2(300, 80));
        var sub = UiKit.Label(pr, "Subtitle", "Super Assembly Car", 24, FontStyle.Normal, new Color(1f, 1f, 1f, 0.6f));
        UiKit.Anchor(sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -105), new Vector2(300, 36));

        ordersBtn = MenuButton(pr, "顧客訂單", -180f);
        ordersBtn.onClick.AddListener(() => SetMode(mode == Mode.Orders ? Mode.None : Mode.Orders));
        assemblyBtn = MenuButton(pr, "組裝", -290f);
        assemblyBtn.onClick.AddListener(() => SetMode(mode == Mode.Assembly ? Mode.None : Mode.Assembly));

        var note = UiKit.Label(pr, "Note", "（選單項目暫定）", 22, FontStyle.Normal, new Color(1f, 1f, 1f, 0.45f));
        UiKit.Anchor(note.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 30), new Vector2(300, 40));

        // 顧客訂單面板（暫定內容）
        var op = UiKit.Panel(canvas.transform, "OrdersPanel", new Color(0.95f, 0.94f, 0.90f, 0.97f));
        UiKit.Anchor(op.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-400f, 0f), new Vector2(620f, 640f));
        var oh = UiKit.Label(op.rectTransform, "Header", "顧客訂單", 44, FontStyle.Bold, new Color(0.15f, 0.15f, 0.15f));
        UiKit.Anchor(oh.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -28), new Vector2(560, 64));
        var ob = UiKit.Label(op.rectTransform, "Body",
            "訂單系統尚未完成。\n\n之後這裡會列出 NPC 的需求：\n・硬性條件（例：容納 ≥ 3）\n・五項性能的目標與權重\n・要走的路線\n\n" +
            "範例　林太太｜接兩個小孩放學\n　容納 ≥ 3，最在乎舒適與穩定",
            28, FontStyle.Normal, new Color(0.2f, 0.2f, 0.2f), TextAnchor.UpperLeft);
        UiKit.Anchor(ob.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -110), new Vector2(540, 500));
        ordersPanel = op.gameObject;

        // 操作指南：放在畫面上方（下方留給試駕的時速）
        hintBg = UiKit.Panel(canvas.transform, "HintBg", new Color(0f, 0f, 0f, 0.55f));
        UiKit.Anchor(hintBg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(guideMaxWidth, 90f));
        hint = UiKit.Label(hintBg.rectTransform, "Hint", "", 24, FontStyle.Normal, Color.white);
        hint.rectTransform.anchorMin = Vector2.zero;
        hint.rectTransform.anchorMax = Vector2.one;
        hint.rectTransform.offsetMin = new Vector2(18f, 10f);
        hint.rectTransform.offsetMax = new Vector2(-18f, -10f);
    }

    Button MenuButton(RectTransform parent, string label, float y)
    {
        var b = UiKit.Button(parent, "Btn_" + label, label, 36, itemColor);
        UiKit.Anchor((RectTransform)b.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(290f, 90f));
        return b;
    }

    void SetMode(Mode m)
    {
        mode = m;
        UiKit.SetButtonColor(ordersBtn, m == Mode.Orders ? activeColor : itemColor);
        UiKit.SetButtonColor(assemblyBtn, m == Mode.Assembly ? activeColor : itemColor);
        ordersPanel.SetActive(m == Mode.Orders);
        if (builder != null) builder.enabled = m == Mode.Assembly;

        switch (m)
        {
            case Mode.Assembly:
                hint.text = "組裝｜左鍵放零件　右鍵刪除　1–7 換零件　中鍵拖曳旋轉視角\n試駕｜T 開始／結束　WASD 駕駛　Shift 煞車　Space 手煞車";
                break;
            case Mode.Orders:
                hint.text = "";
                break;
            default:
                hint.text = "從右邊選單選擇要做的事";
                break;
        }
        hintBg.gameObject.SetActive(hint.text.Length > 0);
        lastText = null;   // 讓 LateUpdate 重新排版
    }

    // 把操作指南放在「左邊零件面板」和「右邊選單」中間的空隙，不蓋到兩邊
    void LateUpdate()
    {
        if (hintBg == null || !hintBg.gameObject.activeSelf) return;
        var screen = new Vector2(Screen.width, Screen.height);
        if (screen == lastScreen && hint.text == lastText) return;
        lastScreen = screen;
        lastText = hint.text;

        float s = canvas.scaleFactor;
        Vector3[] c = new Vector3[4];
        menuRect.GetWorldCorners(c);                 // 螢幕座標（Screen Space Overlay）
        float rightPx = c[0].x - 16f;                // 右邊選單左緣，留一點空隙
        float leftPx = leftPanelRightPx + 16f;       // 左邊零件面板右緣
        if (rightPx - leftPx < 200f) { leftPx = 16f; }   // 太窄就只避開右邊選單

        float widthUnits = Mathf.Min(guideMaxWidth, (rightPx - leftPx) / s);
        float centerPx = (leftPx + rightPx) * 0.5f;
        var r = hintBg.rectTransform;
        r.sizeDelta = new Vector2(widthUnits, r.sizeDelta.y);
        r.anchoredPosition = new Vector2((centerPx - Screen.width * 0.5f) / s, -20f);

        Canvas.ForceUpdateCanvases();
        float textH = LayoutUtility.GetPreferredHeight(hint.rectTransform);
        r.sizeDelta = new Vector2(widthUnits, textH + 20f);
    }
}
