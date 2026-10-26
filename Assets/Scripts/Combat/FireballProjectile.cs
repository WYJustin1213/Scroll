using System.Collections;
using UnityEngine;

public class FireballProjectile : MonoBehaviour
{
    [Header("Travel")]
    [Min(0.01f)]
    [SerializeField]
    private float moveSpeed = 10f;

    [Min(1)]
    [SerializeField]
    private int maxGridSteps = 6;

    [Header("Optional Wall Blocking")]
    [SerializeField]
    private LayerMask blockingLayers;

    [Min(0f)]
    [SerializeField]
    private float blockingCheckRadius = 0.2f;

    private EnemyManager enemyManager;
    private Vector2Int currentCell;
    private Vector2Int cellStep;
    private float travelHeight;
    private bool initialized;

    public void Initialize(
        Vector3 logicalStartWorld,
        Vector3 attackDirection,
        EnemyManager manager)
    {
        if (initialized)
            return;

        initialized = true;
        enemyManager = manager;

        if (enemyManager == null)
        {
            Debug.LogError(
                "FireballProjectile needs an EnemyManager.",
                this
            );

            Destroy(gameObject);
            return;
        }

        currentCell =
            enemyManager.WorldToCell(logicalStartWorld);

        cellStep = DirectionToCellStep(attackDirection);
        travelHeight = transform.position.y;

        if (cellStep == Vector2Int.zero)
        {
            Debug.LogError(
                "FireballProjectile received a zero attack direction.",
                this
            );

            Destroy(gameObject);
            return;
        }

        Vector3 flatDirection = new Vector3(
            cellStep.x,
            0f,
            cellStep.y
        ).normalized;

        transform.rotation = Quaternion.LookRotation(
            flatDirection,
            Vector3.up
        );

        StartCoroutine(TravelGrid());
    }

    private IEnumerator TravelGrid()
    {
        for (int stepIndex = 0;
             stepIndex < maxGridSteps;
             stepIndex++)
        {
            Vector2Int nextCell =
                currentCell + cellStep;

            if (IsBlocked(nextCell))
                break;

            Vector3 targetWorld =
                enemyManager.CellToWorld(
                    nextCell,
                    travelHeight
                );

            while ((transform.position - targetWorld)
                   .sqrMagnitude > 0.0001f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetWorld,
                    moveSpeed * Time.deltaTime
                );

                yield return null;
            }

            transform.position = targetWorld;
            currentCell = nextCell;

            if (enemyManager.TryGetEnemyAtCell(
                    currentCell,
                    out EnemyGridTarget enemy))
            {
                enemy.Kill();
                break;
            }
        }

        Destroy(gameObject);
    }

    private bool IsBlocked(Vector2Int cell)
    {
        if (blockingLayers.value == 0 ||
            blockingCheckRadius <= 0f)
        {
            return false;
        }

        Vector3 worldPosition =
            enemyManager.CellToWorld(
                cell,
                travelHeight
            );

        return Physics.CheckSphere(
            worldPosition,
            blockingCheckRadius,
            blockingLayers,
            QueryTriggerInteraction.Ignore
        );
    }

    private static Vector2Int DirectionToCellStep(
        Vector3 direction)
    {
        direction.y = 0f;

        int x = Mathf.Abs(direction.x) > 0.1f
            ? (int)Mathf.Sign(direction.x)
            : 0;

        int z = Mathf.Abs(direction.z) > 0.1f
            ? (int)Mathf.Sign(direction.z)
            : 0;

        return new Vector2Int(x, z);
    }
}
