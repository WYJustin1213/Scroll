using System.Collections;
using UnityEngine;

public class PlayerSpellController : MonoBehaviour
{
    public enum FireballDirection
    {
        Left,
        Forward,
        Right
    }

    [Header("Rules")]
    [SerializeField]
    private GameRulesConfig rules;

    [Header("References")]
    [SerializeField]
    private PlayerController player;

    [SerializeField]
    private PlayerHealth health;

    [SerializeField]
    private Transform spellOrigin;

    [Header("Fireball")]
    [SerializeField]
    private FireballProjectile fireballPrefab;

    [SerializeField]
    private EnemyManager enemyManager;

    public bool IsCasting { get; private set; }
    public bool LastCastSucceeded { get; private set; }

    private void Awake()
    {
        if (player == null)
            player = GetComponent<PlayerController>();

        if (health == null)
            health = GetComponent<PlayerHealth>();
    }

    public Vector3 GetFireballDirection(
        FireballDirection direction)
    {
        if (player == null)
            return Vector3.forward;

        switch (direction)
        {
            case FireballDirection.Left:
                return (
                    player.ForwardVector +
                    player.LeftVector
                ).normalized;

            case FireballDirection.Right:
                return (
                    player.ForwardVector +
                    player.RightVector
                ).normalized;

            case FireballDirection.Forward:
            default:
                return player.ForwardVector;
        }
    }

    public IEnumerator CastFireball(
        FireballDirection direction)
    {
        if (!CanCast())
        {
            LastCastSucceeded = false;
            yield break;
        }

        EnemyManager resolvedEnemyManager =
            enemyManager != null
                ? enemyManager
                : EnemyManager.Instance;

        if (fireballPrefab == null)
        {
            Debug.LogError(
                "PlayerSpellController: Fireball Prefab is not assigned.",
                this
            );

            LastCastSucceeded = false;
            yield break;
        }

        if (resolvedEnemyManager == null)
        {
            Debug.LogError(
                "PlayerSpellController: no EnemyManager exists in the scene.",
                this
            );

            LastCastSucceeded = false;
            yield break;
        }

        IsCasting = true;
        LastCastSucceeded = true;

        Vector3 attackDirection =
            GetFireballDirection(direction);

        Quaternion spawnRotation =
            Quaternion.LookRotation(
                attackDirection,
                Vector3.up
            );

        FireballProjectile projectile =
            Instantiate(
                fireballPrefab,
                spellOrigin.position,
                spawnRotation
            );

        projectile.Initialize(
            player.transform.position,
            attackDirection,
            resolvedEnemyManager
        );

        float duration =
            Mathf.Max(
                0f,
                rules.fireballCastDuration
            );

        if (duration > 0f)
        {
            yield return new WaitForSeconds(duration);
        }

        IsCasting = false;
    }

    public IEnumerator TeleportBackward()
    {
        if (player == null)
        {
            LastCastSucceeded = false;
            yield break;
        }

        yield return player.TeleportBackward();
        LastCastSucceeded = player.LastActionSucceeded;
    }

    public IEnumerator UseArcaneEye(
        Transform target)
    {
        if (target == null || player == null)
        {
            LastCastSucceeded = false;
            yield break;
        }

        yield return player.FaceToward(target.position);
        LastCastSucceeded = player.LastActionSucceeded;
    }

    private bool CanCast()
    {
        if (IsCasting)
            return false;

        if (player == null ||
            spellOrigin == null ||
            rules == null)
        {
            return false;
        }

        if (player.IsBusy)
            return false;

        if (health != null && health.IsDead)
            return false;

        return true;
    }
}
