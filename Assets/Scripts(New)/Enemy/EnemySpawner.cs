using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemySpawner : MonoBehaviour
{
    private enum SelectionMode
    {
        Equal,
        Tiered
    }

    private enum SpawnResult
    {
        Spawned,
        Occupied,
        NoEligiblePrefab,
        Invalid
    }

    [SerializeField] private LaneTile spawnTile;
    [Tooltip("Player mana source. If unassigned, the spawner finds a PlayerResources in the scene.")]
    [SerializeField] private PlayerResources playerResources;
    [SerializeField, Min(0.01f)] private float spawnInterval = 5f;
    [SerializeField] private SelectionMode selectionMode;
    [Tooltip("Assign each enemy prefab only once. Its definition determines its tier.")]
    [SerializeField] private List<LaneTestEnemy> enemyPrefabs = new List<LaneTestEnemy>();
    [Header("Tier chances (must total 100%)")]
    [SerializeField, Range(0, 100)] private int commonChance = 70;
    [SerializeField, Range(0, 100)] private int rareChance = 25;
    [SerializeField, Range(0, 100)] private int epicChance = 5;

    private float elapsed;
    private int previousMana;

    private void OnEnable()
    {
        elapsed = 0f;
        if (playerResources == null)
            playerResources = FindFirstObjectByType<PlayerResources>();

        if (playerResources == null)
        {
            Debug.LogError("EnemySpawner needs the player's PlayerResources to spawn mana waves.", this);
            return;
        }

        previousMana = playerResources.Mana;
        playerResources.Changed += OnResourcesChanged;
    }

    private void OnDisable()
    {
        if (playerResources != null)
            playerResources.Changed -= OnResourcesChanged;
    }

    private void OnResourcesChanged()
    {
        int mana = playerResources.Mana;
        int crossings = mana > previousMana ? mana / 15 - previousMana / 15 : 0;
        previousMana = mana;

        for (int i = 0; i < crossings; i++)
        {
            SpawnResult result = TrySpawn();
            if (result != SpawnResult.NoEligiblePrefab || !HasSelectablePrefab())
                break;
        }
    }

    private void Update()
    {
        if (spawnInterval <= 0f || float.IsNaN(spawnInterval) || float.IsInfinity(spawnInterval))
        {
            Debug.LogError("EnemySpawner needs a positive, finite spawn interval.", this);
            enabled = false;
            return;
        }

        elapsed += Time.deltaTime;
        if (elapsed < spawnInterval)
            return;

        elapsed %= spawnInterval;
        Spawn();
    }

    private void Spawn()
    {
        TrySpawn();
    }

    private SpawnResult TrySpawn()
    {
        if (spawnTile == null || spawnTile.Lane == null)
        {
            Debug.LogError("EnemySpawner needs a spawn tile assigned to a lane.", this);
            return SpawnResult.Invalid;
        }

        if (selectionMode != SelectionMode.Equal && selectionMode != SelectionMode.Tiered)
        {
            Debug.LogError("EnemySpawner has an invalid selection mode.", this);
            return SpawnResult.Invalid;
        }

        if (selectionMode == SelectionMode.Tiered &&
            (commonChance < 0 || rareChance < 0 || epicChance < 0 ||
             commonChance > 100 || rareChance > 100 || epicChance > 100 ||
             commonChance + rareChance + epicChance != 100))
        {
            Debug.LogError("EnemySpawner tier chances must each be between 0 and 100 and total 100%.", this);
            return SpawnResult.Invalid;
        }

        var seen = new HashSet<LaneTestEnemy>();
        foreach (LaneTestEnemy prefab in enemyPrefabs)
        {
            if (prefab == null || prefab.Definition == null || !prefab.gameObject.activeSelf || !seen.Add(prefab))
            {
                Debug.LogError("EnemySpawner prefabs must be active, unique enemies with definitions.", this);
                return SpawnResult.Invalid;
            }

            EnemySpawnTier tier = prefab.Definition.SpawnTier;
            if (tier != EnemySpawnTier.Common && tier != EnemySpawnTier.Rare && tier != EnemySpawnTier.Epic)
            {
                Debug.LogError("EnemySpawner found an enemy with an invalid spawn tier.", prefab);
                return SpawnResult.Invalid;
            }
        }

        foreach (LaneTestEnemy enemy in FindObjectsByType<LaneTestEnemy>(FindObjectsSortMode.None))
        {
            if (!enemy.IsDead && enemy.CurrentTile == spawnTile)
                return SpawnResult.Occupied;
        }

        foreach (LanePlayerMarker player in FindObjectsByType<LanePlayerMarker>(FindObjectsSortMode.None))
        {
            if (player.Lane == spawnTile.Lane && player.TileIndex == spawnTile.Index)
                return SpawnResult.Occupied;
        }

        LaneTestEnemy selectedPrefab = SelectPrefab();
        if (selectedPrefab == null)
            return SpawnResult.NoEligiblePrefab;

        LaneTestEnemy spawnedEnemy = Instantiate(
            selectedPrefab, spawnTile.StandPosition, selectedPrefab.transform.rotation, spawnTile.Lane.transform);
        if (!spawnedEnemy.InitializeAtTile(spawnTile.Lane, spawnTile))
        {
            Destroy(spawnedEnemy.gameObject);
            return SpawnResult.Invalid;
        }

        return SpawnResult.Spawned;
    }

    private bool HasSelectablePrefab()
    {
        foreach (LaneTestEnemy prefab in enemyPrefabs)
        {
            if (selectionMode == SelectionMode.Equal ||
                (prefab.Definition.SpawnTier == EnemySpawnTier.Common && commonChance > 0) ||
                (prefab.Definition.SpawnTier == EnemySpawnTier.Rare && rareChance > 0) ||
                (prefab.Definition.SpawnTier == EnemySpawnTier.Epic && epicChance > 0))
                return true;
        }

        return false;
    }

    private LaneTestEnemy SelectPrefab()
    {
        EnemySpawnTier selectedTier = EnemySpawnTier.Common;
        if (selectionMode == SelectionMode.Tiered)
        {
            int roll = Random.Range(0, 100);
            selectedTier = roll < commonChance ? EnemySpawnTier.Common
                : roll < commonChance + rareChance ? EnemySpawnTier.Rare : EnemySpawnTier.Epic;
        }

        return SelectPrefabForTier(selectionMode == SelectionMode.Tiered ? selectedTier : (EnemySpawnTier?)null);
    }

    private LaneTestEnemy SelectPrefabForTier(EnemySpawnTier? selectedTier)
    {
        int eligibleCount = 0;
        foreach (LaneTestEnemy prefab in enemyPrefabs)
        {
            if (!selectedTier.HasValue || prefab.Definition.SpawnTier == selectedTier.Value)
                eligibleCount++;
        }

        if (eligibleCount == 0)
        {
            string selection = selectedTier.HasValue ? selectedTier.Value.ToString() + " tier" : "prefab list";
            Debug.LogWarning("EnemySpawner skipped a spawn because its " + selection + " is empty.", this);
            return null;
        }

        int selectedIndex = Random.Range(0, eligibleCount);
        foreach (LaneTestEnemy prefab in enemyPrefabs)
        {
            if (selectedTier.HasValue && prefab.Definition.SpawnTier != selectedTier.Value)
                continue;

            if (selectedIndex-- != 0)
                continue;

            return prefab;
        }

        Debug.LogError("EnemySpawner could not select an eligible prefab.", this);
        return null;
    }
}
