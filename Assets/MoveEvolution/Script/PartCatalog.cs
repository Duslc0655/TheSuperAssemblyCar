using UnityEngine;

public enum PartType { Body, Wheel, Engine, FuelTank, Armor, Wing }

[System.Serializable]
public class PartDef
{
    public PartType type;
    public string displayName;
    public Color color;
    public float mass;
    public float strength;   // 一次撞擊中這個零件能承受的衝量，超過就會被撞飛
    public PrimitiveType shape;
    public Vector3 scale;    // 相對於一格的大小
    public Vector3 euler;    // 外觀的旋轉角度
}

public static class PartCatalog
{
    public static readonly PartDef[] All =
    {
        new PartDef { type = PartType.Body,     displayName = "Body 車身",   color = new Color(0.75f, 0.75f, 0.80f), mass = 1.0f, strength = 60f,  shape = PrimitiveType.Cube,     scale = Vector3.one,                   euler = Vector3.zero },
        new PartDef { type = PartType.Wheel,    displayName = "Wheel 輪胎",  color = new Color(0.12f, 0.12f, 0.12f), mass = 1.5f, strength = 45f,  shape = PrimitiveType.Cylinder, scale = new Vector3(1f, 0.25f, 1f),    euler = new Vector3(0, 0, 90) },
        new PartDef { type = PartType.Engine,   displayName = "Engine 引擎", color = new Color(0.85f, 0.20f, 0.20f), mass = 3.0f, strength = 70f,  shape = PrimitiveType.Cube,     scale = Vector3.one,                   euler = Vector3.zero },
        new PartDef { type = PartType.FuelTank, displayName = "Fuel 油箱",   color = new Color(0.95f, 0.60f, 0.15f), mass = 2.0f, strength = 30f,  shape = PrimitiveType.Cylinder, scale = new Vector3(0.8f, 0.5f, 0.8f), euler = new Vector3(90, 0, 0) },
        new PartDef { type = PartType.Armor,    displayName = "Armor 裝甲",  color = new Color(0.35f, 0.40f, 0.50f), mass = 3.0f, strength = 150f, shape = PrimitiveType.Cube,     scale = Vector3.one,                   euler = Vector3.zero },
        new PartDef { type = PartType.Wing,     displayName = "Wing 翅膀",   color = new Color(0.40f, 0.70f, 0.95f), mass = 0.5f, strength = 15f,  shape = PrimitiveType.Cube,     scale = new Vector3(1f, 0.15f, 1f),    euler = Vector3.zero },
    };
}