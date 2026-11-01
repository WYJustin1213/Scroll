using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class LaneManager : MonoBehaviour
{
    [Header("Tiles")]
    [Tooltip("Filled automatically from child LaneTile objects, ordered by local Z. Index 0 is the tile closest to the player.")]
    [SerializeField] private List<LaneTile> tiles = new List<LaneTile>();

    [Header("Tile Generation (context menu: Generate Tiles)")]
    [SerializeField, Min(1)] private int tileCount = 10;
    [SerializeField] private Vector2 tileSize = new Vector2(2f, 3f);
    [SerializeField, Min(0f)] private float tileGap = 0.05f;
    [SerializeField, Min(0.001f)] private float tileThickness = 0.05f;
    [SerializeField] private Material materialA;
    [SerializeField] private Material materialB;
    [SerializeField] private bool checkerboard = true;
    [Tooltip("Used for the checkerboard pattern across lanes.")]
    [SerializeField, Min(0)] private int laneIndex;

    public int LaneIndex => laneIndex;
    public int TileCount => tiles.Count;
    public IReadOnlyList<LaneTile> Tiles => tiles;

    private void OnEnable()
    {
        CollectTiles();
    }

    private void OnValidate()
    {
        CollectTiles();
    }

    public LaneTile GetTile(int index)
    {
        return index >= 0 && index < tiles.Count ? tiles[index] : null;
    }

    // Tiles are ordered from the player outward, so the next tile for an advancing enemy is index - 1.
    public LaneTile GetNextTile(LaneTile current)
    {
        return current == null ? null : GetTile(current.Index - 1);
    }

    public LaneTile GetNextTile(int currentIndex)
    {
        return GetTile(currentIndex - 1);
    }

    [ContextMenu("Collect Tiles")]
    public void CollectTiles()
    {
        tiles.Clear();
        for (int i = 0; i < transform.childCount; i++)
        {
            LaneTile tile = transform.GetChild(i).GetComponent<LaneTile>();
            if (tile != null)
                tiles.Add(tile);
        }

        tiles.Sort((a, b) => a.transform.localPosition.z.CompareTo(b.transform.localPosition.z));
        for (int i = 0; i < tiles.Count; i++)
        {
            if (tiles[i].Lane != this || tiles[i].Index != i)
                tiles[i].Initialize(this, i);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Generate Tiles")]
    public void GenerateTiles()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (transform.GetChild(i).GetComponent<LaneTile>() != null)
                UnityEditor.Undo.DestroyObjectImmediate(transform.GetChild(i).gameObject);
        }

        for (int i = 0; i < tileCount; i++)
        {
            GameObject tileObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEditor.Undo.RegisterCreatedObjectUndo(tileObject, "Generate Lane Tiles");
            tileObject.name = "Tile " + (i + 1);
            tileObject.transform.SetParent(transform, false);
            tileObject.transform.localPosition = new Vector3(0f, -tileThickness * 0.5f, (i + 0.5f) * tileSize.y);
            tileObject.transform.localScale = new Vector3(
                Mathf.Max(0.01f, tileSize.x - tileGap),
                tileThickness,
                Mathf.Max(0.01f, tileSize.y - tileGap));

            bool useB = checkerboard && (laneIndex + i) % 2 == 1;
            Material material = useB ? materialB : materialA;
            if (material != null)
                tileObject.GetComponent<MeshRenderer>().sharedMaterial = material;

            tileObject.AddComponent<LaneTile>();
        }

        CollectTiles();
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
#endif
}
