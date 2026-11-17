using UnityEngine;

public enum VehicleKind { Bicycle, Motorcycle, Sedan }

/// <summary>NPC 交通工具的規格表。想調手感就改這裡。</summary>
[System.Serializable]
public struct VehicleSpec
{
    public VehicleKind kind;
    public Vector3 size;          // 碰撞盒大小 (寬, 高, 長)，長 = Z
    public float mass;
    public float maxSpeedKmh;
    public float accel;           // 最大加速度 m/s^2
    public float brake;           // 最大煞車減速度 m/s^2
    public float wheelbase;       // 軸距，決定轉彎半徑
    public float maxSteerDeg;     // 最大轉向角
    public float crashDeltaV;     // 受撞擊速度變化超過此值就翻車/報廢 (m/s)
    public float laneOffset;      // 預設走道路的哪一側（右為正）
    public Color color;

    public static VehicleSpec Get(VehicleKind k)
    {
        switch (k)
        {
            case VehicleKind.Bicycle:
                return new VehicleSpec { kind = k, size = new Vector3(0.6f, 1.1f, 1.8f), mass = 85f, maxSpeedKmh = 18f, accel = 1.5f, brake = 5f, wheelbase = 1.1f, maxSteerDeg = 35f, crashDeltaV = 2.5f, laneOffset = 2.8f, color = new Color(0.2f, 0.7f, 0.3f) };
            case VehicleKind.Motorcycle:
                return new VehicleSpec { kind = k, size = new Vector3(0.8f, 1.2f, 2.1f), mass = 220f, maxSpeedKmh = 55f, accel = 4f, brake = 8f, wheelbase = 1.4f, maxSteerDeg = 30f, crashDeltaV = 4f, laneOffset = 1.4f, color = new Color(0.9f, 0.5f, 0.1f) };
            default:
                return new VehicleSpec { kind = k, size = new Vector3(1.9f, 1.4f, 4.4f), mass = 1300f, maxSpeedKmh = 50f, accel = 3.5f, brake = 8f, wheelbase = 2.7f, maxSteerDeg = 30f, crashDeltaV = 6f, laneOffset = 0f, color = new Color(0.25f, 0.45f, 0.9f) };
        }
    }
}
