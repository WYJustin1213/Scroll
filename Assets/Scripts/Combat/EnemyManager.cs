using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    [Header("Grid")]
    [SerializeField]
    private GameRulesConfig rules;

    private readonly HashSet<EnemyGridTarget> enemies =
        new HashSet<EnemyGridTarget>();

    public float CellSize =>
        rules != null ? rules.cellSize : 2f;

    public int AliveCount
    {
        get
        {
            int count = 0;

            foreach (EnemyGridTarget enemy in enemies)
            {
                if (enemy != null && !enemy.IsDead)
                    count++;
            }

            return count;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError(
                "More than one EnemyManager exists in the scene.",
                this
            );

            Destroy(this);
            return;
        }

        Instance = this;

        if (rules == null)
        {
            Debug.LogError(
                "EnemyManager: GameRulesConfig is not assigned.",
                this
            );
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Register(EnemyGridTarget enemy)
    {
        if (enemy == null)
            return;

        enemies.Add(enemy);
    }

    public void Unregister(EnemyGridTarget enemy)
    {
        if (enemy == null)
            return;

        enemies.Remove(enemy);
    }

    public bool TryGetEnemyAtCell(
        Vector2Int cell,
        out EnemyGridTarget enemy)
    {
        foreach (EnemyGridTarget candidate in enemies)
        {
            if (candidate == null || candidate.IsDead)
                continue;

            if (WorldToCell(candidate.transform.position) == cell)
            {
                enemy = candidate;
                return true;
            }
        }

        enemy = null;
        return false;
    }

    public void KillAllEnemies()
    {
        List<EnemyGridTarget> snapshot =
            new List<EnemyGridTarget>(enemies);

        for (int i = 0; i < snapshot.Count; i++)
        {
            EnemyGridTarget enemy = snapshot[i];

            if (enemy != null && !enemy.IsDead)
            {
                enemy.Kill();
            }
        }
    }

    public Vector2Int WorldToCell(Vector3 worldPosition)
    {
        float size = Mathf.Max(0.1f, CellSize);

        return new Vector2Int(
            Mathf.RoundToInt(worldPosition.x / size),
            Mathf.RoundToInt(worldPosition.z / size)
        );
    }

    public Vector3 CellToWorld(
        Vector2Int cell,
        float y)
    {
        float size = Mathf.Max(0.1f, CellSize);

        return new Vector3(
            cell.x * size,
            y,
            cell.y * size
        );
    }

    public Vector3 SnapWorldToGrid(Vector3 worldPosition)
    {
        Vector2Int cell = WorldToCell(worldPosition);
        return CellToWorld(cell, worldPosition.y);
    }
}
