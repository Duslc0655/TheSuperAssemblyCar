using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Menu: SuperAssemblyCar > Place Street Lamps / Toggle Street Lamp Lights
// Drag Assets/Models/SuperAssemblyCar_City.fbx into the scene first; the city contains
// 266 empty "LampMarker_###" transforms where lamps belong.
public static class StreetLampTools
{
    const string LampPath = "Assets/Models/StreetLamp.fbx";
    const string ParentName = "StreetLamps";

    [MenuItem("SuperAssemblyCar/Place Street Lamps")]
    static void Place()
    {
        var lamp = AssetDatabase.LoadAssetAtPath<GameObject>(LampPath);
        if (lamp == null) { EditorUtility.DisplayDialog("Street Lamps", "找不到 " + LampPath, "OK"); return; }

        var markers = new List<Transform>();
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name.StartsWith("LampMarker_")) markers.Add(t);
        if (markers.Count == 0)
        {
            EditorUtility.DisplayDialog("Street Lamps", "場景裡沒有 LampMarker。請先把 Assets/Models/SuperAssemblyCar_City.fbx 拖進場景。", "OK");
            return;
        }

        var old = GameObject.Find(ParentName);
        if (old != null) Undo.DestroyObjectImmediate(old);
        var parent = new GameObject(ParentName);
        Undo.RegisterCreatedObjectUndo(parent, "Place Street Lamps");

        foreach (var m in markers)
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(lamp, parent.transform);
            inst.transform.SetPositionAndRotation(m.position, m.rotation);
            inst.transform.localScale = Vector3.one;
            inst.name = "StreetLamp_" + m.name.Substring("LampMarker_".Length);

            Transform lp = FindDeep(inst.transform, "LightPoint") ?? inst.transform;
            var lgo = new GameObject("Light");
            lgo.transform.SetParent(lp, false);
            lgo.transform.rotation = Quaternion.LookRotation(Vector3.down, inst.transform.forward);
            var light = lgo.AddComponent<Light>();
            light.type = LightType.Spot;
            light.spotAngle = 110f;
            light.range = 16f;
            light.intensity = 8f;
            light.color = new Color(1f, 0.85f, 0.6f);
            light.shadows = LightShadows.None;

            var cap = inst.AddComponent<CapsuleCollider>();
            cap.radius = 0.15f; cap.height = 7f; cap.center = new Vector3(0f, 3.5f, 0f); cap.direction = 1;

            foreach (var r in inst.GetComponentsInChildren<Renderer>())
                GameObjectUtility.SetStaticEditorFlags(r.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
        }
        Selection.activeGameObject = parent;
        Debug.Log($"[SuperAssemblyCar] Placed {markers.Count} street lamps.");
    }

    [MenuItem("SuperAssemblyCar/Toggle Street Lamp Lights")]
    static void ToggleLights()
    {
        var parent = GameObject.Find(ParentName);
        if (parent == null) { EditorUtility.DisplayDialog("Street Lamps", "請先執行 Place Street Lamps。", "OK"); return; }
        var lights = parent.GetComponentsInChildren<Light>(true);
        if (lights.Length == 0) return;
        bool on = !lights[0].enabled;
        Undo.RecordObjects(lights, "Toggle Street Lamp Lights");
        foreach (var l in lights) l.enabled = on;
    }

    static Transform FindDeep(Transform t, string name)
    {
        if (t.name == name) return t;
        foreach (Transform c in t) { var r = FindDeep(c, name); if (r != null) return r; }
        return null;
    }
}
