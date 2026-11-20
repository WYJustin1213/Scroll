using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class LaneSpellController : MonoBehaviour
{
    public static event Action<LaneSpellController> SpellCast;

    [Header("Input")]
    [SerializeField] private Key castKey = Key.F;

    [Header("References")]
    [SerializeField] private LanePlayerMarker player;
    [SerializeField] private PlayerResources playerResources;
    [SerializeField] private Transform spellOrigin;
    [SerializeField] private SpellDefinition defaultSpell;
    [SerializeField] private Manager uiManager;
    [SerializeField, Tooltip("Optional UI display for successfully cast spell icons.")]
    private SpellIconDisplay iconDisplay;

    [Header("Casting")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0.65f, 0.55f);

    private float nextCastTime;

    public SentencePopup SentencePopup { get; set; }
    public PlayerResources Resources => playerResources;

    private AudioSource audio;

    private void Awake()
    {
        if (player == null)
            player = FindFirstObjectByType<LanePlayerMarker>();

        if (playerResources == null && player != null)
            playerResources = player.GetComponent<PlayerResources>();

        if (spellOrigin == null)
            spellOrigin = player != null ? player.transform : transform;

        audio = GetComponent<AudioSource>();

    }

    private void Update()
    {
        GameObject selected = EventSystem.current != null
            ? EventSystem.current.currentSelectedGameObject
            : null;
        TMP_InputField inputField = selected != null ? selected.GetComponent<TMP_InputField>() : null;
        if (inputField != null && inputField.isFocused)
            return;

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
        return CastSpellInternal(spell, point, 0);
    }

    internal bool CastSpellWithTemporaryMana(SpellDefinition spell, LaneSpellPoint point, int temporaryMana)
    {
        return CastSpellInternal(spell, point, temporaryMana);
    }

    private bool CastSpellInternal(SpellDefinition spell, LaneSpellPoint point, int temporaryMana)
    {
        if (spell == null)
        {
            Debug.LogError("LaneSpellController needs a SpellDefinition.", this);
            return false;
        }

        if (!spell.Directional && spell.Function != SpellFunction.Effect)
            return TryCast(spell, null, 0, Vector3.zero, temporaryMana);

        if (point == null ||
            point.Lane == null ||
            (spell.Directional && point.Lane.GetTile(0) == null))
        {
            Debug.LogError("LaneSpellController needs a lane spell point with a valid lane.", this);
            return false;
        }

        return TryCast(
            spell,
            point.Lane,
            0,
            spell.Directional ? point.SpellOrigin.position : Vector3.zero,
            temporaryMana
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
        Vector3 spawnPosition,
        int temporaryMana = 0)
    {
        if (SentencePopup != null && SentencePopup.IsVisible)
            return false;

        if (spell == null)
        {
            Debug.LogError("LaneSpellController needs a SpellDefinition.", this);
            return false;
        }

        if (Time.time < nextCastTime)
            return false;

        if ((spell.Directional || spell.Function == SpellFunction.Effect) && lane == null)
        {
            Debug.LogError("Spell target lane is not available.", this);
            audio.Play();
            return false;
        }

        if (spell.Directional && lane.GetTile(firstTileIndex) == null)
        {
            Debug.LogError("Spell target lane does not have a valid starting tile.", this);
            audio.Play();
            return false;
        }

        if (playerResources == null)
        {
            Debug.LogError("LaneSpellController needs the player's PlayerResources component.", this);
            audio.Play();
            return false;
        }

        if (playerResources.Mana < spell.ManaCost)
        {
            Debug.LogWarning("Not enough mana to cast \"" + spell.name + "\".", this);
            audio.Play();
            return false;          
        }

        if (spell.Function == SpellFunction.Effect)
        {
            if (temporaryMana < 0 || temporaryMana > playerResources.Mana)
            {
                Debug.LogError("Temporary spell mana is invalid.", this);
                return false;
            }

            // Effect spells do not spend mana, so remove the credit before their mana gain.
            playerResources.SetMana(playerResources.Mana - temporaryMana);
            if (!CastSap(spell, lane))
            {
                playerResources.SetMana((int)Math.Min(int.MaxValue, (long)playerResources.Mana + temporaryMana));
                return false;
            }

            NotifySpellCast(spell);
            return true;
        }

        if (!spell.Directional)
        {
            Debug.LogError("Projectile spell \"" + spell.name + "\" must be directional.", spell);
            return false;
        }

        if (spell.Function == SpellFunction.Beam)
        {
            if (spell.shockBeamPrefab == null)
            {
                Debug.LogError("SpellDefinition \"" + spell.name + "\" has no shock beam prefab.", spell);
                return false;
            }

            LaneShockBeam beam = Instantiate(
                spell.shockBeamPrefab,
                spawnPosition,
                Quaternion.identity
            );

            if (!beam.Initialize(
                lane, firstTileIndex, spell.spellElement,
                spell.ShockTrailPositionOffset, spell.ShockTrailRotation))
                return false;
        }
        else
        {
            if (spell.sparkProjectilePrefab == null)
            {
                Debug.LogError("SpellDefinition \"" + spell.name + "\" has no projectile prefab.", spell);
                return false;
            }

            LaneFireballProjectile projectile = Instantiate(
                spell.sparkProjectilePrefab,
                spawnPosition,
                Quaternion.identity
            );

            if (!projectile.Initialize(lane, firstTileIndex, spell.spellElement))
                return false;
        }

        playerResources.SetMana(playerResources.Mana - spell.ManaCost);
        nextCastTime = Time.time;
        /*
        if (uiManager != null)
            uiManager.HideUI();
        */

        NotifySpellCast(spell);
        return true;
    }

    private void NotifySpellCast(SpellDefinition spell)
    {
        if (iconDisplay != null)
            iconDisplay.Show(spell);

        SpellCast?.Invoke(this);
    }

    private bool CastSap(SpellDefinition spell, LaneManager lane)
    {
        GameObject targetPrefab = spell.TargetPrefab;
        HashSet<GameObject> targetsToDestroy = new HashSet<GameObject>();
        SapTarget[] targets = FindObjectsByType<SapTarget>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );
        for (int i = 0; i < targets.Length; i++)
        {
            SapTarget target = targets[i];
            if (targetPrefab != null &&
                target != null &&
                target.SourcePrefab == targetPrefab &&
                (!target.CompareTag("DropMana") || IsInLane(target.transform, lane)) &&
                target.TryConsume())
                targetsToDestroy.Add(target.gameObject);
        }

        Transform[] sceneObjects = FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );
        for (int i = 0; i < sceneObjects.Length; i++)
        {
            GameObject target = sceneObjects[i].gameObject;
            if (target.CompareTag("DropMana") && IsInLane(target.transform, lane))
                targetsToDestroy.Add(target);
        }

        foreach (GameObject target in targetsToDestroy)
            Destroy(target);

        playerResources.SetMana((int)Math.Min(int.MaxValue, (long)playerResources.Mana + 1 + targetsToDestroy.Count));
        nextCastTime = Time.time;
        /*
        if (uiManager != null)
            uiManager.HideUI();
        */

        return true;
    }

    private static bool IsInLane(Transform target, LaneManager lane)
    {
        return target != null && lane != null && target.IsChildOf(lane.transform);
    }
}
