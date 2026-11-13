using UnityEngine;

public enum EnemyElement
{
    Fire,
    Water,
    Shock,
    Ice,
    None
}

public enum EnemySpawnTier
{
    Common,
    Rare,
    Epic
}

[CreateAssetMenu(fileName = "EnemyDefinition", menuName = "Scroll/Enemy Definition")]
public class EnemyDefinition : ScriptableObject
{
    [SerializeField] private Sprite sprite;
    [SerializeField, Min(0.01f)] private float moveInterval = 1f;
    [SerializeField, Min(1)] private int health = 10;
    [SerializeField, Min(0f)] private float attack = 1f;
    [SerializeField, Min(1)] private int drop = 1;
    [SerializeField] private float spriteOffset = 1.1f;
    [SerializeField] private GameObject manaPrefab;
    [SerializeField] private bool ability;
    [SerializeField] private EnemySpawnTier spawnTier = EnemySpawnTier.Common;
    public EnemyElement enemyElement;
    [SerializeField] private ElementImmunity immunities = ElementImmunity.None;

    [SerializeField] private ElementWeakness weaknesses = ElementWeakness.None;

    public ElementWeakness Weaknesses => weaknesses;
    public bool IsWeakTo(SpellElement element) => weaknesses.IsWeakTo(element);
    public ElementImmunity Immunities => immunities;
    public bool IsImmuneTo(SpellElement element) => immunities.IsImmuneTo(element);

    public bool Ability => ability;
    public EnemySpawnTier SpawnTier => spawnTier;
    public Sprite Sprite => sprite;
    public float MoveInterval => moveInterval;
    public int Health => health;
    public float Attack => attack;
    public int Drop => drop;
    public float SpriteOffset => spriteOffset;
    public GameObject ManaPrefab => manaPrefab;
}
