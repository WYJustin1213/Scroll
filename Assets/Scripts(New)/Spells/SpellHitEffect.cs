using UnityEngine;

[DisallowMultipleComponent]
public class SpellHitEffect : MonoBehaviour
{
    [SerializeField, Min(0f)] private float lifetime = 0.15f;
    [SerializeField] private AudioClip hitSound;

    private void OnEnable()
    {
        if (Application.isPlaying)
        {
            gameObject.GetComponent<AudioSource>().PlayOneShot(hitSound, 1);
            Destroy(gameObject, lifetime);
        }
    }
}
