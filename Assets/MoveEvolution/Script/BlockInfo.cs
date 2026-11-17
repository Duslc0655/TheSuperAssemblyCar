using UnityEngine;

public class BlockInfo : MonoBehaviour
{
    public PartDef def;

    [System.NonSerialized] public Vector3Int cell;
    [System.NonSerialized] public bool broken;
    [System.NonSerialized] public VehicleChunk chunk;
}