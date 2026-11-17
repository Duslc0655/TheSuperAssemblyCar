using System.Collections.Generic;
using UnityEngine;

public class VehicleDriver : MonoBehaviour, IVehicleController
{
    [Header("駕駛")]
    public float enginePower = 30f;         // 每個引擎提供的總扭力，平分給所有驅動輪
    public float topSpeed = 1200f;          // 輪胎最高轉速（度/秒）
    public float coastBrakeTorque = 3f;     // 放開油門時的滑行阻力
    public float maxBrakeTorque = 30f;      // brake = 1 時的煞車力
    public float handbrakeTorque = 60f;     // 手煞車
    public float forwardSign = 1f;          // 如果前進時車子倒退，改成 -1

    [Header("轉向")]
    public float maxSteerAngle = 30f;           // 前輪最大轉角（度）
    public float highSpeedSteerFactor = 0.35f;  // 全速時剩下的轉角比例
    public float steerSpring = 200f;            // 轉向彈簧的力道
    public float steerDamper = 20f;
    public float steerSign = 1f;                // 如果按 D 輪胎往左轉，改成 -1

    [Header("試駕開始時")]
    public float wheelScale = 1.3f;
    public float liftHeight = 0.2f;

    [Header("油量")]
    public float fuelBase = 20f;
    public float fuelPerTank = 40f;

    [Header("撞擊")]
    public float strengthMultiplier = 1f;
    public float burstSpeed = 3f;
    public bool logImpacts = true;

    [Header("顯示")]
    public bool showHud = true;             // NPC 的車要關掉

    // ── 介面 IVehicleController ──

    public void SetInput(float throttle, float steer, float brake, bool handbrake = false)
    {
        inThrottle = Mathf.Clamp(throttle, -1f, 1f);
        inSteer = Mathf.Clamp(steer, -1f, 1f);
        inBrake = Mathf.Clamp01(brake);
        inHandbrake = handbrake;
    }

    public Transform VehicleTransform
    {
        get { return mainChunk != null ? mainChunk.transform : null; }
    }

    public float CurrentSpeedKmh
    {
        get { return mainChunk != null ? mainChunk.rb.linearVelocity.magnitude * 3.6f : 0f; }
    }

    public float EstimatedMaxSpeedKmh
    {
        get
        {
            if (!IsOperational) return 0f;
            float radius = 0.5f * cellSize * wheelScale;
            return topSpeed * Mathf.Deg2Rad * radius * 3.6f;
        }
    }

    public float TotalMass
    {
        get
        {
            if (mainChunk == null) return 0f;
            float m = mainChunk.rb.mass;
            foreach (var w in wheels) if (IsDriving(w)) m += w.info.def.mass;
            return m;
        }
    }

    public bool IsOperational
    {
        get { return mainChunk != null && Fuel > 0f && EngineCount > 0 && CountDriveWheels() > 0; }
    }

    public Bounds LocalBounds { get { return cachedBounds; } }

    // ── 給編輯器（GridBuilder）用的 ──

    public Transform CenterTransform { get { return VehicleTransform; } }
    public string Error { get; private set; }
    public int EngineCount { get; private set; }
    public float Fuel { get; private set; }
    public float FuelMax { get; private set; }

    class Wheel
    {
        public BlockInfo info;
        public Rigidbody rb;
        public Collider col;
        public HingeJoint joint;        // 輪胎的滾動關節
        public HingeJoint steerJoint;   // 轉向節連到車身的轉向關節（只有轉向輪有）
        public Rigidbody knuckle;       // 看不見的轉向節（只有轉向輪有）
        public VehicleChunk chunk;
        public bool steered;
    }

    struct PendingBreak
    {
        public BlockInfo block;
        public Vector3 point;
    }

    readonly List<Wheel> wheels = new List<Wheel>();
    readonly List<VehicleChunk> chunks = new List<VehicleChunk>();
    readonly Dictionary<Vector3Int, BlockInfo> solids = new Dictionary<Vector3Int, BlockInfo>();
    readonly List<PendingBreak> pending = new List<PendingBreak>();
    readonly HashSet<BlockInfo> pendingSet = new HashSet<BlockInfo>();
    VehicleChunk mainChunk;
    Bounds cachedBounds;
    float inThrottle, inSteer, inBrake;
    bool inHandbrake;
    float cellSize = 1f;

