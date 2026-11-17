using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Import rules for the city exported from Blender (TheSuperAssemblyCar.blend).
// - Adds MeshColliders to drivable / solid parts, sets static flags.
// - Emission for street lamps and traffic signals, transparency for glass and water.
public class SuperAssemblyCarImporter : AssetPostprocessor
{
    const string ModelsDir = "Assets/Models/";
    const string CityFile = "SuperAssemblyCar_City.fbx";
    const string LampFile = "StreetLamp.fbx";

    static bool IsOurs(string path) =>
        path.StartsWith(ModelsDir) && (path.EndsWith(CityFile) || path.EndsWith(LampFile));

    public override uint GetVersion() => 3;
    public override int GetPostprocessOrder() => 100;

    void OnPreprocessModel()
    {
        if (!IsOurs(assetPath)) return;
        var mi = (ModelImporter)assetImporter;
        mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        mi.materialLocation = ModelImporterMaterialLocation.InPrefab;
        mi.addCollider = false;            // colliders are added selectively below
        mi.importCameras = false;
        mi.importLights = false;
        mi.importAnimation = false;
        mi.isReadable = false;
    }

    void OnPostprocessMaterial(Material m)
    {
        if (!IsOurs(assetPath)) return;
        string n = m.name;
        bool lamp = n.Contains("StreetLamp_Light");
        if (lamp || n.StartsWith("MAT_Signal_"))
        {
            Color c = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white;
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * (lamp ? 4f : 2f));
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        if (n.Contains("Glass")) MakeTransparent(m, 0.45f, 0.95f);
        else if (n.StartsWith("MAT_Water")) MakeTransparent(m, 0.85f, 0.9f);
        else if (n.StartsWith("MAT_Road_") && m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.15f);
    }

    static void MakeTransparent(Material m, float alpha, float smooth)
    {
        if (!m.HasProperty("_Surface")) return;
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        if (m.HasProperty("_SrcBlendAlpha")) m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        if (m.HasProperty("_DstBlendAlpha")) m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)RenderQueue.Transparent;
        Color c = m.GetColor("_BaseColor"); c.a = alpha; m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
    }

    static readonly string[] ColliderPrefixes = {
        "Road_", "Terrain_", "Prop_Sidewalk", "Prop_River_Guardrail", "Prop_TrafficLights_Poles",
        "Prop_SignPosts", "Factory_", "Port_", "Bldg_", "Airport_", "Greenway_", "Green_",
        "Lake_Pier", "Lake_Walk", "Lake_Trunks", "Trees_"
    };
    static readonly string[] NoCollider = {
        "Markings", "Crowns", "Water_", "Signs_", "TrafficLights_Heads", "TrafficLights_Lamps",
        "Windows", "Doors", "Glass", "Sign_Text", "Hall_Band", "Label_", "Guide_", "Swatch_"
    };
    static bool NeedsCollider(string n)
    {
        foreach (var s in NoCollider) if (n.Contains(s)) return false;
        foreach (var p in ColliderPrefixes) if (n.StartsWith(p)) return true;
        return false;
    }
    static bool IsOccluder(string n) =>
        n.StartsWith("Bldg_") || n.StartsWith("Terrain_") || n == "Factory_Hall" || n.StartsWith("Port_Warehouse") || n.StartsWith("Airport_Terminal") || n.StartsWith("Airport_Hangar");

    void OnPostprocessModel(GameObject root)
    {
        if (!assetPath.EndsWith(CityFile)) return;
        int colliders = 0;
        foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
        {
            var go = mf.gameObject;
            string n = go.name;
            if (NeedsCollider(n) && go.GetComponent<Collider>() == null)
            {
                var mc = go.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                colliders++;
            }
            var flags = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic;
            if (IsOccluder(n)) flags |= StaticEditorFlags.OccluderStatic;
            GameObjectUtility.SetStaticEditorFlags(go, flags);
        }
        Debug.Log($"[SuperAssemblyCar] {CityFile}: added {colliders} MeshColliders.");
    }
}
