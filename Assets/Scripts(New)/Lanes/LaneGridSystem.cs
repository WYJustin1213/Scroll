using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class LaneGridSystem : MonoBehaviour
{
    [Header("Lane Layout")]
    [SerializeField, Min(1)] private int laneCount = 5;
    [SerializeField, Min(0.1f)] private float laneWidth = 2f;
    [SerializeField, Min(0f)] private float nearDepth = 0f;
    [SerializeField, Min(1f)] private float farDepth = 32f;
    [SerializeField, Min(1)] private int horizontalLineCount = 12;

    [Header("Grid Appearance")]
    [SerializeField] private bool alternateLaneColors = true;
    [SerializeField] private Color laneColorA = new Color(0.28f, 0.28f, 0.28f);
    [SerializeField] private Color laneColorB = new Color(0.34f, 0.34f, 0.34f);
    [SerializeField] private Color lineColor = new Color(0.78f, 0.78f, 0.78f, 0.7f);
    [SerializeField, Min(0.001f)] private float lineWidth = 0.025f;
    [SerializeField, Min(0f)] private float lineHeight = 0.025f;

    private Mesh gridMesh;
    private Material lineMaterial;
    private readonly List<Material> laneMaterials = new List<Material>();
    private bool isBuildingGrid;
#if UNITY_EDITOR
    private bool gridRebuildScheduled;
#endif

    public int LaneCount => Mathf.Max(1, laneCount);
    public float LaneWidth => Mathf.Max(0.1f, laneWidth);
    public float NearDepth => nearDepth;
    public float FarDepth => Mathf.Max(nearDepth + 1f, farDepth);
    public float TotalWidth => LaneCount * LaneWidth;

    private void OnValidate()
    {
        if (Application.isPlaying || !isActiveAndEnabled || isBuildingGrid)
            return;

#if UNITY_EDITOR
        if (gridRebuildScheduled)
            return;

        gridRebuildScheduled = true;
        UnityEditor.EditorApplication.update += RebuildGridAfterValidation;
#endif
    }

#if UNITY_EDITOR
    private void RebuildGridAfterValidation()
    {
        UnityEditor.EditorApplication.update -= RebuildGridAfterValidation;
        gridRebuildScheduled = false;
        if (this != null && isActiveAndEnabled && !Application.isPlaying)
            BuildGrid();
    }
#endif

    private void OnEnable()
    {
        if (transform.Find("Lane Grid Visuals") == null)
            BuildGrid();
    }

    [ContextMenu("Rebuild Grid")]
    public void BuildGrid()
    {
        if (transform == null || isBuildingGrid)
            return;

        isBuildingGrid = true;
        try
        {
            Transform visuals = transform.Find("Lane Grid Visuals");
            if (visuals != null)
                DestroyOwnedObject(visuals.gameObject);

            ReleaseGeneratedMaterials();
            GameObject visualsObject = new GameObject("Lane Grid Visuals");
            visualsObject.transform.SetParent(transform, false);

            CreateLanes(visualsObject.transform);
            CreateGridMesh(visualsObject.transform);
        }
        finally
        {
            isBuildingGrid = false;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
    }

    public float GetLaneCenterX(int laneIndex)
    {
        int safeLane = Mathf.Clamp(laneIndex, 0, LaneCount - 1);
        return (safeLane - (LaneCount - 1) * 0.5f) * LaneWidth;
    }

    public Vector3 GetLanePosition(int laneIndex, float depth, float height = 0f)
    {
        return transform.TransformPoint(new Vector3(
            GetLaneCenterX(laneIndex),
            height,
            Mathf.Clamp(depth, NearDepth, FarDepth)));
    }

    private void CreateLanes(Transform parent)
    {
        Transform lanesRoot = new GameObject("Lanes").transform;
        lanesRoot.SetParent(parent, false);

        for (int laneIndex = 0; laneIndex < LaneCount; laneIndex++)
        {
            GameObject lane = new GameObject("Lane " + (laneIndex + 1));
            lane.transform.SetParent(lanesRoot, false);
            lane.transform.localPosition = new Vector3(GetLaneCenterX(laneIndex), 0f, 0f);

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Lane Surface";
            floor.transform.SetParent(lane.transform, false);
            floor.transform.localPosition = new Vector3(
                0f,
                -0.025f,
                (NearDepth + FarDepth) * 0.5f);
            floor.transform.localScale = new Vector3(
                LaneWidth,
                0.05f,
                FarDepth - NearDepth);
            DestroyOwnedObject(floor.GetComponent<Collider>());

            Color laneColor = alternateLaneColors && laneIndex % 2 == 1
                ? laneColorB
                : laneColorA;
            Material material = CreateMaterial(laneColor);
            if (material != null)
            {
                laneMaterials.Add(material);
                floor.GetComponent<MeshRenderer>().sharedMaterial = material;
            }
        }
    }

    private void CreateGridMesh(Transform parent)
    {
        GameObject gridObject = new GameObject("Grid Lines");
        gridObject.transform.SetParent(parent, false);

        MeshFilter meshFilter = gridObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = gridObject.AddComponent<MeshRenderer>();
        gridMesh = new Mesh { name = "Lane Grid Mesh" };
        gridMesh.vertices = BuildVertices();
        gridMesh.triangles = BuildTriangles();
        gridMesh.RecalculateNormals();
        gridMesh.RecalculateBounds();

        meshFilter.sharedMesh = gridMesh;
        lineMaterial = CreateMaterial(lineColor);
        meshRenderer.sharedMaterial = lineMaterial;
    }

    private void ReleaseGeneratedMaterials()
    {
        for (int i = 0; i < laneMaterials.Count; i++)
            DestroyOwnedObject(laneMaterials[i]);

        laneMaterials.Clear();
        DestroyOwnedObject(lineMaterial);
        lineMaterial = null;
    }

    private Vector3[] BuildVertices()
    {
        int verticalLineCount = LaneCount + 1;
        int horizontalCount = Mathf.Max(1, horizontalLineCount) + 1;
        var vertices = new List<Vector3>((verticalLineCount + horizontalCount) * 4);
        float halfLineWidth = Mathf.Max(0.001f, lineWidth) * 0.5f;
        float leftEdge = -TotalWidth * 0.5f;

        for (int line = 0; line < verticalLineCount; line++)
        {
            float x = leftEdge + line * LaneWidth;
            AddQuad(vertices,
                new Vector3(x - halfLineWidth, lineHeight, NearDepth),
                new Vector3(x + halfLineWidth, lineHeight, NearDepth),
                new Vector3(x + halfLineWidth, lineHeight, FarDepth),
                new Vector3(x - halfLineWidth, lineHeight, FarDepth));
        }

        for (int line = 0; line < horizontalCount; line++)
        {
            float fraction = line / (float)(horizontalCount - 1);
            float z = Mathf.Lerp(NearDepth, FarDepth, fraction);
            AddQuad(vertices,
                new Vector3(leftEdge, lineHeight, z - halfLineWidth),
                new Vector3(leftEdge, lineHeight, z + halfLineWidth),
                new Vector3(-leftEdge, lineHeight, z + halfLineWidth),
                new Vector3(-leftEdge, lineHeight, z - halfLineWidth));
        }

        return vertices.ToArray();
    }

    private static void AddQuad(List<Vector3> vertices, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        vertices.Add(d);
    }

    private int[] BuildTriangles()
    {
        int quadCount = LaneCount + 1 + Mathf.Max(1, horizontalLineCount) + 1;
        var triangles = new int[quadCount * 6];
        for (int quad = 0; quad < quadCount; quad++)
        {
            int vertex = quad * 4;
            int triangle = quad * 6;

            if (quad <= LaneCount)
            {
                triangles[triangle] = vertex;
                triangles[triangle + 1] = vertex + 2;
                triangles[triangle + 2] = vertex + 1;
                triangles[triangle + 3] = vertex;
                triangles[triangle + 4] = vertex + 3;
                triangles[triangle + 5] = vertex + 2;
            }
            else
            {
                triangles[triangle] = vertex;
                triangles[triangle + 1] = vertex + 1;
                triangles[triangle + 2] = vertex + 2;
                triangles[triangle + 3] = vertex;
                triangles[triangle + 4] = vertex + 2;
                triangles[triangle + 5] = vertex + 3;
            }
        }

        return triangles;
    }

    private static Material CreateMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        if (shader == null)
        {
            Debug.LogError("Lane grid could not find a supported unlit shader.");
            return null;
        }

        var material = new Material(shader);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        else
            material.color = color;

        return material;
    }

    private void OnDestroy()
    {
        if (!Application.isPlaying)
            return;

        DestroyOwnedObject(gridMesh);
        ReleaseGeneratedMaterials();
    }

    private static void DestroyOwnedObject(Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }
}
