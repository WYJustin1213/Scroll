using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class LanePlayerMarker : MonoBehaviour
{
    [SerializeField] private LaneManager lane;
    [SerializeField, Min(0)] private int tileIndex;

    public LaneManager Lane => lane;
    public int TileIndex => tileIndex;

    private void OnEnable()
    {
        RefreshPosition();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
            RefreshPosition();
    }

    private void RefreshPosition()
    {
        if (lane == null)
            return;

        lane.CollectTiles();
        LaneTile tile = lane.GetTile(tileIndex);
        if (tile != null)
            transform.position = tile.StandPosition;
    }
}
