using UnityEngine;

/// <summary>
/// NPC 的「大腦」：沿 LanePath 行駛（純追蹤轉向）、依前方障礙用 IDM 跟車模型加減速、
/// 過彎自動降速。只透過 IVehicleController 控制車子，所以也能接在玩家造的車上。
/// </summary>
[RequireComponent(typeof(SimpleVehicle))]
public class TrafficDriver : MonoBehaviour
{
    public LanePath path;
    public int targetIndex;
    public float laneOffset;
    public float speedFactor = 1f;      // 個性：1 = 開到極速，0.7 = 慢慢開
    public float maxLateralAccel = 4.5f; // 過彎容許的側向加速度
    public float timeHeadway = 1.3f;     // 跟車時距(秒)
    public float minGap = 2f;            // 停車最小間距(公尺)
    public float senseInterval = 0.1f;   // 感測更新間隔 = 反應時間的一部分
    public float senseRange = 45f;
    public bool debugDraw = false;

    SimpleVehicle car;
    IVehicleController ctrl;
    float senseTimer;
    float gap = float.MaxValue;
    float leaderSpeed;
    readonly Collider[] buf = new Collider[8];

    public void Init(LanePath p, int startIndex, float offset, float factor)
    {
        path = p; targetIndex = startIndex; laneOffset = offset; speedFactor = factor;
    }

    void Awake()
    {
        car = GetComponent<SimpleVehicle>();
        ctrl = car;
        senseTimer = Random.value * senseInterval;
    }

    Vector3 Target(int i) => path.PointWithOffset(i, laneOffset);

    void FixedUpdate()
    {
        if (path == null || path.Count < 2) return;
        if (!ctrl.IsOperational) return;

        var spec = car.spec;
        float v = car.ForwardSpeed;
        Vector3 pos = transform.position;

        // 1) 選目標點（前視距離隨速度變大）
        float ld = Mathf.Max(4f, v * 0.8f + 3f);
        for (int k = 0; k < 4; k++)
        {
            Vector3 d = Target(targetIndex) - pos; d.y = 0;
            if (d.magnitude < ld || Vector3.Dot(transform.forward, d) < 0f) targetIndex++;
            else break;
        }
        Vector3 to = Target(targetIndex) - pos; to.y = 0;
        float dist = Mathf.Max(0.1f, to.magnitude);
        float alpha = Vector3.SignedAngle(transform.forward, to, Vector3.up) * Mathf.Deg2Rad;

        // 2) 純追蹤轉向
        float steerAngle = Mathf.Atan(2f * spec.wheelbase * Mathf.Sin(alpha) / dist) * Mathf.Rad2Deg;
        float steer = Mathf.Clamp(steerAngle / spec.maxSteerDeg, -1f, 1f);

        // 3) 期望速度（極速 × 個性，再受彎道限制）
        float kappa = 2f * Mathf.Abs(Mathf.Sin(alpha)) / dist;
        float vCurve = Mathf.Sqrt(maxLateralAccel / Mathf.Max(kappa, 0.001f));
        float v0 = Mathf.Max(2.5f, Mathf.Min(spec.maxSpeedKmh / 3.6f * speedFactor, vCurve));

        // 4) 感測（沿路徑往前掃）
        senseTimer -= Time.fixedDeltaTime;
        if (senseTimer <= 0f) { senseTimer = senseInterval; Sense(ld, v); }

        // 5) IDM
        float aMax = spec.accel;
        float b = spec.brake * 0.6f;
        float free = 1f - Mathf.Pow(Mathf.Max(0f, v) / v0, 4f);
        float inter = 0f;
        if (gap < senseRange)
        {
            float dv = v - leaderSpeed;
            float sStar = minGap + Mathf.Max(0f, v * timeHeadway + v * dv / (2f * Mathf.Sqrt(aMax * b)));
            inter = Mathf.Pow(sStar / Mathf.Max(gap, 0.1f), 2f);
        }
        float acc = aMax * (free - inter);

        float throttle = acc > 0f ? Mathf.Clamp01(acc / aMax) : 0f;
        float brake = acc < 0f ? Mathf.Clamp01(-acc / spec.brake) : 0f;
        ctrl.SetInput(throttle, steer, brake, false);
    }

    void Sense(float ld, float v)
    {
        gap = float.MaxValue; leaderSpeed = 0f;
        var spec = car.spec;
        float range = Mathf.Min(senseRange, 8f + Mathf.Max(0f, v) * 3f);
        float step = 2.5f;
        Vector3 half = new Vector3(spec.size.x * 0.5f + 0.15f, spec.size.y * 0.4f, step * 0.5f + 0.1f);

        // 路徑折線：從車頭前緣 -> 目標點 -> 後續路徑點
        Vector3 prev = transform.position + transform.forward * (spec.size.z * 0.5f);
        int idx = targetIndex;
        Vector3 next = Target(idx);
        float travelled = 0f;
        float own = spec.size.z * 0.5f;

        for (int guard = 0; guard < 16 && travelled < range; guard++)
        {
            Vector3 seg = next - prev; seg.y = 0;
            float len = seg.magnitude;
            if (len < 0.01f) { idx++; next = Target(idx); continue; }
            Vector3 dir = seg / len;
            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);
            for (float s = 0f; s < len && travelled + s < range; s += step)
            {
                Vector3 c = prev + dir * s;
                c.y = transform.position.y;
                int n = Physics.OverlapBoxNonAlloc(c, half, buf, rot, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < n; i++)
                {
                    var col = buf[i];
                    if (col.transform.IsChildOf(transform)) continue;
                    gap = travelled + s - 0.5f; // 估計：到對方的距離，扣掉一點
                    var orb = col.attachedRigidbody;
                    if (orb != null) leaderSpeed = Mathf.Max(0f, Vector3.Dot(orb.linearVelocity, dir));
                    if (debugDraw) Debug.DrawLine(transform.position, c, Color.red, senseInterval);
                    return;
                }
                if (debugDraw) Debug.DrawLine(c, c + Vector3.up * 0.5f, Color.green, senseInterval);
            }
            travelled += len;
            prev = next; idx++; next = Target(idx);
        }
    }
}
