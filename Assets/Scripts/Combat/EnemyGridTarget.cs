using UnityEngine;

public class EnemyGridTarget : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField]
    private bool snapToGridOnStart = true;

    [Header("Death")]
    [SerializeField]
    private bool destroyGameObjectOnDeath = true;

    public bool IsDead { get; private set; }

    public Vector2Int GridCell
    {
        get
        {
            if (EnemyManager.Instance == null)
                return Vector2Int.zero;

            return EnemyManager.Instance.WorldToCell(
                transform.position
            );
        }
    }

    private bool hasStarted;

    private void Start()
    {
        hasStarted = true;
        RegisterWithManager();

        if (snapToGridOnStart &&
            EnemyManager.Instance != null)
        {
            transform.position =
                EnemyManager.Instance.SnapWorldToGrid(
                    transform.position
                );
        }
    }

    private void OnEnable()
    {
        if (hasStarted)
        {
            RegisterWithManager();
        }
    }

    private void OnDisable()
    {
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.Unregister(this);
        }
    }

    private void RegisterWithManager()
    {
        if (IsDead)
            return;

        if (EnemyManager.Instance == null)
        {
            Debug.LogWarning(
                name +
                ": no EnemyManager exists in the scene.",
                this
            );

            return;
        }

        EnemyManager.Instance.Register(this);
    }

    public void Kill()
    {
        if (IsDead)
            return;

        IsDead = true;

        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.Unregister(this);
        }

        Debug.Log(name + " was killed.", this);

        if (destroyGameObjectOnDeath)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
