using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CombatManager : MonoBehaviour
{
    [Serializable]
    private struct LaneName
    {
        [SerializeField] private LaneManager lane;
        [SerializeField] private string name;

        public LaneManager Lane => lane;
        public string Name => name;
    }

    [SerializeField] private PlayerResources playerResources;
    [Tooltip("Optional spell-targeting names for lanes. Unlisted lanes keep their Lane Spell Point name.")]
    [SerializeField] private List<LaneName> laneNames = new List<LaneName>();
    [Tooltip("Index of the lane tile enemies stop at and attack from. 0 is Tile 1, the tile closest to the player.")]
    [SerializeField, Min(0)] private int attackTileIndex;
    [Tooltip("Seconds an enemy waits after stopping at the attack tile before it can attack.")]
    [SerializeField, Min(0f)] private float attackPause = 0.5f;
    [Tooltip("Seconds between any two enemy attacks, shared by all enemies.")]
    [SerializeField, Min(0f)] private float attackCooldown = 1f;

    [Tooltip("Sprite scale multiplier applied to an enemy while it is attacking.")]
    [SerializeField, Min(1f)] private float attackScaleMultiplier = 1.5f;
    [Tooltip("Seconds the enemy sprite stays enlarged after an attack.")]
    [SerializeField, Min(0f)] private float attackScaleDuration = 1f;

    private float nextAttackTime;

    public static CombatManager Instance { get; private set; }

    public PlayerResources PlayerResources => playerResources;
    public string GetLaneName(LaneSpellPoint lanePoint)
    {
        if (lanePoint == null || lanePoint.Lane == null)
            return lanePoint != null ? lanePoint.LaneName : string.Empty;

        for (int i = 0; i < laneNames.Count; i++)
        {
            if (laneNames[i].Lane == lanePoint.Lane && !string.IsNullOrWhiteSpace(laneNames[i].Name))
                return laneNames[i].Name;
        }

        return lanePoint.LaneName;
    }

    public int AttackTileIndex => attackTileIndex;
    public float AttackPause => attackPause;
    public float AttackCooldown => attackCooldown;
    public float AttackScaleMultiplier => attackScaleMultiplier;
    public float AttackScaleDuration => attackScaleDuration;

    private void OnEnable()
    {
        Instance = this;
        nextAttackTime = 0f;
    }

    private void OnDisable()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool CanAttack => Time.time >= nextAttackTime;

    // Returns true if the attack happened; fails while the shared cooldown is running.
    public bool TryAttack(int damage)
    {
        if (!CanAttack || playerResources == null)
            return false;

        nextAttackTime = Time.time + attackCooldown;
        playerResources.SetHealth(playerResources.Health - Mathf.Max(0, damage));
        return true;
    }
}
