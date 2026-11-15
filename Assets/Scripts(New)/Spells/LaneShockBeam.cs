using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class LaneShockBeam : MonoBehaviour
{
    [Header("Travel")]
    [SerializeField, Min(0.01f)] private float moveSpeed = 16f;
    [SerializeField, Min(1)] private int maxTileRange = 8;
    [SerializeField, Min(1)] private int damage = 1;

    [Header("Trail")]
    [SerializeField] private Sprite trailSprite;
    [SerializeField, Min(0.01f)] private float trailSpacing = 0.35f;
    [SerializeField, Min(0.01f)] private float trailScale = 0.1f;
    [SerializeField, Min(0f)] private float fadeDuration = 0.5f;

    [Header("Hit Effect")]
    [SerializeField] private SpellHitEffect hitEffectPrefab;
    [SerializeField] private Vector3 hitEffectOffset = new Vector3(0f, 0.5f, 0f);

    private bool initialized;
    private SpellElement element = SpellElement.None;
    private Transform trailRoot;
    private Vector3 trailPositionOffset;
    private Quaternion trailRotation;
    private Dictionary<LaneTestEnemy, Vector3> enemyPositions =
        new Dictionary<LaneTestEnemy, Vector3>();
    private Dictionary<LaneTestEnemy, Vector3> currentEnemyPositions =
        new Dictionary<LaneTestEnemy, Vector3>();

    public bool Initialize(
        LaneManager lane, int firstTileIndex, SpellElement spellElement,
        Vector3 positionOffset, Vector3 rotation)
    {
        if (initialized)
            return false;

        initialized = true;
        element = spellElement;
        trailPositionOffset = positionOffset;
        trailRotation = Quaternion.Euler(rotation);

        if (lane == null || lane.GetTile(firstTileIndex) == null)
        {
            Debug.LogError("LaneShockBeam needs a valid lane and first tile.", this);
            Destroy(gameObject);
            return false;
        }

        if (trailSprite == null)
        {
            Debug.LogError("LaneShockBeam needs a trail sprite assigned.", this);
            Destroy(gameObject);
            return false;
        }

        if (moveSpeed <= 0f || float.IsNaN(moveSpeed) || float.IsInfinity(moveSpeed) ||
            maxTileRange < 1 || trailSpacing <= 0f || float.IsNaN(trailSpacing) ||
            float.IsInfinity(trailSpacing) || trailScale <= 0f ||
            fadeDuration < 0f || float.IsNaN(fadeDuration) || float.IsInfinity(fadeDuration))
        {
            Debug.LogError("LaneShockBeam has invalid travel or trail settings.", this);
            Destroy(gameObject);
            return false;
        }

        trailRoot = new GameObject("Shock Trail").transform;
        CreateTrailSprite(transform.position);
        RememberEnemyPositions(lane);
        StartCoroutine(TravelLane(lane, firstTileIndex));
        return true;
    }

    private void OnDestroy()
    {
        if (trailRoot != null)
            Destroy(trailRoot.gameObject);
    }

    private IEnumerator TravelLane(LaneManager lane, int firstTileIndex)
    {
        LaneTile firstTile = lane.GetTile(firstTileIndex);
        float heightOffset = transform.position.y - firstTile.StandPosition.y;
        int lastTileIndex = Mathf.Min(firstTileIndex + maxTileRange - 1, lane.TileCount - 1);
        float distanceUntilNextSprite = trailSpacing;

        for (int tileIndex = firstTileIndex; tileIndex <= lastTileIndex; tileIndex++)
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

                AddTrailAlongSegment(startPosition, endPosition, ref distanceUntilNextSprite);
                LaneTestEnemy enemy = FindCrossedEnemy(lane, startPosition, endPosition, flatDirection);
                transform.position = endPosition;
                if (enemy != null)
                {
                    ChainFrom(enemy);
                    yield return FadeTrail();
                    yield break;
                }

                if ((endPosition - targetPosition).sqrMagnitude == 0f && tileIndex == lastTileIndex)
                    break;

                if ((endPosition - startPosition).sqrMagnitude > 0f ||
                    (endPosition - targetPosition).sqrMagnitude > 0f)
                    yield return null;
            } while ((transform.position - targetPosition).sqrMagnitude > 0f);
        }

        yield return FadeTrail();
    }

    private void ChainFrom(LaneTestEnemy firstEnemy)
    {
        var hitEnemies = new HashSet<LaneTestEnemy>();
        LaneTestEnemy current = firstEnemy;

        while (current != null && hitEnemies.Add(current))
        {
            Vector3 hitPosition = current.transform.position;
            if (hitEffectPrefab != null)
                Instantiate(hitEffectPrefab, hitPosition + hitEffectOffset, Quaternion.identity);

            current.TakeDamage(damage, element);

            LaneTestEnemy next = FindNextTarget(current, hitEnemies);
            if (next == null)
                return;

            AddTrailBetween(transform.position, next.transform.position);
            transform.position = next.transform.position;
            current = next;
        }
    }

    private LaneTestEnemy FindNextTarget(LaneTestEnemy current, HashSet<LaneTestEnemy> hitEnemies)
    {
        if (current == null || current.Lane == null || current.CurrentTile == null)
            return null;

        LaneManager lane = current.Lane;
        int currentTileIndex = current.CurrentTile.Index;
        Vector3 position = current.transform.position;
        LaneTestEnemy behind = FindEnemyOnTile(lane, currentTileIndex + 1, position, hitEnemies);
        if (behind != null)
            return behind;

        LaneManager leftLane = null;
        LaneManager rightLane = null;
        float leftDistance = float.PositiveInfinity;
        float rightDistance = float.PositiveInfinity;
        LaneManager[] sceneLanes = FindObjectsByType<LaneManager>(FindObjectsSortMode.None);

        for (int i = 0; i < sceneLanes.Length; i++)
        {
            LaneManager candidate = sceneLanes[i];
            if (candidate == null || candidate == lane ||
                candidate.gameObject.scene != lane.gameObject.scene ||
                candidate.transform.parent != lane.transform.parent)
                continue;

            float offset = candidate.transform.position.x - lane.transform.position.x;
            if (offset < 0f && -offset < leftDistance)
            {
                leftLane = candidate;
                leftDistance = -offset;
            }
            else if (offset > 0f && offset < rightDistance)
            {
                rightLane = candidate;
                rightDistance = offset;
            }
        }

        LaneTestEnemy left = FindEnemyOnTile(leftLane, currentTileIndex, position, hitEnemies);
        LaneTestEnemy right = FindEnemyOnTile(rightLane, currentTileIndex, position, hitEnemies);
        if (left == null)
            return right;
        if (right == null)
            return left;

        return (left.transform.position - position).sqrMagnitude <=
            (right.transform.position - position).sqrMagnitude ? left : right;
    }

    private static LaneTestEnemy FindEnemyOnTile(
        LaneManager lane, int tileIndex, Vector3 fromPosition, HashSet<LaneTestEnemy> hitEnemies)
    {
        if (lane == null || lane.GetTile(tileIndex) == null)
            return null;

        LaneTestEnemy[] enemies = FindObjectsByType<LaneTestEnemy>(FindObjectsSortMode.None);
        LaneTestEnemy closest = null;
        float closestDistance = float.PositiveInfinity;
        for (int i = 0; i < enemies.Length; i++)
        {
            LaneTestEnemy enemy = enemies[i];
            if (enemy == null || enemy.IsDead || enemy.Lane != lane ||
                enemy.CurrentTile == null || enemy.CurrentTile.Index != tileIndex ||
                hitEnemies.Contains(enemy))
                continue;

            float distance = (enemy.transform.position - fromPosition).sqrMagnitude;
            if (distance < closestDistance)
            {
                closest = enemy;
                closestDistance = distance;
            }
        }

        return closest;
    }

    private void AddTrailBetween(Vector3 start, Vector3 end)
    {
        Vector3 difference = end - start;
        float distance = difference.magnitude;
        if (distance <= 0f)
            return;

        Vector3 direction = difference / distance;
        for (float offset = trailSpacing; offset < distance; offset += trailSpacing)
            CreateTrailSprite(start + direction * offset);

        CreateTrailSprite(end);
    }

    private void AddTrailAlongSegment(Vector3 start, Vector3 end, ref float distanceUntilNextSprite)
    {
        Vector3 difference = end - start;
        float distance = difference.magnitude;
        if (distance <= 0f)
            return;

        Vector3 direction = difference / distance;
        float traveled = 0f;
        while (distanceUntilNextSprite <= distance - traveled)
        {
            traveled += distanceUntilNextSprite;
            CreateTrailSprite(start + direction * traveled);
            distanceUntilNextSprite = trailSpacing;
        }

        distanceUntilNextSprite -= distance - traveled;
    }

    private void CreateTrailSprite(Vector3 position)
    {
        GameObject segment = new GameObject("Shock Trail Sprite");
        segment.transform.SetPositionAndRotation(position + trailPositionOffset, trailRotation);
        segment.transform.localScale = Vector3.one * trailScale;
        segment.transform.SetParent(trailRoot, true);

        SpriteRenderer renderer = segment.AddComponent<SpriteRenderer>();
        renderer.sprite = trailSprite;
        renderer.sortingOrder = 10;
    }

    private IEnumerator FadeTrail()
    {
        if (fadeDuration <= 0f)
        {
            Destroy(gameObject);
            yield break;
        }

        SpriteRenderer[] renderers = trailRoot.GetComponentsInChildren<SpriteRenderer>();
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed = Mathf.Min(elapsed + Time.deltaTime, fadeDuration);
            float alpha = 1f - elapsed / fadeDuration;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                    continue;

                Color color = renderers[i].color;
                color.a = alpha;
                renderers[i].color = color;
            }

            yield return null;
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
