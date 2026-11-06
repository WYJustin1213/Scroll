using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class LaneFireballProjectile : MonoBehaviour
{
    [Header("Travel")]
    [SerializeField, Min(0.01f)] private float moveSpeed = 16f;
    [SerializeField, Min(1)] private int maxTileRange = 8;
    [SerializeField, Min(1)] private int damage = 1;

    [Header("Visual")]
    [SerializeField] private Color projectileColor = new Color(1f, 0.3f, 0.05f, 1f);

    private bool initialized;

    private void Awake()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        MaterialPropertyBlock properties = new MaterialPropertyBlock();

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].GetPropertyBlock(properties);
            properties.SetColor("_Color", projectileColor);
            properties.SetColor("_BaseColor", projectileColor);
            renderers[i].SetPropertyBlock(properties);
        }
    }

    // firstTileIndex is the first tile the fireball travels to (inclusive).
    public bool Initialize(LaneManager lane, int firstTileIndex)
    {
        if (initialized)
            return false;

        initialized = true;

        if (lane == null || lane.GetTile(firstTileIndex) == null)
        {
            Debug.LogError("LaneFireballProjectile needs a valid lane and first tile.", this);
            Destroy(gameObject);
            return false;
        }

        StartCoroutine(TravelLane(lane, firstTileIndex));
        return true;
    }

    private IEnumerator TravelLane(LaneManager lane, int firstTileIndex)
    {
        LaneTile firstTile = lane.GetTile(firstTileIndex);
        float heightOffset = transform.position.y - firstTile.StandPosition.y;
        int lastTileIndex = Mathf.Min(
            firstTileIndex + maxTileRange - 1,
            lane.TileCount - 1
        );

        for (int tileIndex = firstTileIndex;
             tileIndex <= lastTileIndex;
             tileIndex++)
        {
            LaneTile tile = lane.GetTile(tileIndex);
            Vector3 targetPosition = tile.StandPosition + Vector3.up * heightOffset;
            Vector3 flatDirection = targetPosition - transform.position;
            flatDirection.y = 0f;

            if (flatDirection.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(flatDirection);

            while ((transform.position - targetPosition).sqrMagnitude > 0.0001f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPosition,
                    moveSpeed * Time.deltaTime
                );

                yield return null;
            }

            transform.position = targetPosition;

            LaneTestEnemy enemy = FindEnemyOnTile(lane, tile);
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                break;
            }
        }

        Destroy(gameObject);
    }

    private static LaneTestEnemy FindEnemyOnTile(LaneManager lane, LaneTile tile)
    {
        LaneTestEnemy[] enemies = FindObjectsByType<LaneTestEnemy>(FindObjectsSortMode.None);

        for (int i = 0; i < enemies.Length; i++)
        {
            LaneTestEnemy enemy = enemies[i];
            if (enemy != null &&
                !enemy.IsDead &&
                enemy.Lane == lane &&
                enemy.CurrentTile == tile)
            {
                return enemy;
            }
        }

        return null;
    }
}
