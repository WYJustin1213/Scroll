using UnityEngine;

[DisallowMultipleComponent]
public class SpellHitEffect : MonoBehaviour
{
    [SerializeField, Min(0f)] private float lifetime = 0.15f;

    private void OnEnable()
    {
        if (Application.isPlaying)
            Destroy(gameObject, lifetime);
    }
}
