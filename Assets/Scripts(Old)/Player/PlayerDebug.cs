using UnityEngine;

/// <summary>
/// Phase 1-2 testing helper only.
/// It deliberately contains NO Update(), WASD input, mouse look, or command parser.
/// Use the component context menu while the game is running.
/// </summary>
public class PlayerActionDebug : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController player;
    [SerializeField] private PlayerSpellController spells;

    [Header("Arcane Eye Test")]
    [SerializeField] private Transform arcaneEyeTarget;

    private void Awake()
    {
        if (player == null)
            player = GetComponent<PlayerController>();

        if (spells == null)
            spells = GetComponent<PlayerSpellController>();
    }

    [ContextMenu("DEBUG/Move Forward")]
    private void DebugMoveForward()
    {
        if (!CanRun() || player == null) return;
        StartCoroutine(player.MoveForward());
    }

    [ContextMenu("DEBUG/Move Back")]
    private void DebugMoveBack()
    {
        if (!CanRun() || player == null) return;
        StartCoroutine(player.MoveBack());
    }

    [ContextMenu("DEBUG/Move Left")]
    private void DebugMoveLeft()
    {
        if (!CanRun() || player == null) return;
        StartCoroutine(player.MoveLeft());
    }

    [ContextMenu("DEBUG/Move Right")]
    private void DebugMoveRight()
    {
        if (!CanRun() || player == null) return;
        StartCoroutine(player.MoveRight());
    }

    [ContextMenu("DEBUG/Teleport Backward")]
    private void DebugTeleportBackward()
    {
        if (!CanRun() || spells == null) return;
        StartCoroutine(spells.TeleportBackward());
    }

    [ContextMenu("DEBUG/Fireball Left")]
    private void DebugFireballLeft()
    {
        if (!CanRun() || spells == null) return;
        StartCoroutine(
            spells.CastFireball(PlayerSpellController.FireballDirection.Left)
        );
    }

    [ContextMenu("DEBUG/Fireball Forward")]
    private void DebugFireballForward()
    {
        if (!CanRun() || spells == null) return;
        StartCoroutine(
            spells.CastFireball(PlayerSpellController.FireballDirection.Forward)
        );
    }

    [ContextMenu("DEBUG/Fireball Right")]
    private void DebugFireballRight()
    {
        if (!CanRun() || spells == null) return;
        StartCoroutine(
            spells.CastFireball(PlayerSpellController.FireballDirection.Right)
        );
    }

    [ContextMenu("DEBUG/Arcane Eye")]
    private void DebugArcaneEye()
    {
        if (!CanRun() || spells == null || arcaneEyeTarget == null)
            return;

        StartCoroutine(spells.UseArcaneEye(arcaneEyeTarget));
    }

    [ContextMenu("DEBUG/Snap Player To Grid")]
    private void DebugSnapToGrid()
    {
        if (!CanRun() || player == null) return;
        player.SnapPositionToGrid();
    }

    private bool CanRun()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Enter Play Mode before using Phase 1-2 debug actions.", this);
            return false;
        }

        return true;
    }
}
