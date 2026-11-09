using UnityEngine;

[DisallowMultipleComponent]
public class SapTarget : MonoBehaviour
{
    [SerializeField, Tooltip("Set this to the prefab asset that owns this marker.")]
    private GameObject sourcePrefab;

    private bool consumed;

    public GameObject SourcePrefab => sourcePrefab;

    public bool TryConsume()
    {
        if (consumed)
            return false;

        consumed = true;
        return true;
    }
}
