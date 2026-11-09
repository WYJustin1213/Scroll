using System.Collections;
using System.Collections.Generic;
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

    [Header("Hit Effect")]
    [SerializeField] private SpellHitEffect hitEffectPrefab;
    [SerializeField] private Vector3 hitEffectOffset = new Vector3(0f, 0.5f, 0f);

    private bool initialized;
    private SpellElement element = SpellElement.None;
    private Dictionary<LaneTestEnemy, Vector3> enemyPositions = new Dictionary<LaneTestEnemy, Vector3>();
    private Dictionary<LaneTestEnemy, Vector3> currentEnemyPositions = new Dictionary<LaneTestEnemy, Vector3>();

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
    public bool Initialize(LaneManager lane, int firstTileIndex, SpellElement spellElement)
    {
        if (initialized)
            return false;

        initialized = true;
        element = spellElement;

        if (lane == null || lane.GetTile(firstTileIndex) == null)
        {
            Debug.LogError("LaneFireballProjectile needs a valid lane and first tile.", this);
            Destroy(gameObject);
            return false;
        }

        RememberEnemyPositions(lane);
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

            do
            {
                Vector3 startPosition = transform.position;
                Vector3 endPosition = Vector3.MoveTowards(
                    startPosition,
                    targetPosition,
                    moveSpeed * Time.deltaTime
                );

                LaneTestEnemy enemy = FindCrossedEnemy(lane, startPosition, endPosition, flatDirection);
                transform.position = endPosition;
                if (enemy != null)
                {
                    if (hitEffectPrefab != null)
                        Instantiate(hitEffectPrefab, enemy.transform.position + hitEffectOffset, Quaternion.identity);

                    enemy.TakeDamage(damage, element);
                    Destroy(gameObject);
                    yield break;
                }

                if ((endPosition - targetPosition).sqrMagnitude == 0f && tileIndex == lastTileIndex)
                    break;

                if ((endPosition - startPosition).sqrMagnitude > 0f ||
                    (endPosition - targetPosition).sqrMagnitude > 0f)
                    yield return null;
            } while ((transform.position - targetPosition).sqrMagnitude > 0f);
        }

        Destroy(gameObject);
    }

    private void RememberEnemyPositions(LaneManager lane)
    {
        enemyPositions.Clear();
        LaneTestEnemy[] enemies = FindObjectsByType<LaneTestEnemy>(FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            if (!enemies[i].IsDead && enemies[i].Lane == lane)
                enemyPositions[enemies[i]] = enemies[i].transform.position;
        }
    }

    private LaneTestEnemy FindCrossedEnemy(
        LaneManager lane, Vector3 startPosition, Vector3 endPosition, Vector3 direction)
    {
        LaneTestEnemy[] enemies = FindObjectsByType<LaneTestEnemy>(FindObjectsSortMode.None);
        currentEnemyPositions.Clear();
        LaneTestEnemy closestEnemy = null;
        float closestTime = float.PositiveInfinity;
        direction.y = 0f;
        direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : lane.transform.forward;
        direction.y = 0f;
        direction.Normalize();

        for (int i = 0; i < enemies.Length; i++)
        {
            LaneTestEnemy enemy = enemies[i];
            if (enemy == null || enemy.IsDead || enemy.Lane != lane)
                continue;

            Vector3 currentPosition = enemy.transform.position;
            currentEnemyPositions[enemy] = currentPosition;
            if (!enemyPositions.TryGetValue(enemy, out Vector3 previousPosition))
                previousPosition = currentPosition;

            // Compare relative positions along the lane so motion by either object can cause a hit.
            float startDistance = Vector3.Dot(previousPosition - startPosition, direction);
            float endDistance = Vector3.Dot(currentPosition - endPosition, direction);
            if ((startDistance > 0f && endDistance > 0f) ||
                (startDistance < 0f && endDistance < 0f))
                continue;

            float distanceChange = startDistance - endDistance;
            float hitTime = distanceChange != 0f ? startDistance / distanceChange : 0f;
            if (hitTime < closestTime)
            {
                closestTime = hitTime;
                closestEnemy = enemy;
            }
        }

        Dictionary<LaneTestEnemy, Vector3> previousPositions = enemyPositions;
        enemyPositions = currentEnemyPositions;
        currentEnemyPositions = previousPositions;
        return closestEnemy;
    }
}
