using UnityEngine;

/// <summary>把底下的子物件當成路徑點（依順序），NPC 沿著它行駛。</summary>
public class LanePath : MonoBehaviour
{
    public bool loop = true;
    public Color gizmoColor = Color.yellow;

    public int Count => transform.childCount;

    public Vector3 GetPoint(int i)
    {
        int n = Count;
        if (n == 0) return transform.position;
        if (loop) i = ((i % n) + n) % n;
        else i = Mathf.Clamp(i, 0, n - 1);
        return transform.GetChild(i).position;
    }

    public Vector3 PointWithOffset(int i, float offset)
    {
        Vector3 d = GetPoint(i + 1) - GetPoint(i - 1);
        d.y = 0;
        if (d.sqrMagnitude < 0.0001f) return GetPoint(i);
        Vector3 right = Vector3.Cross(Vector3.up, d.normalized);
        return GetPoint(i) + right * offset;
    }

    public int FindNearestIndex(Vector3 p)
    {
        int best = 0; float bd = float.MaxValue;
        for (int i = 0; i < Count; i++)
        {
            float d = (GetPoint(i) - p).sqrMagnitude;
            if (d < bd) { bd = d; best = i; }
        }
        return best;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        for (int i = 0; i < Count; i++)
        {
            Gizmos.DrawSphere(GetPoint(i), 0.5f);
            if (loop || i < Count - 1) Gizmos.DrawLine(GetPoint(i), GetPoint(i + 1));
        }
    }
}
