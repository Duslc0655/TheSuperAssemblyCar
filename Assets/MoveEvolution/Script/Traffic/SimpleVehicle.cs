using UnityEngine;

/// <summary>
/// NPC 用的輕量街機車輛：一個剛體 + 一個碰撞盒，實作 IVehicleController。
/// 沒速度就不能轉彎；受到夠大的撞擊會報廢（翻車、失去控制）。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class SimpleVehicle : MonoBehaviour, IVehicleController
{
    public VehicleSpec spec;
    public float grip = 8f;          // 側向抓地，越大越不會滑
    public bool Wrecked { get; private set; }
    public float WreckAge { get; private set; }

    Rigidbody rb;
    BoxCollider box;
    float throttle, steer, brake;
    bool handbrake;

    // ---- IVehicleController ----
    public void SetInput(float t, float s, float b, bool hb = false)
    {
        throttle = Mathf.Clamp(t, -1f, 1f);
        steer = Mathf.Clamp(s, -1f, 1f);
        brake = Mathf.Clamp01(b);
        handbrake = hb;
    }
    public Transform VehicleTransform => this != null ? transform : null;
    public float CurrentSpeedKmh => rb != null ? rb.linearVelocity.magnitude * 3.6f : 0f;
    public float EstimatedMaxSpeedKmh => Wrecked ? 0f : spec.maxSpeedKmh;
    public float TotalMass => rb != null ? rb.mass : spec.mass;
    public bool IsOperational => !Wrecked;
    public Bounds LocalBounds => new Bounds(Vector3.zero, spec.size);
    public float ForwardSpeed => rb != null ? Vector3.Dot(rb.linearVelocity, transform.forward) : 0f;
    public Rigidbody Body => rb;

    // ---- 建立 ----
    public static SimpleVehicle Create(VehicleKind kind, Vector3 pos, Quaternion rot)
    {
        var spec = VehicleSpec.Get(kind);
        var go = new GameObject("NPC_" + kind);
        go.transform.SetPositionAndRotation(pos, rot);

        var rb = go.AddComponent<Rigidbody>();
        rb.mass = spec.mass;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        var box = go.AddComponent<BoxCollider>();
        box.size = spec.size;
        box.sharedMaterial = new PhysicsMaterial("npc_slide") { dynamicFriction = 0f, staticFriction = 0f, frictionCombine = PhysicsMaterialCombine.Minimum, bounciness = 0f };

        var v = go.AddComponent<SimpleVehicle>();
        v.spec = spec;
        v.rb = rb;
        v.box = box;
        v.BuildVisual();
        return v;
    }

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (box == null) box = GetComponent<BoxCollider>();
    }

    void BuildVisual()
    {
        Color c = spec.color;
        Color dark = new Color(0.1f, 0.1f, 0.1f);
        Vector3 s = spec.size;
        switch (spec.kind)
        {
            case VehicleKind.Bicycle:
                Part(PrimitiveType.Cube, new Vector3(0, -0.1f, 0), new Vector3(0.08f, 0.08f, 1.2f), c);
                Part(PrimitiveType.Cylinder, new Vector3(0, -0.3f, 0.65f), new Vector3(0.7f, 0.04f, 0.7f), dark, new Vector3(0, 0, 90));
                Part(PrimitiveType.Cylinder, new Vector3(0, -0.3f, -0.65f), new Vector3(0.7f, 0.04f, 0.7f), dark, new Vector3(0, 0, 90));
                Part(PrimitiveType.Capsule, new Vector3(0, 0.3f, -0.1f), new Vector3(0.35f, 0.4f, 0.35f), new Color(0.9f, 0.8f, 0.7f));
                break;
            case VehicleKind.Motorcycle:
                Part(PrimitiveType.Cube, new Vector3(0, -0.1f, 0), new Vector3(0.3f, 0.3f, 1.2f), c);
                Part(PrimitiveType.Cylinder, new Vector3(0, -0.35f, 0.7f), new Vector3(0.55f, 0.1f, 0.55f), dark, new Vector3(0, 0, 90));
                Part(PrimitiveType.Cylinder, new Vector3(0, -0.35f, -0.7f), new Vector3(0.55f, 0.1f, 0.55f), dark, new Vector3(0, 0, 90));
                Part(PrimitiveType.Capsule, new Vector3(0, 0.3f, -0.1f), new Vector3(0.4f, 0.45f, 0.4f), new Color(0.2f, 0.2f, 0.25f));
                break;
            default:
                Part(PrimitiveType.Cube, new Vector3(0, -0.15f, 0), new Vector3(s.x, 0.6f, s.z), c);
                Part(PrimitiveType.Cube, new Vector3(0, 0.35f, -0.2f), new Vector3(s.x * 0.9f, 0.5f, s.z * 0.5f), c * 0.8f);
                float wx = s.x * 0.5f, wz = s.z * 0.32f;
                foreach (var p in new[] { new Vector2(wx, wz), new Vector2(-wx, wz), new Vector2(wx, -wz), new Vector2(-wx, -wz) })
                    Part(PrimitiveType.Cylinder, new Vector3(p.x, -0.45f, p.y), new Vector3(0.7f, 0.12f, 0.7f), dark, new Vector3(0, 0, 90));
                break;
        }
    }

    void Part(PrimitiveType t, Vector3 lp, Vector3 ls, Color col, Vector3 euler = default)
    {
        var g = GameObject.CreatePrimitive(t);
        var c = g.GetComponent<Collider>();
        if (c != null) DestroyImmediate(c);
        g.transform.SetParent(transform, false);
        g.transform.localPosition = lp;
        g.transform.localScale = ls;
        g.transform.localEulerAngles = euler;
        SetColor(g.GetComponent<Renderer>(), col);
    }

    static void SetColor(Renderer r, Color col)
    {
        var sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        var m = new Material(sh);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
        m.color = col;
        r.sharedMaterial = m;
    }

    // ---- 物理 ----
    void FixedUpdate()
    {
        if (Wrecked) { WreckAge += Time.fixedDeltaTime; return; }
        float dt = Time.fixedDeltaTime;
        Vector3 v = rb.linearVelocity;
        Vector3 fwd = transform.forward;
        Vector3 right = transform.right;
        float f = Vector3.Dot(v, fwd);
        float lat = Vector3.Dot(v, right);
        float maxV = spec.maxSpeedKmh / 3.6f;

        // 縱向
        if (throttle > 0f && f < maxV) f += throttle * spec.accel * dt;
        else if (throttle < 0f && f > -maxV * 0.3f) f += throttle * spec.accel * dt;
        float brk = brake * spec.brake + (handbrake ? spec.brake * 1.5f : 0f);
        if (brk > 0f) f = Mathf.MoveTowards(f, 0f, brk * dt);
        f = Mathf.MoveTowards(f, 0f, 0.3f * dt); // 滾動阻力

        // 側向抓地
        lat *= Mathf.Max(0f, 1f - grip * dt);

        rb.linearVelocity = fwd * f + right * lat + Vector3.up * v.y;

        // 轉向：速度越慢越轉不動，停住完全不轉
        float yaw = f / spec.wheelbase * Mathf.Tan(steer * spec.maxSteerDeg * Mathf.Deg2Rad);
        rb.angularVelocity = new Vector3(0f, yaw, 0f);
    }

    void OnCollisionEnter(Collision c)
    {
        if (Wrecked) return;
        float dv = c.impulse.magnitude / Mathf.Max(1f, rb.mass);
        if (dv > spec.crashDeltaV) Wreck();
    }

    public void Wreck()
    {
        if (Wrecked) return;
        Wrecked = true;
        throttle = steer = brake = 0f;
        rb.constraints = RigidbodyConstraints.None;
        box.sharedMaterial = new PhysicsMaterial("npc_wreck") { dynamicFriction = 0.6f, staticFriction = 0.6f, bounciness = 0.1f };
        rb.linearDamping = 0.3f;
        rb.angularDamping = 0.5f;
        foreach (var r in GetComponentsInChildren<Renderer>())
            if (r.sharedMaterial != null)
            {
                var m = r.sharedMaterial;
                Color col = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : m.color;
                col *= 0.35f;
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
                m.color = col;
            }
    }
}
