using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class GridBuilder : MonoBehaviour
{
    public float cellSize = 1f;
    public Transform vehicleRoot;
    public string carName = "BasicCar";

    readonly Dictionary<Vector3Int, GameObject> blocks = new Dictionary<Vector3Int, GameObject>();
    readonly Dictionary<GameObject, Vector3Int> cells = new Dictionary<GameObject, Vector3Int>();
    readonly List<KeyValuePair<Vector3Int, PartDef>> savedLayout = new List<KeyValuePair<Vector3Int, PartDef>>();
    GameObject ghost;
    Camera cam;
    OrbitCamera orbit;
    VehicleDriver driver;
    PlayerVehicleInput playerInput;
    bool testDriving;
    string hint = "";
    int selected = 0;
    Rect panelRect;
    float yStats, yTest, yName, ySave, yHint;

    const float BtnH = 34f, Gap = 4f, PanelW = 230f, StatsH = 196f;

    void Start()
    {
        cam = Camera.main;
        if (cam != null) orbit = cam.GetComponent<OrbitCamera>();
        if (vehicleRoot == null)
            vehicleRoot = new GameObject("Vehicle").transform;

        yStats = 20 + PartCatalog.All.Length * (BtnH + Gap) + 4;
        yTest = yStats + StatsH;
        yName = yTest + BtnH + 6;
        ySave = yName + 32;
        yHint = ySave + BtnH + 4;
        panelRect = new Rect(10, 10, PanelW, yHint + 60);
        RebuildGhost();
    }

    // 被關掉時（例如車廠選單切到別的功能）把預覽方塊藏起來
    void OnDisable()
    {
        if (ghost != null) ghost.SetActive(false);
    }

    void Update()
    {
        var mouse = Mouse.current;
        var kb = Keyboard.current;
        if (mouse == null || cam == null) return;

        Vector2 mp = mouse.position.ReadValue();
        bool overPanel = !testDriving && panelRect.Contains(new Vector2(mp.x, Screen.height - mp.y));
        // 滑鼠在其他 uGUI 介面上（例如車廠右側選單）時，也不放零件
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) overPanel = true;

        // 正在輸入車名時，按鍵不當成快捷鍵；點一下面板外面就結束輸入
        bool typing = GUIUtility.keyboardControl != 0;
        if (typing && !overPanel && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame))
        {
            GUI.FocusControl(null);
            GUIUtility.keyboardControl = 0;
            ghost.SetActive(false);
            return;
        }

        if (!typing && kb != null && kb.tKey.wasPressedThisFrame) ToggleTestDrive();
        if (testDriving) { ghost.SetActive(false); return; }

        // 數字鍵 1~6 切換零件
        if (!typing && kb != null)
            for (int i = 0; i < PartCatalog.All.Length; i++)
                if (kb[Key.Digit1 + i].wasPressedThisFrame) Select(i);

        bool altHeld = kb != null && kb.leftAltKey.isPressed;
        if (overPanel || altHeld || typing) { ghost.SetActive(false); return; }

        Ray ray = cam.ScreenPointToRay(mp);
        RaycastHit hit;
        if (!Physics.Raycast(ray, out hit, 200f))
        {
            ghost.SetActive(false);
            return;
        }

        // 右鍵：刪除
        Vector3Int target;
        if (mouse.rightButton.wasPressedThisFrame && cells.TryGetValue(hit.collider.gameObject, out target))
            RemoveBlock(target);

        // 把點到的位置往法線方向推半格，就是要放置的格子
        Vector3 p = hit.point + hit.normal * (cellSize * 0.5f);
        Vector3Int cell = new Vector3Int(
            Mathf.FloorToInt(p.x / cellSize),
            Mathf.FloorToInt(p.y / cellSize),
            Mathf.FloorToInt(p.z / cellSize));

        bool free = cell.y >= 0 && !blocks.ContainsKey(cell);
        ghost.SetActive(free);
        if (!free) return;
        ghost.transform.position = CellToWorld(cell);

        // 左鍵：放置
        if (mouse.leftButton.wasPressedThisFrame)
        {
            PartDef def = PartCatalog.All[selected];
            if (def.maxPerCar > 0 && CountOf(def.type) >= def.maxPerCar)
                hint = def.displayName + " 一台車最多 " + def.maxPerCar + " 個";
            else
                PlaceBlock(cell, def);
        }
    }

    // ───────────────────────── 試駕 ─────────────────────────

    void ToggleTestDrive()
    {
        if (!testDriving)
        {
            if (blocks.Count == 0) { hint = "先放一些方塊"; return; }
            VehicleStats st = CurrentStats();
            if (st.Overloaded) { hint = "超過負荷值（" + st.load.ToString("0.#") + " / " + st.loadCapacity.ToString("0.#") + "），加主體或拿掉零件"; return; }

            savedLayout.Clear();
            foreach (var kv in blocks)
                savedLayout.Add(new KeyValuePair<Vector3Int, PartDef>(kv.Key, kv.Value.GetComponent<BlockInfo>().def));

            driver = vehicleRoot.gameObject.AddComponent<VehicleDriver>();
            if (!driver.Build(blocks))
            {
                hint = driver.Error;
                Destroy(driver);
                driver = null;
                return;
            }
            hint = "";
            playerInput = vehicleRoot.gameObject.AddComponent<PlayerVehicleInput>();
            if (orbit != null) orbit.followTarget = driver.CenterTransform;
            ghost.SetActive(false);
            testDriving = true;
        }
        else
        {
            if (driver != null) Destroy(driver);
            driver = null;
            if (playerInput != null) Destroy(playerInput);
            playerInput = null;
            if (orbit != null) orbit.followTarget = null;

            // 把車砍掉，照存好的配置重新擺回格子上
            ClearAll();
            foreach (var item in savedLayout) PlaceBlock(item.Key, item.Value);
            testDriving = false;
        }
    }

    // ───────────────────────── 存檔 / 讀檔 ─────────────────────────

    void SaveCar()
    {
        if (blocks.Count == 0) { hint = "沒有方塊可以存"; return; }

        var bp = new CarBlueprint { name = carName };
        foreach (var kv in blocks)
        {
            PartDef d = kv.Value.GetComponent<BlockInfo>().def;
            bp.blocks.Add(new BlueprintBlock { x = kv.Key.x, y = kv.Key.y, z = kv.Key.z, part = d.type.ToString() });
        }
        string path = CarLibrary.Save(bp);
        hint = "已存檔：" + bp.name;
        Debug.Log("車輛存檔位置：" + path);
    }

    void LoadCar()
    {
        CarBlueprint bp = CarLibrary.Load(carName);
        if (bp == null) { hint = "找不到：" + carName; return; }

        ClearAll();
        int n = 0;
        foreach (var b in bp.blocks)
        {
            PartDef def = CarLibrary.FindPart(b.part);
            if (def == null) continue;
            PlaceBlock(new Vector3Int(b.x, b.y, b.z), def);
            n++;
        }
        hint = "已載入：" + bp.name + "（" + n + " 個方塊）";
    }

    void ClearAll()
    {
        foreach (var b in new List<GameObject>(blocks.Values)) Destroy(b);
        blocks.Clear();
        cells.Clear();
    }

    // ───────────────────────── 方塊 ─────────────────────────

    void Select(int index)
    {
        selected = index;
        RebuildGhost();
    }

    void RebuildGhost()
    {
        if (ghost != null) Destroy(ghost);
        ghost = PartFactory.Create(PartCatalog.All[selected], cellSize);
        ghost.name = "Ghost";
        foreach (var c in ghost.GetComponentsInChildren<Collider>()) c.enabled = false;
        foreach (var r in ghost.GetComponentsInChildren<Renderer>())
            r.material.color = Color.Lerp(PartCatalog.All[selected].color, Color.white, 0.45f);
        ghost.SetActive(false);
    }

    void PlaceBlock(Vector3Int cell, PartDef def)
    {
        GameObject b = PartFactory.Create(def, cellSize);
        b.transform.SetParent(vehicleRoot);
        b.transform.position = CellToWorld(cell);
        b.name = def.displayName + " " + cell.x + "," + cell.y + "," + cell.z;
        blocks[cell] = b;
        cells[b] = cell;
    }

    void RemoveBlock(Vector3Int cell)
    {
        GameObject b;
        if (!blocks.TryGetValue(cell, out b)) return;
        blocks.Remove(cell);
        cells.Remove(b);
        Destroy(b);
    }

    Vector3 CellToWorld(Vector3Int c) { return (new Vector3(c.x, c.y, c.z) + Vector3.one * 0.5f) * cellSize; }

    int CountOf(PartType t)
    {
        int n = 0;
        foreach (var b in blocks.Values) if (b.GetComponent<BlockInfo>().def.type == t) n++;
        return n;
    }

    VehicleStats CurrentStats()
    {
        var defs = new List<PartDef>();
        foreach (var b in blocks.Values) defs.Add(b.GetComponent<BlockInfo>().def);
        return VehicleStats.From(defs);
    }

    // ───────────────────────── 介面 ─────────────────────────

    void OnGUI()
    {
        var btn = new GUIStyle(GUI.skin.button) { fontSize = 16 };
        var lbl = new GUIStyle(GUI.skin.label) { fontSize = 15 };
        lbl.normal.textColor = Color.white;
        var field = new GUIStyle(GUI.skin.textField) { fontSize = 16 };

        if (testDriving)
        {
            GUI.Box(new Rect(10, 10, PanelW, 60), "");
            if (GUI.Button(new Rect(20, 20, PanelW - 20, BtnH), "T  Back to Edit", btn))
                ToggleTestDrive();
            return;
        }

        GUI.Box(panelRect, "");
        Color old = GUI.backgroundColor;
        for (int i = 0; i < PartCatalog.All.Length; i++)
        {
            Rect r = new Rect(20, 20 + i * (BtnH + Gap), PanelW - 20, BtnH);
            GUI.backgroundColor = i == selected ? Color.green : Color.white;
            if (GUI.Button(r, (i + 1) + "  " + PartCatalog.All[i].displayName, btn)) Select(i);
        }
        GUI.backgroundColor = old;

        float mass = 0f;
        foreach (var b in blocks.Values) mass += b.GetComponent<BlockInfo>().def.mass;
        VehicleStats st = CurrentStats();
        GUI.Label(new Rect(20, yStats, PanelW - 20, 22),
            "方塊 " + blocks.Count + "　重量 " + mass.ToString("0.0"), lbl);
        var loadLbl = new GUIStyle(lbl);
        loadLbl.normal.textColor = st.Overloaded ? new Color(1f, 0.4f, 0.35f) : Color.white;
        GUI.Label(new Rect(20, yStats + 22, PanelW - 20, 22),
            "負荷 " + st.load.ToString("0.#") + " / " + st.loadCapacity.ToString("0.#") + (st.Overloaded ? "（超載）" : ""), loadLbl);
        for (int i = 0; i < 5; i++)
        {
            float y = yStats + 50 + i * 26;
            GUI.Label(new Rect(20, y, 44, 22), VehicleStats.Names[i], lbl);
            Rect bar = new Rect(66, y + 5, PanelW - 66 - 44, 12);
            GUI.color = new Color(0.3f, 0.3f, 0.3f);
            GUI.DrawTexture(bar, Texture2D.whiteTexture);
            GUI.color = new Color(0.35f, 0.8f, 0.45f);
            GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * st[i] / VehicleStats.Max, bar.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(PanelW - 38, y, 40, 22), st[i].ToString("0.#"), lbl);
        }

        if (GUI.Button(new Rect(20, yTest, PanelW - 20, BtnH), "T  Test Drive", btn))
        {
            ToggleTestDrive();
            return;
        }

        carName = GUI.TextField(new Rect(20, yName, PanelW - 20, 28), carName, 24, field);
        float half = (PanelW - 20 - 4) / 2f;
        if (GUI.Button(new Rect(20, ySave, half, BtnH), "Save", btn)) SaveCar();
        if (GUI.Button(new Rect(20 + half + 4, ySave, half, BtnH), "Load", btn)) LoadCar();

        GUI.Label(new Rect(20, yHint, PanelW - 20, 60), hint, lbl);
    }
}