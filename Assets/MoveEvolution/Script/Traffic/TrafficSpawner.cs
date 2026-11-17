using System.Collections.Generic;
using UnityEngine;

/// <summary>在 LanePath 上生成並維護 NPC 車流；報廢的車過一陣子會清掉並補生新車。</summary>
public class TrafficSpawner : MonoBehaviour
{
    public LanePath path;
    public int bicycles = 3;
    public int motorcycles = 4;
    public int sedans = 6;
    public float wreckLifetime = 12f;
    public float respawnClearRadius = 5f;

    class Slot { public VehicleKind kind; public SimpleVehicle car; }
    readonly List<Slot> slots = new List<Slot>();

    void Start()
    {
        if (path == null) { Debug.LogError("TrafficSpawner needs a LanePath"); enabled = false; return; }

        var kinds = new List<VehicleKind>();
        for (int i = 0; i < bicycles; i++) kinds.Add(VehicleKind.Bicycle);
        for (int i = 0; i < motorcycles; i++) kinds.Add(VehicleKind.Motorcycle);
        for (int i = 0; i < sedans; i++) kinds.Add(VehicleKind.Sedan);
        for (int i = kinds.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); var t = kinds[i]; kinds[i] = kinds[j]; kinds[j] = t; }

        for (int i = 0; i < kinds.Count; i++)
        {
            var s = new Slot { kind = kinds[i] };
            int idx = Mathf.FloorToInt((float)i * path.Count / kinds.Count);
            s.car = Spawn(s.kind, idx);
            slots.Add(s);
        }
    }

    SimpleVehicle Spawn(VehicleKind kind, int idx)
    {
        var spec = VehicleSpec.Get(kind);
        Vector3 p = path.PointWithOffset(idx, spec.laneOffset);
        Vector3 dir = path.PointWithOffset(idx + 1, spec.laneOffset) - p; dir.y = 0;
        float y = p.y;
        if (Physics.Raycast(p + Vector3.up * 5f, Vector3.down, out var hit, 20f, ~0, QueryTriggerInteraction.Ignore)) y = hit.point.y;
        p.y = y + spec.size.y * 0.5f + 0.05f;

        var car = SimpleVehicle.Create(kind, p, Quaternion.LookRotation(dir.normalized, Vector3.up));
        car.transform.SetParent(transform);
        var drv = car.gameObject.AddComponent<TrafficDriver>();
        drv.Init(path, idx + 1, spec.laneOffset, Random.Range(0.8f, 1f));
        return car;
    }

    void Update()
    {
        foreach (var s in slots)
        {
            bool dead = s.car == null || s.car.WreckAge > wreckLifetime || s.car.transform.position.y < -10f;
            if (!dead) continue;
            if (s.car != null) Destroy(s.car.gameObject);
            for (int tries = 0; tries < 10; tries++)
            {
                int idx = Random.Range(0, path.Count);
                if (!Physics.CheckSphere(path.GetPoint(idx) + Vector3.up, respawnClearRadius, ~0, QueryTriggerInteraction.Ignore))
                { s.car = Spawn(s.kind, idx); break; }
            }
        }
    }
}
