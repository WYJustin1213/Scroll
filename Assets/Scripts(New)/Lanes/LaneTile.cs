using UnityEngine;

[DisallowMultipleComponent]
public class LaneTile : MonoBehaviour
{
    [SerializeField] private LaneManager lane;
    [SerializeField, Min(0)] private int index;

    public LaneManager Lane => lane;
    public int Index => index;

    // Top surface center of the tile, where units stand.
    public Vector3 StandPosition => transform.TransformPoint(new Vector3(0f, 0.5f, 0f));

    public void Initialize(LaneManager lane, int index)
    {
        this.lane = lane;
        this.index = index;
    }
}
