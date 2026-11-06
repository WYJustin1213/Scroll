using UnityEngine;

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
    [SerializeField, Min(0)] private int manaAmount = 1;

    public Sprite Sprite => sprite;
    public float MoveInterval => moveInterval;
    public int Health => health;
    public float Attack => attack;
    public int Drop => drop;
    public float SpriteOffset => spriteOffset;
    public GameObject ManaPrefab => manaPrefab;
    public int ManaAmount => manaAmount;
}
