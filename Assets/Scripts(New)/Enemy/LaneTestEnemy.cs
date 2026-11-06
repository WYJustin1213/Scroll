using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class LaneTestEnemy : MonoBehaviour
{
    [SerializeField] private EnemyDefinition definition;
    [SerializeField] private BoxCollider2D hitbox;
    [SerializeField] private LaneManager lane;
    [SerializeField, Min(0)] private int startTileIndex;

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

    public void TakeDamage(int amount)
    {
        if (dead || amount <= 0)
            return;

        currentHealth = Mathf.Max(0, Health - amount);
        healthInitialized = true;
        if (currentHealth == 0)
        {
            dead = true;
            Destroy(gameObject);
        }
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

        moveTimer += Time.deltaTime;
        if (moveTimer < definition.MoveInterval)
            return;

        moveTimer -= definition.MoveInterval;
        LaneTile next = lane.GetNextTile(currentTile);
        if (next == null)
            return;

        currentTile = next;
        transform.position = currentTile.StandPosition;
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