    static readonly Vector3Int[] Dirs =
    {
        new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0),
        new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0),
        new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1)
    };

    // ───────────────────────── 建立 ─────────────────────────

    public bool Build(Dictionary<Vector3Int, GameObject> blocks)
    {
        var solidList = new List<BlockInfo>();
        int minWheelZ = int.MaxValue, maxWheelZ = int.MinValue;
        foreach (var kv in blocks)
        {
            BlockInfo info = kv.Value.GetComponent<BlockInfo>();
            info.cell = kv.Key;
            info.broken = false;
            info.chunk = null;
            if (info.def.type == PartType.Wheel)
            {
                minWheelZ = Mathf.Min(minWheelZ, kv.Key.z);
                maxWheelZ = Mathf.Max(maxWheelZ, kv.Key.z);
                continue;
            }
            solids[kv.Key] = info;
            solidList.Add(info);
        }
        if (solidList.Count == 0) { Error = "至少要有一個非輪胎的方塊"; return false; }

        // 至少前後兩排輪胎，最前面那一排才是轉向輪
        bool hasFrontAxle = maxWheelZ > minWheelZ;

        BoxCollider firstBox = solidList[0].GetComponent<BoxCollider>();
        if (firstBox != null) cellSize = firstBox.size.x;

        foreach (var go in blocks.Values) go.transform.position += Vector3.up * liftHeight;

        // 非輪胎方塊：相連的算同一群，每一群一個剛體
        foreach (var comp in FindComponents(solidList))
        {
            VehicleChunk c = CreateChunk(comp, null);
            if (mainChunk == null || c.rb.mass > mainChunk.rb.mass) mainChunk = c;
        }

        // 輪胎各自一個剛體
        foreach (var kv in blocks)
        {
            BlockInfo info = kv.Value.GetComponent<BlockInfo>();
            if (info.def.type != PartType.Wheel) continue;
            wheels.Add(MakeWheel(info, hasFrontAxle && info.cell.z == maxWheelZ));
        }

        AttachWheels();
        RefreshIgnores();
        RecountDrive();
        RecomputeBounds();
        Fuel = FuelMax;
        return true;
    }

    Wheel MakeWheel(BlockInfo info, bool steered)
    {
        GameObject go = info.gameObject;
        BoxCollider box = go.GetComponent<BoxCollider>();
        float cell = box.size.x;
        DestroyImmediate(box);
        SphereCollider sphere = go.AddComponent<SphereCollider>();
        sphere.radius = 0.5f * cell * wheelScale;

        Transform visual = go.transform.GetChild(0);
        Vector3 s = visual.localScale;
        visual.localScale = new Vector3(s.x * wheelScale, s.y, s.z * wheelScale);

        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.mass = info.def.mass;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.solverIterations = 20;
        rb.solverVelocityIterations = 10;
        rb.maxAngularVelocity = 100f;

        VehicleChunk vc = go.AddComponent<VehicleChunk>();
        vc.driver = this;
        vc.rb = rb;

        return new Wheel { info = info, rb = rb, col = sphere, steered = steered };
    }

    // ───────────────────────── 結構 ─────────────────────────

    static List<List<BlockInfo>> FindComponents(List<BlockInfo> list)
    {
        var lookup = new Dictionary<Vector3Int, BlockInfo>();
        foreach (var b in list) lookup[b.cell] = b;

        var visited = new HashSet<BlockInfo>();
        var result = new List<List<BlockInfo>>();
        foreach (var start in list)
        {
            if (visited.Contains(start)) continue;
            var comp = new List<BlockInfo>();
            var queue = new Queue<BlockInfo>();
            queue.Enqueue(start);
            visited.Add(start);
            while (queue.Count > 0)
            {
                BlockInfo cur = queue.Dequeue();
                comp.Add(cur);
                foreach (var d in Dirs)
                {
                    BlockInfo n;
                    if (lookup.TryGetValue(cur.cell + d, out n) && visited.Add(n))
                        queue.Enqueue(n);
                }
            }
            result.Add(comp);
        }
        return result;
    }

    static float TotalMassOf(List<BlockInfo> list)
    {
        float m = 0f;
        foreach (var b in list) m += b.def.mass;
        return m;
    }

    VehicleChunk CreateChunk(List<BlockInfo> comp, VehicleChunk from)
    {
        Vector3 com = Vector3.zero;
        float m = 0f;
        foreach (var b in comp)
        {
            com += b.transform.position * b.def.mass;
            m += b.def.mass;
        }
        com /= Mathf.Max(m, 0.0001f);

        GameObject go = new GameObject("Chunk");
        go.transform.SetParent(transform, false);
        go.transform.position = com;
        go.transform.rotation = from != null ? from.transform.rotation : transform.rotation;

        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.solverIterations = 12;
        rb.solverVelocityIterations = 6;

        VehicleChunk chunk = go.AddComponent<VehicleChunk>();
        chunk.driver = this;
        chunk.rb = rb;
        foreach (var b in comp)
        {
            b.transform.SetParent(go.transform, true);
            b.chunk = chunk;
            chunk.blocks.Add(b);
        }
        chunk.RefreshMass();

        if (from != null && from.rb != null)
        {
            rb.linearVelocity = from.rb.GetPointVelocity(com);
            rb.angularVelocity = from.rb.angularVelocity;
        }
        chunks.Add(chunk);
        return chunk;
    }

    void Resplit(VehicleChunk c)
    {
        List<List<BlockInfo>> comps = FindComponents(c.blocks);
        if (comps.Count > 1)
        {
            int keep = 0;
            float best = -1f;
            for (int i = 0; i < comps.Count; i++)
            {
                float m = TotalMassOf(comps[i]);
                if (m > best) { best = m; keep = i; }
            }
            for (int i = 0; i < comps.Count; i++)
            {
                if (i == keep) continue;
                foreach (var b in comps[i]) c.blocks.Remove(b);
                CreateChunk(comps[i], c);
            }
        }
        c.RefreshMass();
    }

    void RecomputeBounds()
    {
        if (mainChunk == null) { cachedBounds = new Bounds(Vector3.zero, Vector3.zero); return; }
        Transform t = mainChunk.transform;
        bool first = true;
        Bounds result = new Bounds(Vector3.zero, Vector3.zero);
        foreach (var b in mainChunk.blocks)
        {
            Bounds one = new Bounds(t.InverseTransformPoint(b.transform.position), Vector3.one * cellSize);
            if (first) { result = one; first = false; } else result.Encapsulate(one);
        }
        foreach (var w in wheels)
        {
            if (!IsDriving(w)) continue;
            Bounds one = new Bounds(t.InverseTransformPoint(w.info.transform.position), Vector3.one * cellSize);
            if (first) { result = one; first = false; } else result.Encapsulate(one);
        }
        cachedBounds = result;
    }

    // ───────────────────────── 輪胎 ─────────────────────────

    bool HasNeighborIn(Wheel w, VehicleChunk chunk)
    {
        foreach (var d in Dirs)
        {
            BlockInfo n;
            if (solids.TryGetValue(w.info.cell + d, out n) && !n.broken && n.chunk == chunk)
                return true;
        }
        return false;
    }

    void AttachWheels()
    {
        foreach (var w in wheels)
        {
            if (w.info.broken) continue;
            if (w.chunk != null && HasNeighborIn(w, w.chunk)) continue;

            VehicleChunk best = null;
            float bestMass = -1f;
            foreach (var d in Dirs)
            {
                BlockInfo n;
                if (!solids.TryGetValue(w.info.cell + d, out n)) continue;
                if (n.broken || n.chunk == null) continue;
                if (n.chunk.rb.mass > bestMass) { bestMass = n.chunk.rb.mass; best = n.chunk; }
            }

            if (best == null) ReleaseWheel(w);
            else Reconnect(w, best);
        }
    }

    Rigidbody CreateKnuckle()
    {
        GameObject go = new GameObject("Knuckle");
        go.transform.SetParent(transform, false);
        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.inertiaTensor = Vector3.one * 0.1f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.solverIterations = 20;
        rb.solverVelocityIterations = 10;
        return rb;
    }

    void Reconnect(Wheel w, VehicleChunk target)
    {
        if (w.joint != null) DestroyImmediate(w.joint);
        if (w.steerJoint != null) DestroyImmediate(w.steerJoint);

        Rigidbody rollAnchor = target.rb;

        if (w.steered)
        {
            // 車身 ──(垂直軸，負責左右轉)── 轉向節 ──(橫向軸，負責滾動)── 輪胎
            if (w.knuckle == null) w.knuckle = CreateKnuckle();
            w.knuckle.transform.SetPositionAndRotation(w.info.transform.position, target.transform.rotation);
            w.knuckle.linearVelocity = w.rb.linearVelocity;
            w.knuckle.angularVelocity = Vector3.zero;

            HingeJoint sj = w.knuckle.gameObject.AddComponent<HingeJoint>();
            sj.connectedBody = target.rb;
            sj.anchor = Vector3.zero;
            sj.axis = Vector3.up;
            sj.useLimits = true;
            sj.limits = new JointLimits { min = -maxSteerAngle, max = maxSteerAngle };
            sj.useSpring = true;
            sj.spring = new JointSpring { spring = steerSpring, damper = steerDamper, targetPosition = 0f };
            w.steerJoint = sj;
            rollAnchor = w.knuckle;
        }

        HingeJoint hj = w.info.gameObject.AddComponent<HingeJoint>();
        hj.connectedBody = rollAnchor;
        hj.anchor = Vector3.zero;
        hj.axis = Vector3.right;
        hj.useMotor = true;
        hj.motor = new JointMotor { force = coastBrakeTorque, targetVelocity = 0f, freeSpin = false };
        w.joint = hj;
        w.chunk = target;
    }

    void ReleaseWheel(Wheel w)
    {
        if (w.joint != null) DestroyImmediate(w.joint);
        if (w.steerJoint != null) DestroyImmediate(w.steerJoint);
        if (w.knuckle != null) Destroy(w.knuckle.gameObject);
        w.joint = null;
        w.steerJoint = null;
        w.knuckle = null;
        w.chunk = null;
    }

    void RefreshIgnores()
    {
        foreach (var w in wheels)
        {
            if (w.col == null) continue;
            foreach (var kv in solids)
            {
                if (kv.Value == null) continue;
                Collider c = kv.Value.GetComponent<Collider>();
                if (c == null) continue;
                Physics.IgnoreCollision(w.col, c, w.chunk != null && kv.Value.chunk == w.chunk);
            }
            foreach (var o in wheels)
            {
                if (o == w || o.col == null) continue;
                Physics.IgnoreCollision(w.col, o.col, w.chunk != null && w.chunk == o.chunk);
            }
        }
    }

    void RecountDrive()
    {
        EngineCount = 0;
        int tanks = 0;
        if (mainChunk != null)
        {
            foreach (var b in mainChunk.blocks)
            {
                if (b.def.type == PartType.Engine) EngineCount++;
                else if (b.def.type == PartType.FuelTank) tanks++;
            }
        }
        FuelMax = fuelBase + tanks * fuelPerTank;
        Fuel = Mathf.Min(Fuel, FuelMax);
    }

    bool IsDriving(Wheel w)
    {
        return w.joint != null && w.chunk != null && w.chunk == mainChunk;
    }

    int CountDriveWheels()
    {
        int n = 0;
        foreach (var w in wheels) if (IsDriving(w)) n++;
        return n;
    }

    // ───────────────────────── 撞擊 ─────────────────────────

    public void ReportImpact(Collision col)
    {
        var sums = new Dictionary<BlockInfo, float>();
        var points = new Dictionary<BlockInfo, Vector3>();
        for (int i = 0; i < col.contactCount; i++)
        {
            ContactPoint cp = col.GetContact(i);
            BlockInfo bi = cp.thisCollider.GetComponent<BlockInfo>();
            if (bi == null) continue;
            float cur;
            sums.TryGetValue(bi, out cur);
            sums[bi] = cur + cp.impulse.magnitude;
            if (!points.ContainsKey(bi)) points[bi] = cp.point;
        }

        foreach (var kv in sums)
        {
            BlockInfo bi = kv.Key;
            if (bi.broken) continue;
            float limit = bi.def.strength * strengthMultiplier;
            if (logImpacts && kv.Value > limit * 0.3f)
                Debug.Log(bi.def.displayName + " 受到衝量 " + kv.Value.ToString("0.0") + " / 耐受 " + limit.ToString("0"));
            if (kv.Value > limit && pendingSet.Add(bi))
                pending.Add(new PendingBreak { block = bi, point = points[bi] });
        }
    }

    void ProcessPending()
    {
        if (pending.Count == 0) return;
        var list = new List<PendingBreak>(pending);
        pending.Clear();
        pendingSet.Clear();

        bool changed = false;
        foreach (var p in list)
        {
            if (p.block == null || p.block.broken) continue;
            BreakBlock(p.block, p.point);
            changed = true;
        }
        if (changed)
        {
            AttachWheels();
            RefreshIgnores();
            RecountDrive();
            RecomputeBounds();
        }
    }

    void BreakBlock(BlockInfo b, Vector3 point)
    {
        b.broken = true;
        Vector3 dir = b.transform.position - point;
        dir.y += 0.3f;
        dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.up;

        if (b.def.type == PartType.Wheel)
        {
            foreach (var w in wheels)
            {
                if (w.info != b) continue;
                ReleaseWheel(w);
                w.rb.linearVelocity += dir * burstSpeed * 0.5f;
            }
            return;
        }

        VehicleChunk old = b.chunk;
        if (old == null) return;
        old.blocks.Remove(b);

        VehicleChunk debris = CreateChunk(new List<BlockInfo> { b }, old);
        debris.rb.linearVelocity += dir * burstSpeed;

        if (old.blocks.Count == 0)
        {
            chunks.Remove(old);
            Destroy(old.gameObject);
        }
        else
        {
            Resplit(old);
        }
    }

    // ───────────────────────── 駕駛 ─────────────────────────

    void FixedUpdate()
    {
        ProcessPending();

        int driveWheels = CountDriveWheels();
        bool hasPower = Fuel > 0f && EngineCount > 0 && driveWheels > 0;
        float torquePerWheel = hasPower ? EngineCount * enginePower / driveWheels : 0f;

        bool braking = inBrake > 0.05f || inHandbrake;
        float thr = braking ? 0f : inThrottle;   // 煞車優先於油門

        // 前輪轉角：速度越快，能轉的角度越小
        float speed = mainChunk != null ? mainChunk.rb.linearVelocity.magnitude : 0f;
        float topMs = Mathf.Max(EstimatedMaxSpeedKmh / 3.6f, 1f);
        float steerAngle = inSteer * maxSteerAngle
                         * Mathf.Lerp(1f, highSpeedSteerFactor, Mathf.Clamp01(speed / topMs))
                         * steerSign;

        foreach (var w in wheels)
        {
            if (w.joint == null) continue;

            if (w.steerJoint != null)
            {
                JointSpring sp = w.steerJoint.spring;
                sp.targetPosition = steerAngle;
                w.steerJoint.spring = sp;
            }

            JointMotor m = w.joint.motor;
            if (inHandbrake)
            {
                m.force = handbrakeTorque;
                m.targetVelocity = 0f;
            }
            else if (inBrake > 0.05f)
            {
                m.force = coastBrakeTorque + inBrake * maxBrakeTorque;
                m.targetVelocity = 0f;
            }
            else if (hasPower && IsDriving(w) && Mathf.Abs(thr) > 0.01f)
            {
                m.force = torquePerWheel * Mathf.Abs(thr);
                m.targetVelocity = thr * topSpeed * forwardSign;
            }
            else
            {
                m.force = coastBrakeTorque;
                m.targetVelocity = 0f;
            }
            w.joint.motor = m;
        }

        if (hasPower && !braking)
            Fuel = Mathf.Max(0f, Fuel - Mathf.Abs(inThrottle) * EngineCount * Time.fixedDeltaTime);
    }

    void OnDestroy()
    {
        foreach (var c in chunks)
            if (c != null) Destroy(c.gameObject);
        foreach (var w in wheels)
            if (w.knuckle != null) Destroy(w.knuckle.gameObject);
    }

    void OnGUI()
    {
        if (!showHud || mainChunk == null) return;
        float w = 400f, h = 100f;
        Rect r = new Rect((Screen.width - w) / 2f, Screen.height - h - 10f, w, h);
        GUI.Box(r, "");

        var lbl = new GUIStyle(GUI.skin.label) { fontSize = 16 };
        lbl.normal.textColor = Color.white;
        GUI.Label(new Rect(r.x + 10, r.y + 6, w - 20, 26),
            "Speed: " + CurrentSpeedKmh.ToString("0") + " km/h", lbl);

        float frac = FuelMax > 0f ? Fuel / FuelMax : 0f;
        Rect bar = new Rect(r.x + 10, r.y + 36, w - 20, 16);
        GUI.color = Color.gray;
        GUI.DrawTexture(bar, Texture2D.whiteTexture);
        GUI.color = Color.Lerp(Color.red, Color.green, frac);
        GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * frac, bar.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        string warn = EngineCount == 0 ? "主車體沒有引擎"
                    : CountDriveWheels() == 0 ? "主車體沒有輪胎"
                    : Fuel <= 0f ? "沒油了"
                    : "WASD 駕駛　Shift 煞車　Space 手煞　T 回編輯";
        GUI.Label(new Rect(r.x + 10, r.y + 60, w - 20, 30), warn, lbl);
    }
}