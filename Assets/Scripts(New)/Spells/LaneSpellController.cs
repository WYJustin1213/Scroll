using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class LaneSpellController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private Key castKey = Key.F;

    [Header("References")]
    [SerializeField] private LanePlayerMarker player;
    [SerializeField] private Transform spellOrigin;
    [SerializeField] private LaneFireballProjectile fireballPrefab;

    [Header("Casting")]
    [SerializeField, Min(0f)] private float cooldown = 0.5f;
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0.65f, 0.55f);

    private float nextCastTime;

    private void Awake()
    {
        if (player == null)
            player = GetComponent<LanePlayerMarker>();

        if (spellOrigin == null)
            spellOrigin = transform;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null &&
            castKey != Key.None &&
            keyboard[castKey].wasPressedThisFrame)
        {
            CastFireball();
        }
    }

    public bool CastFireball()
    {
        if (Time.time < nextCastTime)
            return false;

        if (player == null)
        {
            Debug.LogError("LaneSpellController needs a LanePlayerMarker.", this);
            return false;
        }

        if (player.Lane == null)
        {
            Debug.LogError("LaneSpellController's player has no lane assigned.", this);
            return false;
        }

        if (player.Lane.GetTile(player.TileIndex) == null)
        {
            Debug.LogError("LaneSpellController's player tile is not available.", this);
            return false;
        }

        if (spellOrigin == null)
        {
            Debug.LogError("LaneSpellController needs a spell origin.", this);
            return false;
        }

        if (fireballPrefab == null)
        {
            Debug.LogError("LaneSpellController needs a fireball prefab.", this);
            return false;
        }

        Vector3 spawnPosition = spellOrigin.TransformPoint(spawnOffset);
        LaneFireballProjectile projectile = Instantiate(
            fireballPrefab,
            spawnPosition,
            Quaternion.identity
        );

        if (!projectile.Initialize(player.Lane, player.TileIndex))
            return false;

        nextCastTime = Time.time + cooldown;
        return true;
    }
}
