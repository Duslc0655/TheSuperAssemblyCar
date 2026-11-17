using UnityEngine;
using UnityEngine.UI;

// 車廠內部：在組車場景（CarBuilder）周圍圍出廠房，包含牆、天花板、燈、捲門、貨架等擺設。
// 牆和天花板是單面的（只朝內），攝影機拉到外面時會自動看穿，不會擋住視線。
// 所有有碰撞的擺設都放在 Ignore Raycast 圖層，組車時滑鼠點不到它們，但試駕時會撞到。
public class FactoryInterior : MonoBehaviour
{
    [Header("廠房範圍 (m)")]
    public float halfWidth = 20f;   // x：-20 ~ 20
    public float zMin = -16f;
    public float zMax = 36f;        // 隊友的撞擊牆在 z = 25，包在裡面
    public float height = 9f;

    [Header("隊友場景裡的物件")]
    [Tooltip("進車廠時移除擋在組裝區和捲門中間的撞擊測試牆（只在執行時移除，不改場景檔）")]
    public bool removeCrashWall = true;

    [Header("招牌")]
    public string signText = "SUPER ASSEMBLY CAR";   // 捲門上方的遊戲標題

    [Header("顏色")]
    public Color floorColor = new Color(0.50f, 0.52f, 0.54f);
    public Color wallColor = new Color(0.80f, 0.79f, 0.74f);
    public Color wainscotColor = new Color(0.30f, 0.36f, 0.42f);
    public Color ceilingColor = new Color(0.62f, 0.63f, 0.65f);
    public Color accentColor = new Color(0.86f, 0.18f, 0.16f);   // 呼應車廠招牌的紅
    public Color safetyYellow = new Color(0.98f, 0.80f, 0.10f);

    const int IgnoreRaycast = 2;
    static Shader lit;

    void Start()
    {
        if (removeCrashWall)
        {
            var crashWall = GameObject.Find("CrashWall");
            if (crashWall != null) Destroy(crashWall);
        }
        Build();
    }

