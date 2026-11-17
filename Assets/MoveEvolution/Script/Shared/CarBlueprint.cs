using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class BlueprintBlock
{
    public int x, y, z;
    public string part;     // PartType 的名字，例如 "Body"、"Wheel"
}

[Serializable]
public class CarBlueprint
{
    public string name = "NewCar";
    public int version = 1;
    public List<BlueprintBlock> blocks = new List<BlueprintBlock>();
}

public static class CarLibrary
{
    const string Folder = "Cars";

    static string SaveDir { get { return Path.Combine(Application.persistentDataPath, Folder); } }

    public static string SafeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "NewCar";
        foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        return name.Trim();
    }

    // 玩家存檔：寫到電腦的使用者資料夾，不會進 Git
    public static string Save(CarBlueprint bp)
    {
        bp.name = SafeName(bp.name);
        Directory.CreateDirectory(SaveDir);
        string path = Path.Combine(SaveDir, bp.name + ".json");
        File.WriteAllText(path, JsonUtility.ToJson(bp, true));
        return path;
    }

    // 先找玩家存檔，找不到再找專案內建的範例車（Assets/Resources/Cars）
    public static CarBlueprint Load(string name)
    {
        name = SafeName(name);
        string path = Path.Combine(SaveDir, name + ".json");
        if (File.Exists(path))
            return JsonUtility.FromJson<CarBlueprint>(File.ReadAllText(path));

        TextAsset ta = Resources.Load<TextAsset>(Folder + "/" + name);
        return ta != null ? JsonUtility.FromJson<CarBlueprint>(ta.text) : null;
    }

    public static PartDef FindPart(string typeName)
    {
        foreach (var d in PartCatalog.All)
            if (d.type.ToString() == typeName) return d;
        return null;
    }
}