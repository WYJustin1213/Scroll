using UnityEngine;

[ExecuteAlways]
[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public class LanePrototypeController : MonoBehaviour
{
    [Header("Scene Setup")]
    [SerializeField] private Camera gameCamera;

    [Header("Camera Framing")]
    [Tooltip("These values are applied continuously, so they can be adjusted while the game is running.")]
    [SerializeField] private Vector3 cameraPosition = new Vector3(0f, 5f, -8f);
    [SerializeField] private Vector3 cameraLookAt = new Vector3(0f, 0f, 14f);
    [SerializeField, Range(25f, 90f)] private float fieldOfView = 55f;

    private void OnEnable()
    {
        ApplyCameraFraming();
    }

    private void OnValidate()
    {
        if (isActiveAndEnabled)
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
}