    void Build()
    {
        float w = halfWidth * 2f, d = zMax - zMin, zc = (zMin + zMax) * 0.5f;

        // 地板（不加碰撞，滑鼠會穿過去點到隊友原本的 BuildFloor）
        Quad("Floor", new Vector3(0, 0.005f, zc), new Vector2(w, d), new Vector3(90, 0, 0), Mat(floorColor, 0.35f));
        // 組裝區的黃色框線 12 x 12 m
        float a = 6f, lw = 0.15f;
        Material yellow = Mat(safetyYellow, 0.3f);
        Quad("Zone_N", new Vector3(0, 0.012f, a), new Vector2(2 * a, lw), new Vector3(90, 0, 0), yellow);
        Quad("Zone_S", new Vector3(0, 0.012f, -a), new Vector2(2 * a, lw), new Vector3(90, 0, 0), yellow);
        Quad("Zone_E", new Vector3(a, 0.012f, 0), new Vector2(lw, 2 * a), new Vector3(90, 0, 0), yellow);
        Quad("Zone_W", new Vector3(-a, 0.012f, 0), new Vector2(lw, 2 * a), new Vector3(90, 0, 0), yellow);
        // 試駕車道標線（組裝區到捲門）
        for (float z = a + 2f; z < zMax - 2f; z += 4f)
            Quad("LaneDash", new Vector3(0, 0.012f, z), new Vector2(0.2f, 2f), new Vector3(90, 0, 0), Mat(Color.white, 0.3f));

        // 牆（單面朝內）＋下緣深色牆裙
        Material wall = Mat(wallColor, 0.1f), band = Mat(wainscotColor, 0.2f);
        Wall("Wall_North", new Vector3(0, 0, zMax), w, new Vector3(0, 0, 0), wall, band);
        Wall("Wall_South", new Vector3(0, 0, zMin), w, new Vector3(0, 180, 0), wall, band);
        Wall("Wall_East", new Vector3(halfWidth, 0, zc), d, new Vector3(0, 90, 0), wall, band);
        Wall("Wall_West", new Vector3(-halfWidth, 0, zc), d, new Vector3(0, -90, 0), wall, band);

        // 試駕時擋住車子的隱形牆
        Material none = null;
        Solid("Bound_N", new Vector3(0, height / 2, zMax + 0.5f), new Vector3(w + 2, height, 1), none);
        Solid("Bound_S", new Vector3(0, height / 2, zMin - 0.5f), new Vector3(w + 2, height, 1), none);
        Solid("Bound_E", new Vector3(halfWidth + 0.5f, height / 2, zc), new Vector3(1, height, d + 2), none);
        Solid("Bound_W", new Vector3(-halfWidth - 0.5f, height / 2, zc), new Vector3(1, height, d + 2), none);

        // 天花板（單面朝下）＋鋼樑
        Quad("Ceiling", new Vector3(0, height, zc), new Vector2(w, d), new Vector3(-90, 0, 0), Mat(ceilingColor, 0.1f));
        Material steel = Mat(new Color(0.25f, 0.27f, 0.30f), 0.4f);
        for (float z = zMin + 4f; z < zMax; z += 8f)
            Box("Beam", new Vector3(0, height - 0.35f, z), new Vector3(w, 0.5f, 0.35f), steel, false);

        // 燈具＋點光源
        Material lampMat = Emissive(new Color(1f, 0.96f, 0.88f));
        foreach (float x in new[] { -10f, 10f })
            for (float z = zMin + 8f; z < zMax - 2f; z += 12f)
            {
                Box("LampFixture", new Vector3(x, height - 0.7f, z), new Vector3(2.4f, 0.12f, 0.6f), lampMat, false);
                var lg = new GameObject("Light");
                lg.transform.SetParent(transform, false);
                lg.transform.localPosition = new Vector3(x, height - 1.2f, z);
                var l = lg.AddComponent<Light>();
                l.type = LightType.Point;
                l.range = 20f;
                l.intensity = 3f;
                l.color = new Color(1f, 0.95f, 0.85f);
                l.shadows = LightShadows.None;
            }

        // 北牆捲門（試駕車道盡頭）
        float doorW = 12f, doorH = 6.5f;
        Material shutter = Mat(new Color(0.55f, 0.58f, 0.60f), 0.5f), shutterLine = Mat(new Color(0.40f, 0.43f, 0.45f), 0.5f);
        Quad("Shutter", new Vector3(0, doorH / 2, zMax - 0.05f), new Vector2(doorW, doorH), Vector3.zero, shutter);
        for (float y = 0.4f; y < doorH; y += 0.4f)
            Quad("ShutterLine", new Vector3(0, y, zMax - 0.06f), new Vector2(doorW, 0.05f), Vector3.zero, shutterLine);
        Material frame = Mat(accentColor, 0.3f);
        Box("DoorFrame_L", new Vector3(-doorW / 2 - 0.3f, doorH / 2, zMax - 0.2f), new Vector3(0.6f, doorH, 0.4f), frame, true);
        Box("DoorFrame_R", new Vector3(doorW / 2 + 0.3f, doorH / 2, zMax - 0.2f), new Vector3(0.6f, doorH, 0.4f), frame, true);
        Box("DoorFrame_T", new Vector3(0, doorH + 0.3f, zMax - 0.2f), new Vector3(doorW + 1.2f, 0.6f, 0.4f), frame, true);
        Sign(signText, new Vector3(0, doorH + 1.4f, zMax - 0.1f), 0f, 1800f);

        // 柱子
        Material pillar = Mat(new Color(0.85f, 0.85f, 0.82f), 0.2f), pillarBand = Mat(safetyYellow, 0.3f);
        foreach (float x in new[] { -halfWidth + 0.5f, halfWidth - 0.5f })
            for (float z = zMin + 4f; z < zMax; z += 8f)
            {
                Box("Pillar", new Vector3(x, height / 2, z), new Vector3(0.8f, height, 0.8f), pillar, true);
                Box("PillarBand", new Vector3(x, 0.6f, z), new Vector3(0.84f, 1.2f, 0.84f), pillarBand, false);
            }

        // 西牆：貨架＋紙箱
        Material rack = Mat(new Color(0.20f, 0.35f, 0.65f), 0.3f), boxMat = Mat(new Color(0.72f, 0.55f, 0.35f), 0.1f);
        for (int i = 0; i < 4; i++)
        {
            float z = zMin + 6f + i * 5f;
            float x = -halfWidth + 1.4f;
            foreach (float y in new[] { 0.9f, 2.1f, 3.3f })
                Box("Shelf", new Vector3(x, y, z), new Vector3(1.6f, 0.08f, 4f), rack, false);
            foreach (float dz in new[] { -1.9f, 1.9f })
                Box("ShelfPost", new Vector3(x, 1.8f, z + dz), new Vector3(1.6f, 3.6f, 0.1f), rack, true);
            for (int k = 0; k < 3; k++)
                Box("Crate", new Vector3(x, 1.25f + (k % 2) * 1.2f, z - 1.2f + k * 1.2f), new Vector3(1f, 0.6f, 0.9f), boxMat, false);
        }

        // 東牆：工具櫃、工作檯、輪胎堆
        Material cabinet = Mat(accentColor, 0.4f), bench = Mat(new Color(0.35f, 0.30f, 0.25f), 0.2f), top = Mat(new Color(0.6f, 0.6f, 0.62f), 0.5f);
        float ex = halfWidth - 1.2f;
        Box("ToolCabinet", new Vector3(ex, 1.0f, zMin + 5f), new Vector3(1.2f, 2f, 3f), cabinet, true);
        Box("ToolCabinet", new Vector3(ex, 1.0f, zMin + 8.5f), new Vector3(1.2f, 2f, 3f), cabinet, true);
        Box("Workbench", new Vector3(ex - 0.2f, 0.45f, zMin + 13f), new Vector3(1.6f, 0.9f, 4f), bench, true);
        Box("WorkbenchTop", new Vector3(ex - 0.2f, 0.93f, zMin + 13f), new Vector3(1.7f, 0.06f, 4.1f), top, false);
        Material tire = Mat(new Color(0.08f, 0.08f, 0.08f), 0.2f);
        for (int s = 0; s < 2; s++)
            for (int k = 0; k < 4; k++)
            {
                var t = Prim(PrimitiveType.Cylinder, "TireStack", new Vector3(ex - 0.3f, 0.15f + k * 0.3f, zMin + 18f + s * 1.6f),
                             new Vector3(1.2f, 0.15f, 1.2f), Vector3.zero, tire, true);
            }

        // 南牆（攝影機背後）：辦公室窗、海報
        Quad("OfficeWindow", new Vector3(-8f, 3.2f, zMin + 0.05f), new Vector2(8f, 2.2f), new Vector3(0, 180, 0), Mat(new Color(0.55f, 0.75f, 0.85f), 0.9f));
        Sign("組裝區", new Vector3(8f, 4.5f, zMin + 0.1f), 180f, 800f);
    }

