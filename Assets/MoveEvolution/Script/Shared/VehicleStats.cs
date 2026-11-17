using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 五項性能＋負荷值。由車上所有零件加總，性能限制在 0–10。
/// 組車畫面顯示用，之後評分（NPC 需求符合度）也用這個。
/// </summary>
[System.Serializable]
public struct VehicleStats
{
    public const float Max = 10f;
    public static readonly string[] Names = { "速度", "靈活", "穩定", "容納", "舒適" };

    public float speed, agility, stability, capacity, comfort;
    public float load, loadCapacity;

    public bool Overloaded { get { return load > loadCapacity + 0.001f; } }

    public float this[int i]
    {
        get
        {
            switch (i)
            {
                case 0: return speed;
                case 1: return agility;
                case 2: return stability;
                case 3: return capacity;
                default: return comfort;
            }
        }
    }

    public static VehicleStats From(IEnumerable<PartDef> parts)
    {
        var s = new VehicleStats();
        foreach (var d in parts)
        {
            if (d == null) continue;
            s.speed += d.speed;
            s.agility += d.agility;
            s.stability += d.stability;
            s.capacity += d.capacity;
            s.comfort += d.comfort;
            s.load += d.load;
            s.loadCapacity += d.loadCapacity;
        }
        s.speed = Mathf.Clamp(s.speed, 0f, Max);
        s.agility = Mathf.Clamp(s.agility, 0f, Max);
        s.stability = Mathf.Clamp(s.stability, 0f, Max);
        s.capacity = Mathf.Clamp(s.capacity, 0f, Max);
        s.comfort = Mathf.Clamp(s.comfort, 0f, Max);
        return s;
    }

    public static VehicleStats From(CarBlueprint bp)
    {
        var parts = new List<PartDef>();
        if (bp != null)
            foreach (var b in bp.blocks) parts.Add(CarLibrary.FindPart(b.part));
        return From(parts);
    }
}
