using UnityEngine;

[ExecuteAlways]
[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public class LanePrototypeController : MonoBehaviour
{
    [Header("Scene Setup")]
    [SerializeField] private LaneGridSystem grid;
    [SerializeField] private Camera gameCamera;

    [Header("Camera Framing")]
    [Tooltip("These values are applied continuously, so they can be adjusted while the game is running.")]
    [SerializeField] private Vector3 cameraPosition = new Vector3(0f, 5f, -8f);
    [SerializeField] private Vector3 cameraLookAt = new Vector3(0f, 0f, 14f);
    [SerializeField, Range(25f, 90f)] private float fieldOfView = 55f;

    [Header("Stationary Player")]
    [SerializeField, Min(0)] private int playerLane = 2;
    [SerializeField, Min(0f)] private float playerDepth = 1.5f;

    private void OnEnable()
    {
        if (grid == null)
            grid = GetComponent<LaneGridSystem>();

        if (grid == null)
        {
            Debug.LogError("Lane prototype requires a LaneGridSystem component.", this);
            enabled = false;
            return;
        }

        ApplyCameraFraming();
        bool sceneChanged = EnsurePlayer();
        sceneChanged |= BindEnemiesToPlayer();

#if UNITY_EDITOR
        if (sceneChanged && !Application.isPlaying)
        {
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
    }

    private void OnValidate()
    {
        if (isActiveAndEnabled && grid == null)
            grid = GetComponent<LaneGridSystem>();

        if (isActiveAndEnabled && grid != null)
            ApplyCameraFraming();
    }

    private void LateUpdate()
    {
        if (Application.isPlaying)
            ApplyCameraFraming();
    }

    private void ApplyCameraFraming()
    {
        if (gameCamera == null)
            gameCamera = Camera.main;

        if (gameCamera == null)
        {
            if (Application.isPlaying)
                Debug.LogWarning("Lane prototype could not find a camera to frame the lanes.", this);
            return;
        }

        if (gameCamera.transform.parent != null)
            gameCamera.transform.SetParent(null, true);

        Vector3 lookDirection = cameraLookAt - cameraPosition;
        if (lookDirection.sqrMagnitude < 0.0001f)
        {
            Debug.LogWarning("Camera position and look-at target must be different.", this);
            return;
        }

        gameCamera.transform.SetPositionAndRotation(
            cameraPosition,
            Quaternion.LookRotation(lookDirection));
        gameCamera.fieldOfView = fieldOfView;
        gameCamera.orthographic = false;
        gameCamera.clearFlags = CameraClearFlags.SolidColor;
        gameCamera.backgroundColor = new Color(0.11f, 0.11f, 0.11f);
    }

    private bool EnsurePlayer()
    {
        Transform existingPlayer = transform.Find("Stationary Player");
        if (existingPlayer != null)
            return false;

        GameObject player = new GameObject("Stationary Player");
        player.transform.SetParent(transform, false);
        LanePlayerMarker marker = player.AddComponent<LanePlayerMarker>();
        marker.Initialize(grid, playerLane, playerDepth);
        return true;
    }

    private bool BindEnemiesToPlayer()
    {
        Transform playerObject = transform.Find("Stationary Player");
        LanePlayerMarker playerMarker = playerObject != null
            ? playerObject.GetComponent<LanePlayerMarker>()
            : null;

        if (playerMarker == null)
            Debug.LogError("Lane prototype requires a Stationary Player with a LanePlayerMarker.", this);

        Transform existingEnemies = transform.Find("Test Enemies");
        if (existingEnemies == null)
        {
            existingEnemies = transform.Find("Placeholder Enemies");
            if (existingEnemies != null)
                existingEnemies.name = "Test Enemies";
        }

        if (existingEnemies == null)
        {
            Debug.LogError("Lane prototype requires a Test Enemies group in the scene.", this);
            return false;
        }

        if (playerMarker == null)
            return false;

        bool targetsChanged = false;
        LaneTestEnemy[] testEnemies = existingEnemies.GetComponentsInChildren<LaneTestEnemy>(true);
        for (int i = 0; i < testEnemies.Length; i++)
            targetsChanged |= testEnemies[i].SetTarget(playerMarker.transform);

        return targetsChanged;
    }
}