    // ───── 建造用的小工具 ─────

    static Material Mat(Color c, float smooth)
    {
        if (lit == null) lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null) lit = Shader.Find("Standard");
        var m = new Material(lit);
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        return m;
    }

    static Material Emissive(Color c)
    {
        var m = Mat(c, 0.5f);
        m.EnableKeyword("_EMISSION");
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c * 2.5f);
        return m;
    }

    GameObject Prim(PrimitiveType type, string name, Vector3 pos, Vector3 scale, Vector3 euler, Material mat, bool collide)
    {
        var g = GameObject.CreatePrimitive(type);
        g.name = name;
        g.transform.SetParent(transform, false);
        g.transform.localPosition = pos;
        g.transform.localScale = scale;
        g.transform.localEulerAngles = euler;
        var col = g.GetComponent<Collider>();
        if (!collide) { if (col != null) DestroyImmediate(col); }
        else g.layer = IgnoreRaycast;
        var r = g.GetComponent<Renderer>();
        if (mat != null) r.sharedMaterial = mat;
        else r.enabled = false;
        return g;
    }

    void Quad(string name, Vector3 pos, Vector2 size, Vector3 euler, Material mat)
    {
        var g = Prim(PrimitiveType.Quad, name, pos, new Vector3(size.x, size.y, 1f), euler, mat, false);
        g.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    void Box(string name, Vector3 pos, Vector3 size, Material mat, bool collide)
    {
        Prim(PrimitiveType.Cube, name, pos, size, Vector3.zero, mat, collide);
    }

    void Solid(string name, Vector3 pos, Vector3 size, Material mat)
    {
        Prim(PrimitiveType.Cube, name, pos, size, Vector3.zero, mat, true);
    }

    void Wall(string name, Vector3 basePos, float length, Vector3 euler, Material wall, Material band)
    {
        Quaternion q = Quaternion.Euler(euler);
        Vector3 inward = q * Vector3.back * 0.01f;   // 牆裙稍微往內，避免跟牆面重疊閃爍
        Quad(name, basePos + Vector3.up * (height / 2f), new Vector2(length, height), euler, wall);
        Quad(name + "_Band", basePos + Vector3.up * 0.6f + inward, new Vector2(length, 1.2f), euler, band);
    }

    void Sign(string text, Vector3 pos, float yaw, float width)
    {
        var go = new GameObject("Sign_" + text, typeof(Canvas));
        go.transform.SetParent(transform, false);
        go.transform.localPosition = pos;
        go.transform.localEulerAngles = new Vector3(0, yaw, 0);
        go.transform.localScale = Vector3.one * 0.01f;
        var c = go.GetComponent<Canvas>();
        c.renderMode = RenderMode.WorldSpace;
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(width, 200);
        var bg = UiKit.Panel(go.transform, "Bg", new Color(0.12f, 0.12f, 0.14f, 0.95f));
        UiKit.Stretch(bg.rectTransform);
        var t = UiKit.Label(go.transform, "Text", text, 120, FontStyle.Bold, Color.white);
        UiKit.Stretch(t.rectTransform);
    }
}
