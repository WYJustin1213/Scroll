using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Rules")]
    [SerializeField] private GameRulesConfig rules;

    [Header("Collision")]
    [SerializeField] private LayerMask obstacleMask;

    [Header("References")]
    [SerializeField] private Transform cameraRoot;
    [SerializeField] private PlayerHealth health;

    [Header("Starting Direction")]
    [SerializeField] private CardinalDirection startingDirection = CardinalDirection.North;

    private CharacterController characterController;

    public CardinalDirection Facing { get; private set; }
    public bool IsBusy { get; private set; }
    public bool LastActionSucceeded { get; private set; }

    public float CellSize => rules != null ? rules.cellSize : 2f;
    public Vector3 ForwardVector => FacingToVector(Facing);

    public Vector3 RightVector
    {
        get
        {
            Vector3 forward = ForwardVector;
            return new Vector3(forward.z, 0f, -forward.x);
        }
    }

    public Vector3 LeftVector => -RightVector;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        Facing = startingDirection;

        if (health == null)
            health = GetComponent<PlayerHealth>();
    }

    private void Start()
    {
        if (!ValidateSetup())
            return;

        SnapPositionToGrid();
        ApplyFacingInstantly();
    }

    // -------------------------------------------------
    // PUBLIC MOVEMENT ACTIONS
    // -------------------------------------------------

    public IEnumerator MoveForward()
    {
        yield return MoveAndFace(ForwardVector);
    }

    public IEnumerator MoveBack()
    {
        yield return MoveAndFace(-ForwardVector);
    }

    public IEnumerator MoveLeft()
    {
        yield return MoveAndFace(LeftVector);
    }

    public IEnumerator MoveRight()
    {
        yield return MoveAndFace(RightVector);
    }

    // -------------------------------------------------
    // NORMAL GRID MOVEMENT
    // -------------------------------------------------

    private IEnumerator MoveAndFace(Vector3 direction)
    {
        if (!CanBeginAction())
        {
            LastActionSucceeded = false;
            yield break;
        }

        direction = direction.normalized;

        // IMPORTANT: Check first, rotate second.
        // That guarantees a blocked move leaves both position and facing unchanged.
        if (IsBlocked(direction))
        {
            LastActionSucceeded = false;
            Debug.Log("Movement blocked.", this);
            yield break;
        }

        IsBusy = true;
        LastActionSucceeded = true;

        CardinalDirection newFacing = VectorToFacing(direction);
        yield return RotateTo(newFacing);

        Vector3 startPosition = transform.position;
        Vector3 targetPosition = SnapWorldPositionToGrid(
            startPosition + direction * rules.cellSize
        );

        float duration = Mathf.Max(0.001f, rules.moveDuration);
        float timer = 0f;

        while (timer < duration)
        {
            if (health != null && health.IsDead)
            {
                LastActionSucceeded = false;
                break;
            }

            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);

            Vector3 desiredPosition = Vector3.Lerp(
                startPosition,
                targetPosition,
                t
            );

            Vector3 movement = desiredPosition - transform.position;
            characterController.Move(movement);

            yield return null;
        }

        if (LastActionSucceeded)
        {
            characterController.Move(targetPosition - transform.position);
        }

        IsBusy = false;
    }

    // -------------------------------------------------
    // TELEPORT SUPPORT
    // -------------------------------------------------

    public IEnumerator TeleportBackward()
    {
        if (!CanBeginAction())
        {
            LastActionSucceeded = false;
            yield break;
        }

        Vector3 direction = -ForwardVector;

        if (IsBlocked(direction))
        {
            LastActionSucceeded = false;
            Debug.Log("Teleport blocked.", this);
            yield break;
        }

        IsBusy = true;
        LastActionSucceeded = true;

        Vector3 targetPosition = SnapWorldPositionToGrid(
            transform.position + direction * rules.cellSize
        );

        // Teleport is intentionally instantaneous and does NOT change Facing.
        characterController.enabled = false;
        transform.position = targetPosition;
        characterController.enabled = true;

        IsBusy = false;
        yield return null;
    }

    // -------------------------------------------------
    // ARCANE EYE SUPPORT
    // -------------------------------------------------

    public IEnumerator FaceToward(Vector3 worldPosition)
    {
        if (!CanBeginAction())
        {
            LastActionSucceeded = false;
            yield break;
        }

        Vector3 offset = worldPosition - transform.position;
        offset.y = 0f;

        if (offset.sqrMagnitude < 0.001f)
        {
            LastActionSucceeded = false;
            yield break;
        }

        Vector3 cardinalDirection;

        if (Mathf.Abs(offset.x) >= Mathf.Abs(offset.z))
        {
            cardinalDirection = new Vector3(
                Mathf.Sign(offset.x),
                0f,
                0f
            );
        }
        else
        {
            cardinalDirection = new Vector3(
                0f,
                0f,
                Mathf.Sign(offset.z)
            );
        }

        IsBusy = true;
        LastActionSucceeded = true;

        yield return RotateTo(VectorToFacing(cardinalDirection));

        IsBusy = false;
    }

    // -------------------------------------------------
    // ROTATION
    // -------------------------------------------------

    private IEnumerator RotateTo(CardinalDirection direction)
    {
        Facing = direction;

        Vector3 forward = FacingToVector(direction);
        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.LookRotation(forward, Vector3.up);

        float duration = Mathf.Max(0.001f, rules.turnDuration);
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);

            transform.rotation = Quaternion.Slerp(
                startRotation,
                targetRotation,
                t
            );

            yield return null;
        }

        transform.rotation = targetRotation;
    }

    // -------------------------------------------------
    // COLLISION
    // -------------------------------------------------

    public bool IsBlocked(Vector3 direction)
    {
        if (rules == null)
            return true;

        direction.y = 0f;
        direction.Normalize();

        Vector3 center = transform.TransformPoint(characterController.center);

        float halfHeight = Mathf.Max(
            0f,
            characterController.height * 0.5f - characterController.radius
        );

        Vector3 top = center + Vector3.up * halfHeight;
        Vector3 bottom = center - Vector3.up * halfHeight;
        float radius = characterController.radius * 0.9f;

        return Physics.CapsuleCast(
            top,
            bottom,
            radius,
            direction,
            out _,
            rules.cellSize,
            obstacleMask,
            QueryTriggerInteraction.Ignore
        );
    }

    // -------------------------------------------------
    // GRID / DIRECTION HELPERS
    // -------------------------------------------------

    public void SnapPositionToGrid()
    {
        transform.position = SnapWorldPositionToGrid(transform.position);
    }

    private Vector3 SnapWorldPositionToGrid(Vector3 position)
    {
        if (rules == null)
            return position;

        position.x = Mathf.Round(position.x / rules.cellSize) * rules.cellSize;
        position.z = Mathf.Round(position.z / rules.cellSize) * rules.cellSize;
        return position;
    }

    private void ApplyFacingInstantly()
    {
        transform.rotation = Quaternion.LookRotation(
            FacingToVector(Facing),
            Vector3.up
        );
    }

    public static Vector3 FacingToVector(CardinalDirection direction)
    {
        switch (direction)
        {
            case CardinalDirection.North:
                return Vector3.forward;
            case CardinalDirection.East:
                return Vector3.right;
            case CardinalDirection.South:
                return Vector3.back;
            case CardinalDirection.West:
                return Vector3.left;
            default:
                return Vector3.forward;
        }
    }

    public static CardinalDirection VectorToFacing(Vector3 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.z))
        {
            return direction.x > 0f
                ? CardinalDirection.East
                : CardinalDirection.West;
        }

        return direction.z > 0f
            ? CardinalDirection.North
            : CardinalDirection.South;
    }

    private bool CanBeginAction()
    {
        if (IsBusy)
            return false;

        if (health != null && health.IsDead)
            return false;

        return rules != null;
    }

    private bool ValidateSetup()
    {
        bool valid = true;

        if (rules == null)
        {
            Debug.LogError("PlayerController: GameRulesConfig is not assigned.", this);
            valid = false;
        }

        if (cameraRoot == null)
        {
            Debug.LogWarning(
                "PlayerController: CameraRoot is not assigned. Movement will still work because rotation is applied to Player, but assign it for a complete setup.",
                this
            );
        }

        return valid;
    }
}
