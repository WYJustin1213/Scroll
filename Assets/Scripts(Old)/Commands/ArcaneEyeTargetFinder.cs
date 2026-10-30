using UnityEngine;

public class ArcaneEyeTargetFinder : MonoBehaviour
{
    [Header("Search")]
    [SerializeField] private LayerMask enemyLayer;

    [SerializeField] private float searchRadius = 30f;

    [Header("Prototype Fallback")]
    [SerializeField] private Transform fallbackTarget;

    public Transform FindTarget(Vector3 searchOrigin)
    {
        Collider[] hits = Physics.OverlapSphere(
            searchOrigin,
            searchRadius,
            enemyLayer,
            QueryTriggerInteraction.Collide
        );

        Transform closestTarget = null;
        float closestDistanceSquared = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            if (hit == null)
                continue;

            float distanceSquared = (hit.transform.position - searchOrigin).sqrMagnitude;

            if (distanceSquared < closestDistanceSquared)
            {
                closestDistanceSquared = distanceSquared;

                closestTarget = hit.transform;
            }
        }

        if (closestTarget != null)
            return closestTarget;

        return fallbackTarget;
    }
}
