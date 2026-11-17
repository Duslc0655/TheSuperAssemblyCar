using System.Collections.Generic;
using UnityEngine;

public static class VehicleFactory
{
    // position 是車子底部的中心點。NPC 的車 playerControlled 用預設的 false。
    public static VehicleDriver Spawn(CarBlueprint bp, Vector3 position, Quaternion rotation,
                                      bool playerControlled = false, float cellSize = 1f)
    {
        if (bp == null || bp.blocks.Count == 0) return null;

        int minX = int.MaxValue, maxX = int.MinValue, minZ = int.MaxValue, maxZ = int.MinValue;
        foreach (var b in bp.blocks)
        {
            minX = Mathf.Min(minX, b.x); maxX = Mathf.Max(maxX, b.x);
            minZ = Mathf.Min(minZ, b.z); maxZ = Mathf.Max(maxZ, b.z);
        }
        Vector3 footprintCenter = new Vector3((minX + maxX + 1) * 0.5f, 0f, (minZ + maxZ + 1) * 0.5f) * cellSize;

        GameObject root = new GameObject("Vehicle_" + bp.name);
        root.transform.SetPositionAndRotation(position, rotation);

        var blocks = new Dictionary<Vector3Int, GameObject>();
        foreach (var b in bp.blocks)
        {
            PartDef def = CarLibrary.FindPart(b.part);
            if (def == null) continue;
            var cell = new Vector3Int(b.x, b.y, b.z);
            GameObject go = PartFactory.Create(def, cellSize);
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition =
                (new Vector3(cell.x, cell.y, cell.z) + Vector3.one * 0.5f) * cellSize - footprintCenter;
            go.name = def.displayName + " " + cell.x + "," + cell.y + "," + cell.z;
            blocks[cell] = go;
        }

        VehicleDriver driver = root.AddComponent<VehicleDriver>();
        driver.showHud = playerControlled;
        if (!driver.Build(blocks)) { Object.Destroy(root); return null; }
        if (playerControlled) root.AddComponent<PlayerVehicleInput>();
        return driver;
    }
}