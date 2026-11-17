using System.Collections.Generic;
using UnityEngine;

public class VehicleChunk : MonoBehaviour
{
    [System.NonSerialized] public VehicleDriver driver;
    [System.NonSerialized] public Rigidbody rb;
    [System.NonSerialized] public List<BlockInfo> blocks = new List<BlockInfo>();

    // 依照目前還在這一塊裡的方塊，重新算總重量和重心
    public void RefreshMass()
    {
        float m = 0f;
        Vector3 sum = Vector3.zero;
        foreach (var b in blocks)
        {
            m += b.def.mass;
            sum += b.transform.position * b.def.mass;
        }
        if (m <= 0f || rb == null) return;
        rb.mass = m;
        rb.centerOfMass = transform.InverseTransformPoint(sum / m);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (driver != null) driver.ReportImpact(collision);
    }
}