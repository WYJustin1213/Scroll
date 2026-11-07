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
    [SerializeField] private SpellDefinition defaultSpell;
    [SerializeField] private Manager uiManager;

    [Header("Casting")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0.65f, 0.55f);

    private float nextCastTime;

    private void Awake()
    {
        if (player == null)
            player = FindFirstObjectByType<LanePlayerMarker>();

        if (spellOrigin == null)
            spellOrigin = player != null ? player.transform : transform;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null &&
            castKey != Key.None &&
            keyboard[castKey].wasPressedThisFrame)
        {
            CastDefaultSpell();
        }
    }

    public bool CastFireball()
    {
        return CastDefaultSpell();
    }

    public bool CastFireball(LaneSpellPoint point)
    {
        return CastSpell(defaultSpell, point);
    }

    public bool CastSpell(SpellDefinition spell, LaneSpellPoint point)
    {
        if (point == null || point.Lane == null || point.Lane.GetTile(0) == null)
        {
            Debug.LogError("LaneSpellController needs a lane spell point with a valid lane.", this);
            return false;
        }

        return TryCast(
            spell,
            point.Lane,
            0,
            point.SpellOrigin.position
        );
    }

    private bool CastDefaultSpell()
    {
        if (player == null)
        {
            Debug.LogError("LaneSpellController needs a LanePlayerMarker.", this);
            return false;
        }

        if (player.Lane == null || player.Lane.GetTile(player.TileIndex) == null)
        {
            Debug.LogError("LaneSpellController's player lane or tile is not available.", this);
            return false;
        }

        if (spellOrigin == null)
        {
            Debug.LogError("LaneSpellController needs a spell origin.", this);
            return false;
        }

        return TryCast(
            defaultSpell,
            player.Lane,
            player.TileIndex + 1,
            spellOrigin.TransformPoint(spawnOffset)
        );
    }

    private bool TryCast(
        SpellDefinition spell,
        LaneManager lane,
        int firstTileIndex,
        Vector3 spawnPosition)
    {
        if (spell == null)
        {
            Debug.LogError("LaneSpellController needs a SpellDefinition.", this);
            return false;
        }

        if (Time.time < nextCastTime)
            return false;

        if (lane == null || lane.GetTile(firstTileIndex) == null)
        {
            Debug.LogError("Spell target lane does not have a valid starting tile.", this);
            return false;
        }

        if (spell.ProjectilePrefab == null)
        {
            Debug.LogError("SpellDefinition \"" + spell.name + "\" has no projectile prefab.", spell);
            return false;
        }

        LaneFireballProjectile projectile = Instantiate(
            spell.ProjectilePrefab,
            spawnPosition,
            Quaternion.identity
        );

        if (!projectile.Initialize(lane, firstTileIndex, spell.spellElement))
            return false;

        nextCastTime = Time.time + spell.Cooldown;
        if (uiManager != null)
            uiManager.HideUI();

        return true;
    }
}
