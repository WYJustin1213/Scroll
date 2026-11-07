using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SpawnEnemiesOnDeath : EnemyAbility
{
    [SerializeField] private LaneTestEnemy enemyPrefab;
    [SerializeField, Min(1)] private int spawnCount = 3;

    public override void OnDeath(LaneTestEnemy dyingEnemy)
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("Spawn Enemies On Death requires an enemy prefab.", this);
            return;
        }

        if (dyingEnemy.Lane == null || dyingEnemy.CurrentTile == null)
        {
            Debug.LogWarning("Cannot spawn enemies because the dying enemy has no lane tile.", dyingEnemy);
            return;
        }

        List<LaneTile> candidates = GetCandidateTiles(dyingEnemy);
        int spawnedCount = 0;
        for (int i = 0; i < spawnCount; i++)
        {
            LaneTile tile = FindFirstAvailableTile(candidates);
            if (tile == null)
                break;

            LaneTestEnemy spawnedEnemy = Instantiate(
                enemyPrefab,
                tile.StandPosition,
                Quaternion.identity,
                tile.Lane.transform);
            if (spawnedEnemy.gameObject.scene != dyingEnemy.gameObject.scene)
                SceneManager.MoveGameObjectToScene(spawnedEnemy.gameObject, dyingEnemy.gameObject.scene);

            if (!spawnedEnemy.InitializeAtTile(tile.Lane, tile))
            {
                Destroy(spawnedEnemy.gameObject);
                return;
            }

            spawnedCount++;
        }

        if (spawnedCount < spawnCount)
        {
            Debug.LogWarning(
                $"Only spawned {spawnedCount} of {spawnCount} requested enemies because no more preferred tiles are available.",
                dyingEnemy);
        }
    }

    private static List<LaneTile> GetCandidateTiles(LaneTestEnemy dyingEnemy)
    {
        LaneManager lane = dyingEnemy.Lane;
        LaneTile currentTile = dyingEnemy.CurrentTile;
        var candidates = new List<LaneTile> { currentTile };
        LaneManager[] sceneLanes = FindObjectsByType<LaneManager>(FindObjectsSortMode.None);
        var siblingLanes = new List<LaneManager>();

        for (int i = 0; i < sceneLanes.Length; i++)
        {
            LaneManager candidateLane = sceneLanes[i];
            if (candidateLane.gameObject.scene == lane.gameObject.scene &&
                candidateLane.transform.parent == lane.transform.parent)
            {
                siblingLanes.Add(candidateLane);
            }
        }

        Vector3 lateralAxis = lane.transform.right;
        siblingLanes.Sort((a, b) =>
            Vector3.Dot(a.transform.position, lateralAxis)
                .CompareTo(Vector3.Dot(b.transform.position, lateralAxis)));

        int lanePosition = siblingLanes.IndexOf(lane);
        if (lanePosition > 0)
        {
            AddCandidate(candidates, siblingLanes[lanePosition - 1].GetTile(currentTile.Index));
        }

        if (lanePosition >= 0 && lanePosition < siblingLanes.Count - 1)
        {
            AddCandidate(candidates, siblingLanes[lanePosition + 1].GetTile(currentTile.Index));
        }

        AddCandidate(candidates, lane.GetTile(currentTile.Index + 1));

        for (int tileIndex = currentTile.Index + 1; tileIndex < lane.TileCount; tileIndex++)
        {
            AddCandidate(candidates, lane.GetTile(tileIndex));
            AddCandidatesOnLanes(candidates, siblingLanes, lane, tileIndex);
        }

        for (int tileIndex = currentTile.Index - 1; tileIndex >= 0; tileIndex--)
        {
            AddCandidate(candidates, lane.GetTile(tileIndex));
            AddCandidatesOnLanes(candidates, siblingLanes, lane, tileIndex);
        }

        return candidates;
    }

    private static void AddCandidatesOnLanes(
        List<LaneTile> candidates,
        List<LaneManager> siblingLanes,
        LaneManager lane,
        int tileIndex)
    {
        for (int i = 0; i < siblingLanes.Count; i++)
        {
            LaneManager candidateLane = siblingLanes[i];
            if (candidateLane != lane)
                AddCandidate(candidates, candidateLane.GetTile(tileIndex));
        }
    }

    private static void AddCandidate(List<LaneTile> candidates, LaneTile candidate)
    {
        if (candidate != null && !candidates.Contains(candidate))
            candidates.Add(candidate);
    }

    private static LaneTile FindFirstAvailableTile(List<LaneTile> candidates)
    {
        LaneTestEnemy[] enemies = FindObjectsByType<LaneTestEnemy>(FindObjectsSortMode.None);
        for (int i = 0; i < candidates.Count; i++)
        {
            LaneTile candidate = candidates[i];
            bool occupied = false;
            for (int j = 0; j < enemies.Length; j++)
            {
                LaneTestEnemy enemy = enemies[j];
                if (enemy != null &&
                    !enemy.IsDead &&
                    enemy.Lane == candidate.Lane &&
                    enemy.CurrentTile == candidate)
                {
                    occupied = true;
                    break;
                }
            }

            if (!occupied)
                return candidate;
        }

        return null;
    }
}
