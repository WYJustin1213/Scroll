using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(LaneTestEnemy))]
public class JumpOnSpellCast : EnemyAbility
{
    [SerializeField, Min(0.01f)] private float jumpDuration = 0.35f;
    [SerializeField, Min(0f)] private float jumpHeight = 1.5f;

    private LaneTestEnemy enemy;

    private void OnEnable()
    {
        enemy = GetComponent<LaneTestEnemy>();
        if (Application.isPlaying)
            LaneSpellController.SpellCast += HandleSpellCast;
    }

    private void OnDisable()
    {
        LaneSpellController.SpellCast -= HandleSpellCast;
    }

    public override void OnDeath(LaneTestEnemy dyingEnemy)
    {
    }

    private void HandleSpellCast(LaneSpellController controller)
    {
        if (enemy == null || enemy.IsDead || !enemy.isActiveAndEnabled ||
            enemy.Definition == null || !enemy.Definition.Ability ||
            controller.gameObject.scene != gameObject.scene)
            return;

        if (enemy.Lane == null || enemy.CurrentTile == null)
        {
            Debug.LogWarning("Cannot jump because the enemy has no lane tile.", enemy);
            return;
        }

        LaneTile destination = FindDestination();
        if (destination != null)
            enemy.JumpToTile(destination, jumpDuration, jumpHeight);
    }

    private LaneTile FindDestination()
    {
        LaneManager currentLane = enemy.Lane;
        int currentIndex = enemy.CurrentTile.Index;
        LaneManager[] sceneLanes = FindObjectsByType<LaneManager>(FindObjectsSortMode.None);
        LaneTestEnemy[] enemies = FindObjectsByType<LaneTestEnemy>(FindObjectsSortMode.None);
        LanePlayerMarker[] players = FindObjectsByType<LanePlayerMarker>(FindObjectsSortMode.None);
        var lanes = new List<LaneManager>();
        int maxTileCount = 0;

        foreach (LaneManager lane in sceneLanes)
        {
            if (lane != currentLane && lane.gameObject.scene == currentLane.gameObject.scene &&
                lane.transform.parent == currentLane.transform.parent)
            {
                lanes.Add(lane);
                maxTileCount = Mathf.Max(maxTileCount, lane.TileCount);
            }
        }

        for (int index = currentIndex; index < maxTileCount; index++)
        {
            LaneTile destination = ChooseAvailableTile(lanes, enemies, players, index);
            if (destination != null)
                return destination;
        }

        for (int index = currentIndex - 1; index >= 0; index--)
        {
            LaneTile destination = ChooseAvailableTile(lanes, enemies, players, index);
            if (destination != null)
                return destination;
        }

        return null;
    }

    private static LaneTile ChooseAvailableTile(
        List<LaneManager> lanes, LaneTestEnemy[] enemies, LanePlayerMarker[] players, int index)
    {
        var candidates = new List<LaneTile>();
        foreach (LaneManager lane in lanes)
        {
            LaneTile tile = lane.GetTile(index);
            if (tile == null)
                continue;

            bool occupied = false;
            foreach (LaneTestEnemy other in enemies)
            {
                if (!other.IsDead && other.Lane == lane && other.CurrentTile == tile)
                {
                    occupied = true;
                    break;
                }
            }

            foreach (LanePlayerMarker player in players)
            {
                if (player.Lane == lane && player.TileIndex == index)
                {
                    occupied = true;
                    break;
                }
            }

            if (!occupied)
                candidates.Add(tile);
        }

        return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;
    }
}
