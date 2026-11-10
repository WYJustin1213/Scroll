using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(LaneTestEnemy))]
public class EnemyAttack : MonoBehaviour
{
    private static readonly System.Collections.Generic.List<EnemyAttack> waiting = new System.Collections.Generic.List<EnemyAttack>();

    private LaneTestEnemy enemy;
    private float readySince;
    private Transform visual;
    private Vector3 baseScale = Vector3.one;
    private float waitedAtTile;
    private float enlargedUntil;
    private bool enlarged;

    private void Awake()
    {
        enemy = GetComponent<LaneTestEnemy>();
    }

    private void OnDisable()
    {
        Unregister();
        RestoreScale();
    }

    private void Update()
    {
        if (enlarged && Time.time >= enlargedUntil)
            RestoreScale();

        CombatManager combat = CombatManager.Instance;
        if (combat == null || enemy == null || enemy.IsDead || enemy.IsJumping ||
            enemy.CurrentTile == null || enemy.CurrentTile.Index != combat.AttackTileIndex)
        {
            waitedAtTile = 0f;
            Unregister();
            return;
        }

        waitedAtTile += Time.deltaTime;
        if (waitedAtTile < combat.AttackPause)
        {
            Unregister();
            return;
        }

        if (!waiting.Contains(this))
        {
            readySince = Time.time;
            waiting.Add(this);
        }

        // Enemies attack in the order they became ready so none is starved by update order.
        if (!combat.CanAttack || !IsFirstInQueue())
            return;

        int damage = enemy.Attack > 0f ? Mathf.Max(1, Mathf.RoundToInt(enemy.Attack)) : 0;
        if (damage > 0 && combat.TryAttack(damage))
        {
            Unregister();
            waitedAtTile = 0f;
            Enlarge(combat);
        }
    }

    private bool IsFirstInQueue()
    {
        for (int i = 0; i < waiting.Count; i++)
        {
            EnemyAttack other = waiting[i];
            if (other == null || other == this)
                continue;
            if (other.readySince < readySince ||
                (other.readySince == readySince && other.GetInstanceID() < GetInstanceID()))
                return false;
        }
        return true;
    }

    private void Unregister()
    {
        waiting.Remove(this);
    }

    private void Enlarge(CombatManager combat)
    {
        if (visual == null)
        {
            visual = transform.Find("Sprite");
            if (visual == null)
                return;
        }

        if (!enlarged)
            baseScale = visual.localScale;

        visual.localScale = baseScale * combat.AttackScaleMultiplier;
        enlargedUntil = Time.time + combat.AttackScaleDuration;
        enlarged = true;
    }

    private void RestoreScale()
    {
        if (enlarged && visual != null)
            visual.localScale = baseScale;
        enlarged = false;
    }
}
