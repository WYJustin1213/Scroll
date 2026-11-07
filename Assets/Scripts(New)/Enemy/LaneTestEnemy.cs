using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class LaneTestEnemy : MonoBehaviour
{
    [SerializeField] private EnemyDefinition definition;
    [SerializeField] private BoxCollider2D hitbox;
    [SerializeField] private LaneManager lane;
    [SerializeField, Min(0)] private int startTileIndex;
    [SerializeField] private float manaDropYOffset;

    private SpriteRenderer spriteRenderer;
    private LaneTile currentTile;
    private float moveTimer;
    private bool dead;
    private int currentHealth;
    private bool healthInitialized;

    public EnemyDefinition Definition => definition;
    public int Health => healthInitialized ? currentHealth : (definition != null ? definition.Health : 0);
    public float Attack => definition != null ? definition.Attack : 0f;
    public int Drop => definition != null ? definition.Drop : 0;
    public LaneManager Lane => lane;
    public LaneTile CurrentTile => currentTile;
    public bool IsDead => dead;

    public void TakeDamage(int amount, SpellElement element = SpellElement.None)
    {
        if (dead || amount <= 0)
            return;

        if (definition != null && definition.IsImmuneTo(element))
            return;

        // Immunity takes precedence over weakness if both are set for the same element.
        if (definition != null && definition.IsWeakTo(element))
            amount = Mathf.Max(amount, Health);

        currentHealth = Mathf.Max(0, Health - amount);
        healthInitialized = true;
        if (currentHealth == 0)
        {
            dead = true;
            if (definition != null && definition.Ability)
            {
                EnemyAbility[] abilities = GetComponents<EnemyAbility>();
                for (int i = 0; i < abilities.Length; i++)
                {
                    if (abilities[i] != null && abilities[i].isActiveAndEnabled)
                        abilities[i].OnDeath(this);
                }
            }

            if (definition != null && definition.ManaPrefab != null)
            {
                for (int i = 0; i < definition.ManaAmount; i++)
                {
                    Vector3 manaPosition = transform.position + Vector3.up * manaDropYOffset;
                    Instantiate(definition.ManaPrefab, manaPosition, Quaternion.identity);
                }
            }

            Destroy(gameObject);
        }
    }

    public bool InitializeAtTile(LaneManager spawnLane, LaneTile spawnTile)
    {
        if (spawnLane == null || spawnTile == null || spawnTile.Lane != spawnLane)
        {
            Debug.LogError("Cannot initialize an enemy at an invalid lane tile.", this);
            return false;
        }

        lane = spawnLane;
        startTileIndex = spawnTile.Index;
        currentTile = spawnTile;
        moveTimer = 0f;
        transform.position = spawnTile.StandPosition;
        return true;
    }

    private void OnEnable()
    {
        bool sceneChanged = EnsureSpriteRenderer();
        UpdateSprite();
        SnapToStartTile();

#if UNITY_EDITOR
        if (sceneChanged && !Application.isPlaying && gameObject.scene.IsValid())
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    private void OnValidate()
    {
        EnsureSpriteRenderer();
        UpdateSprite();
        if (!Application.isPlaying)
            SnapToStartTile();
    }

    private void Update()
    {
        if (!Application.isPlaying || lane == null || currentTile == null)
            return;

        if (definition == null)
            return;

        float moveInterval = definition.MoveInterval;
        moveTimer += Time.deltaTime;
        while (moveTimer >= moveInterval)
        {
            LaneTile next = lane.GetNextTile(currentTile);
            if (next == null)
            {
                moveTimer = 0f;
                transform.position = currentTile.StandPosition;
                return;
            }

            currentTile = next;
            moveTimer -= moveInterval;
        }

        LaneTile destination = lane.GetNextTile(currentTile);
        transform.position = destination != null
            ? Vector3.Lerp(currentTile.StandPosition, destination.StandPosition, moveTimer / moveInterval)
            : currentTile.StandPosition;
    }

    private void SnapToStartTile()
    {
        if (lane == null)
            return;

        lane.CollectTiles();
        currentTile = lane.GetTile(startTileIndex);
        if (currentTile != null)
            transform.position = currentTile.StandPosition;
    }

    private bool EnsureSpriteRenderer()
    {
        Transform visual = transform.Find("Sprite");
        if (visual == null)
        {
            Transform placeholderVisual = transform.Find("Enemy Placeholder Visual");
            if (placeholderVisual != null)
                DestroyOwnedObject(placeholderVisual.gameObject);

            GameObject visualObject = new GameObject("Sprite");
            visualObject.transform.SetParent(transform, false);
            visual = visualObject.transform;
        }

        bool changed = false;
        spriteRenderer = visual.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = visual.gameObject.AddComponent<SpriteRenderer>();
            changed = true;
        }

        visual.localPosition = Vector3.up * (definition != null ? definition.SpriteOffset : 0f);
        return changed;
    }

    private void UpdateSprite()
    {
        if (spriteRenderer == null)
            return;

        spriteRenderer.sprite = definition != null ? definition.Sprite : null;
    }

    private static void DestroyOwnedObject(Object ownedObject)
    {
        if (ownedObject == null)
            return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            DestroyImmediate(ownedObject);
            return;
        }
#endif

        Destroy(ownedObject);
    }
}
