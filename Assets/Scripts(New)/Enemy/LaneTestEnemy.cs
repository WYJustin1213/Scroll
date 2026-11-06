using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class LaneTestEnemy : MonoBehaviour
{
    [SerializeField] private Sprite sprite;
    [SerializeField] private BoxCollider2D hitbox;
    [SerializeField] private LaneManager lane;
    [SerializeField, Min(0)] private int startTileIndex;
    [SerializeField, Min(0.01f)] private float moveInterval = 1f;
    [SerializeField, Min(1)] private int health = 10;
    [SerializeField, Min(0f)] private float attack = 1f;
    [SerializeField] private float spriteOffset = 1.1f;

    [SerializeField] private GameObject mana;
    [SerializeField] private int manaAmount = 1;

    private SpriteRenderer spriteRenderer;
    private LaneTile currentTile;
    private float moveTimer;
    private bool dead;

    public int Health => health;
    public float Attack => attack;
    public LaneManager Lane => lane;
    public LaneTile CurrentTile => currentTile;
    public bool IsDead => dead;

    public void TakeDamage(int amount)
    {
        if (dead || amount <= 0)
            return;

        health = Mathf.Max(0, health - amount);
        if (health == 0)
        {
            dead = true;
            for(int i = 0; i < manaAmount; i++){
                Instantiate(mana, transform.position, Quaternion.identity);
            }
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

        moveTimer += Time.deltaTime;
        if (moveTimer < moveInterval)
            return;

        moveTimer -= moveInterval;
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

        visual.localPosition = Vector3.up * spriteOffset;
        return changed;
    }

    private void UpdateSprite()
    {
        if (spriteRenderer == null)
            return;

        spriteRenderer.sprite = sprite;
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
