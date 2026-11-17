using UnityEngine;

public static class PartFactory
{
    public static GameObject Create(PartDef def, float cellSize)
    {
        // 根物件負責碰撞（整格大小），外觀是子物件
        GameObject root = new GameObject(def.displayName);
        BoxCollider box = root.AddComponent<BoxCollider>();
        box.size = Vector3.one * cellSize;
        root.AddComponent<BlockInfo>().def = def;

        GameObject visual = GameObject.CreatePrimitive(def.shape);
        Object.Destroy(visual.GetComponent<Collider>());
        visual.transform.SetParent(root.transform, false);
        visual.transform.localScale = def.scale * cellSize;
        visual.transform.localEulerAngles = def.euler;
        visual.GetComponent<Renderer>().material.color = def.color;
        return root;
    }
}