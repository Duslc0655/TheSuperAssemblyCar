using System.Collections;
using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// 開場鏡頭：城市上空環繞全景 → 飛到車廠正面停住。
// 車廠「正面」= 從廠房 (Factory_Hall) 往停車場 (Factory_Parking) 的方向，也就是招牌那一面。
public class CameraIntro : MonoBehaviour
{
    public Camera cam;

    [Header("全景")]
    [Tooltip("環繞全景的秒數")] public float panoramaSeconds = 5f;
    [Tooltip("全景期間繞城市轉幾度")] public float orbitDegrees = 70f;
    [Tooltip("高度 = 城市半徑 × 此值")] public float heightFactor = 0.8f;
    [Tooltip("水平距離 = 城市半徑 × 此值")] public float distanceFactor = 1.0f;

    [Header("飛到車廠正面")]
    public float flySeconds = 4f;
    public string factoryBuilding = "Factory_Hall";
    public string factoryFront = "Factory_Parking";
    [Tooltip("鏡頭離廠房正面多遠 (m)")] public float frontDistance = 30f;
    [Tooltip("鏡頭高度 (m)")] public float frontHeight = 9f;
    [Tooltip("看向廠房離地多高 (m)")] public float lookHeight = 5f;
    [Tooltip("門口按鈕放在廠房正面離地多高 (m)")] public float doorHeight = 3f;

    [Header("其他")]
    [Tooltip("播放中按任意鍵或點滑鼠可跳過")] public bool allowSkip = true;
    public UnityEvent onFinished;

    public bool IsPlaying { get; private set; }

    Vector3 center;
    float radius = 250f;
    bool skip;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) { Debug.LogWarning("[CameraIntro] 找不到攝影機。"); enabled = false; return; }
        cam.farClipPlane = Mathf.Max(cam.farClipPlane, 3000f);
        ComputeCityBounds();
        // 開始畫面期間，鏡頭先停在全景起點（被灰布蓋住）
        float endDeg, startDeg;
        GetOrbitRange(out startDeg, out endDeg);
        cam.transform.position = OrbitPos(startDeg);
        cam.transform.LookAt(center);
    }

    void Update()
    {
        if (!IsPlaying || !allowSkip) return;
#if ENABLE_INPUT_SYSTEM
        bool pressed = (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
                    || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
#else
        bool pressed = Input.anyKeyDown;
#endif
        if (pressed) skip = true;
    }

    public void Play()
    {
        if (!enabled || IsPlaying) return;
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        IsPlaying = true;
        skip = false;

        Vector3 frontPos, frontLook;
        bool hasFactory = FactoryPose(out frontPos, out frontLook);
        float startDeg, endDeg;
        GetOrbitRange(out startDeg, out endDeg);

        // 1) 全景環繞
        float t = 0f;
        while (t < panoramaSeconds && !skip)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / panoramaSeconds);
            cam.transform.position = OrbitPos(Mathf.Lerp(startDeg, endDeg, k));
            cam.transform.LookAt(center);
            yield return null;
        }

        // 2) 飛到車廠正面
        if (hasFactory)
        {
            Vector3 p0 = cam.transform.position;
            Vector3 mid = Vector3.Lerp(p0, frontPos, 0.5f) + Vector3.up * radius * 0.1f;
            t = 0f;
            while (t < flySeconds && !skip)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / flySeconds));
                Vector3 a = Vector3.Lerp(p0, mid, k);
                Vector3 b = Vector3.Lerp(mid, frontPos, k);
                cam.transform.position = Vector3.Lerp(a, b, k);
                cam.transform.LookAt(Vector3.Lerp(center, frontLook, k));
                yield return null;
            }
            cam.transform.position = frontPos;
            cam.transform.LookAt(frontLook);
        }
        else
        {
            Debug.LogWarning($"[CameraIntro] 場景裡找不到 {factoryBuilding}，鏡頭停在全景。");
        }

        IsPlaying = false;
        if (onFinished != null) onFinished.Invoke();
    }

    // 全景結束時，鏡頭剛好在車廠正面那一側，接著飛下去比較順
    void GetOrbitRange(out float startDeg, out float endDeg)
    {
        Vector3 fp, fl;
        endDeg = 0f;
        if (FactoryPose(out fp, out fl))
            endDeg = Mathf.Atan2(fp.x - center.x, fp.z - center.z) * Mathf.Rad2Deg;
        startDeg = endDeg - orbitDegrees;
    }

    Vector3 OrbitPos(float deg)
    {
        float r = deg * Mathf.Deg2Rad;
        Vector3 dir = new Vector3(Mathf.Sin(r), 0f, Mathf.Cos(r));
        return center + dir * radius * distanceFactor + Vector3.up * radius * heightFactor;
    }

    void ComputeCityBounds()
    {
        bool any = false;
        Bounds b = new Bounds();
        var renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        foreach (var r in renderers)
        {
            if (!r.name.StartsWith("Terrain_City")) continue;
            if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
        }
        if (!any)
            foreach (var r in renderers)
            {
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
        if (!any) return;
        center = b.center;
        radius = Mathf.Max(b.extents.x, b.extents.z);
    }

    bool FactoryPose(out Vector3 pos, out Vector3 look)
    {
        pos = look = Vector3.zero;
        Renderer hall = FindRenderer(factoryBuilding);
        if (hall == null) return false;
        Renderer front = FindRenderer(factoryFront);
        Bounds hb = hall.bounds;

        Vector3 dir = front != null ? front.bounds.center - hb.center : hb.center - center;
        dir.y = 0f;
        if (dir.sqrMagnitude < 1e-4f) dir = Vector3.forward;
        dir.Normalize();

        float reach = Mathf.Abs(dir.x) * hb.extents.x + Mathf.Abs(dir.z) * hb.extents.z;
        look = new Vector3(hb.center.x, hb.min.y + lookHeight, hb.center.z);
        pos = new Vector3(hb.center.x, hb.min.y, hb.center.z) + dir * (reach + frontDistance) + Vector3.up * frontHeight;
        return true;
    }

    // 車廠正面（招牌、停車場那一面）牆面中央、離地 doorHeight 的位置
    public bool TryGetFactoryDoor(out Vector3 door)
    {
        door = Vector3.zero;
        Renderer hall = FindRenderer(factoryBuilding);
        if (hall == null) return false;
        Renderer front = FindRenderer(factoryFront);
        Bounds hb = hall.bounds;
        Vector3 dir = front != null ? front.bounds.center - hb.center : hb.center - center;
        dir.y = 0f;
        if (dir.sqrMagnitude < 1e-4f) dir = Vector3.forward;
        dir.Normalize();
        float reach = Mathf.Abs(dir.x) * hb.extents.x + Mathf.Abs(dir.z) * hb.extents.z;
        door = new Vector3(hb.center.x, hb.min.y + doorHeight, hb.center.z) + dir * reach;
        return true;
    }

    static Renderer FindRenderer(string objectName)
    {
        foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            if (r.name == objectName) return r;
        return null;
    }
}
