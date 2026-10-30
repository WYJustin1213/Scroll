using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class LanePlayerMarker : MonoBehaviour
{
    [SerializeField] private Color placeholderColor = new Color(0.84f, 0.78f, 0.62f);
    [SerializeField] private LaneGridSystem grid;
    [SerializeField, Min(0)] private int lane = 2;
    [SerializeField, Min(0f)] private float depth = 1.5f;

    public int Lane => lane;
    public float Depth => depth;

    public void Initialize(LaneGridSystem grid, int lane, float depth)
    {
        if (grid == null)
        {
            Debug.LogError("Player placeholder requires a LaneGridSystem.", this);
            return;
        }

        this.grid = grid;
        this.lane = Mathf.Clamp(lane, 0, grid.LaneCount - 1);
        this.depth = Mathf.Clamp(depth, grid.NearDepth, grid.FarDepth);
        RefreshPosition();
        CreateVisual();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying && grid != null)
            RefreshPosition();
    }

    private void RefreshPosition()
    {
        transform.position = grid.GetLanePosition(lane, depth);
    }

    private void CreateVisual()
    {
        Transform existingVisual = transform.Find("Player Placeholder Visual");
        if (existingVisual != null)
        {
            existingVisual.gameObject.SetActive(true);
            return;
        }

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "Player Placeholder Visual";
        visual.transform.SetParent(transform, false);
        visual.transform.localPosition = Vector3.up;
        visual.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);
        RemoveCollider(visual.GetComponent<Collider>());

        Renderer renderer = visual.GetComponent<Renderer>();
        renderer.sharedMaterial = new Material(renderer.sharedMaterial)
        {
            color = placeholderColor
        };
    }

    private static void RemoveCollider(Collider collider)
    {
        if (collider == null)
            return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            DestroyImmediate(collider);
            return;
        }
#endif

        Destroy(collider);
    }
}
