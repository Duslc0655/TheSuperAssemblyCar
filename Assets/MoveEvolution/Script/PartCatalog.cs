using UnityEngine;

// 零件類別：依《移動演化》設計文件的 6 類配件＋特殊配件
// 存檔（JSON 藍圖）用的是這裡的英文名字，改名會讓舊藍圖讀不到該零件。
public enum PartType { Body, Interior, Wheel, Suspension, Steering, Engine, Special }

[System.Serializable]
public class PartDef
{
    public PartType type;
    public string displayName;
    public string description;
    public Color color;
    public float mass;
    public float strength;   // 一次撞擊能承受的衝量，超過就會被撞掉
    public PrimitiveType shape;
    public Vector3 scale;    // 相對一格的大小（只影響外觀，碰撞永遠是一整格）
    public Vector3 euler;    // 外觀的旋轉角度

    [Header("性能（每放一個就加一次，總和限制在 0–10）")]
    public float speed;      // 速度
    public float agility;    // 靈活
    public float stability;  // 穩定
    public float capacity;   // 容納
    public float comfort;    // 舒適

    [Header("負荷")]
    public float load;          // 這個零件佔用多少負荷值
    public float loadCapacity;  // 這個零件提供多少負荷上限（只有主體有）
    public int maxPerCar;       // 一台車最多幾個，0 = 不限
}

public static class PartCatalog
{
    public static readonly PartDef[] All =
    {
        new PartDef { type = PartType.Body, displayName = "主體 Body", description = "車身骨架，每格提供 1 點負荷上限",
            color = new Color(0.75f, 0.75f, 0.80f), mass = 1.0f, strength = 60f,
            shape = PrimitiveType.Cube, scale = Vector3.one, euler = Vector3.zero,
            loadCapacity = 1f },

        new PartDef { type = PartType.Interior, displayName = "內裝 Interior", description = "座椅與車廂：容納、舒適",
            color = new Color(0.86f, 0.74f, 0.58f), mass = 1.0f, strength = 40f,
            shape = PrimitiveType.Cube, scale = new Vector3(0.9f, 0.6f, 0.9f), euler = Vector3.zero,
            capacity = 1f, comfort = 1f, load = 2f },

        new PartDef { type = PartType.Wheel, displayName = "輪胎 Wheel", description = "靈活、速度（通常裝 4 個）",
            color = new Color(0.12f, 0.12f, 0.12f), mass = 1.5f, strength = 45f,
            shape = PrimitiveType.Cylinder, scale = new Vector3(1f, 0.25f, 1f), euler = new Vector3(0, 0, 90),
            agility = 0.5f, speed = 0.5f, load = 1f },

        new PartDef { type = PartType.Suspension, displayName = "懸吊 Suspension", description = "穩定、舒適",
            color = new Color(0.95f, 0.78f, 0.20f), mass = 1.0f, strength = 50f,
            shape = PrimitiveType.Cylinder, scale = new Vector3(0.45f, 0.45f, 0.45f), euler = Vector3.zero,
            stability = 1f, comfort = 1f, load = 1f },

        new PartDef { type = PartType.Steering, displayName = "操縱 Steering", description = "靈活、穩定",
            color = new Color(0.55f, 0.35f, 0.85f), mass = 0.8f, strength = 40f,
            shape = PrimitiveType.Cylinder, scale = new Vector3(0.8f, 0.08f, 0.8f), euler = new Vector3(70, 0, 0),
            agility = 1f, stability = 1f, load = 1f },

        new PartDef { type = PartType.Engine, displayName = "動力 Engine", description = "速度（有點吵，舒適略降）",
            color = new Color(0.85f, 0.20f, 0.20f), mass = 3.0f, strength = 70f,
            shape = PrimitiveType.Cube, scale = Vector3.one, euler = Vector3.zero,
            speed = 2f, comfort = -0.5f, load = 3f },

        new PartDef { type = PartType.Special, displayName = "特殊 Special", description = "效果待定（先空著），一台車限裝 1 個",
            color = new Color(1.00f, 0.80f, 0.10f), mass = 1.0f, strength = 40f,
            shape = PrimitiveType.Sphere, scale = new Vector3(0.8f, 0.8f, 0.8f), euler = Vector3.zero,
            maxPerCar = 1 },
    };

    public static PartDef Find(PartType t)
    {
        foreach (var d in All) if (d.type == t) return d;
        return null;
    }
}
