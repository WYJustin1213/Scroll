using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class LaneTestEnemy : MonoBehaviour
{
    [SerializeField] private Sprite sprite;
    [SerializeField] private BoxCollider2D hitbox;
    [SerializeField] private Transform target;
    [SerializeField, Min(0f)] private float speed = 2f;
    [SerializeField, Min(1)] private int health = 10;
    [SerializeField, Min(0f)] private float attack = 1f;
    [SerializeField] private float spriteOffset = 1.1f;

    private SpriteRenderer spriteRenderer;

    public float Speed => speed;
    public int Health => health;
    public float Attack => attack;

    private void OnEnable()
    {
        bool sceneChanged = EnsureSpriteRenderer();
        UpdateSprite();

#if UNITY_EDITOR
        if (sceneChanged && !Application.isPlaying && gameObject.scene.IsValid())
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    public bool SetTarget(Transform target)
    {
        if (this.target == target)
            return false;

        this.target = target;
        return true;
    }

    private void OnValidate()
    {
        EnsureSpriteRenderer();
        UpdateSprite();
    }

    private void Update()
    {
        if (!Application.isPlaying || target == null || speed <= 0f)
            return;

        Vector3 position = transform.position;
        Vector3 targetPosition = new Vector3(position.x, position.y, target.position.z);
        transform.position = Vector3.MoveTowards(position, targetPosition, speed * Time.deltaTime);
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
