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

    [Header("Casting")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0.65f, 0.55f);

    private float nextCastTime;

    private void Awake()
    {
        if (player == null)
            player = FindFirstObjectByType<LanePlayerMarker>();

        if (playerResources == null && player != null)
            playerResources = player.GetComponent<PlayerResources>();

        if (spellOrigin == null)
            spellOrigin = player != null ? player.transform : transform;
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
        if (spell == null)
        {
            Debug.LogError("LaneSpellController needs a SpellDefinition.", this);
            return false;
        }

        if (!spell.Directional)
            return TryCast(spell, null, 0, Vector3.zero);

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

        if (spell.Directional && (lane == null || lane.GetTile(firstTileIndex) == null))
        {
            Debug.LogError("Spell target lane does not have a valid starting tile.", this);
            return false;
        }

        if (playerResources == null)
        {
            Debug.LogError("LaneSpellController needs the player's PlayerResources component.", this);
            return false;
        }

        if (playerResources.Mana < spell.ManaCost)
        {
            Debug.LogWarning("Not enough mana to cast \"" + spell.name + "\".", this);
            return false;
        }

        if (spell.Function == SpellFunction.Effect)
        {
            if (!CastSap(spell))
                return false;

            SpellCast?.Invoke(this);
            return true;
        }

        if (!spell.Directional)
        {
            Debug.LogError("Projectile spell \"" + spell.name + "\" must be directional.", spell);
            return false;
        }

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

        playerResources.SetMana(playerResources.Mana - spell.ManaCost);
        nextCastTime = Time.time;
        if (uiManager != null)
            uiManager.HideUI();

        SpellCast?.Invoke(this);
        return true;
    }

    private bool CastSap(SpellDefinition spell)
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
            if (target.CompareTag("DropMana"))
                targetsToDestroy.Add(target);
        }

        foreach (GameObject target in targetsToDestroy)
            Destroy(target);

        playerResources.SetMana(playerResources.Mana + 1 + targetsToDestroy.Count);
        nextCastTime = Time.time;
        if (uiManager != null)
            uiManager.HideUI();

        return true;
    }
}
