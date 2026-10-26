using System.Collections;
using UnityEngine;

public class CommandExecutor : MonoBehaviour
{
    [Header("Player")]
    [SerializeField]
    private PlayerController player;

    [SerializeField]
    private PlayerSpellController spellController;

    [Header("Arcane Eye")]
    [SerializeField]
    private ArcaneEyeTargetFinder arcaneEyeTargetFinder;

    [Header("Enemies")]
    [SerializeField]
    private EnemyManager enemyManager;

    public bool LastInstructionSucceeded
    {
        get;
        private set;
    }

    public IEnumerator Execute(
        RuntimeInstruction instruction)
    {
        LastInstructionSucceeded = true;

        if (instruction == null)
        {
            LastInstructionSucceeded = false;
            yield break;
        }

        switch (instruction.type)
        {
            case InstructionType.MoveForward:
                yield return player.MoveForward();

                LastInstructionSucceeded =
                    player.LastActionSucceeded;
                break;

            case InstructionType.MoveBack:
                yield return player.MoveBack();

                LastInstructionSucceeded =
                    player.LastActionSucceeded;
                break;

            case InstructionType.MoveLeft:
                yield return player.MoveLeft();

                LastInstructionSucceeded =
                    player.LastActionSucceeded;
                break;

            case InstructionType.MoveRight:
                yield return player.MoveRight();

                LastInstructionSucceeded =
                    player.LastActionSucceeded;
                break;

            case InstructionType.FireballLeft:
                yield return spellController.CastFireball(
                    PlayerSpellController
                        .FireballDirection.Left
                );

                // A missed fireball is still a successfully
                // executed command.
                LastInstructionSucceeded = true;
                break;

            case InstructionType.FireballForward:
                yield return spellController.CastFireball(
                    PlayerSpellController
                        .FireballDirection.Forward
                );

                LastInstructionSucceeded = true;
                break;

            case InstructionType.FireballRight:
                yield return spellController.CastFireball(
                    PlayerSpellController
                        .FireballDirection.Right
                );

                LastInstructionSucceeded = true;
                break;

            case InstructionType.TeleportationSpell:
                yield return
                    spellController.TeleportBackward();

                LastInstructionSucceeded =
                    player.LastActionSucceeded;
                break;

            case InstructionType.ArcaneEye:
                yield return ExecuteArcaneEye();
                break;

            case InstructionType.PowerOverwhelming:
                EnemyManager resolvedEnemyManager =
                    enemyManager != null
                        ? enemyManager
                        : EnemyManager.Instance;

                if (resolvedEnemyManager == null)
                {
                    Debug.LogError(
                        "Power Overwhelming failed: no EnemyManager exists.",
                        this
                    );

                    LastInstructionSucceeded = false;
                    yield break;
                }

                resolvedEnemyManager.KillAllEnemies();
                LastInstructionSucceeded = true;
                yield return null;
                break;

            default:
                Debug.LogError(
                    "Unsupported instruction: " +
                    instruction.type
                );

                LastInstructionSucceeded = false;
                break;
        }
    }

    private IEnumerator ExecuteArcaneEye()
    {
        if (arcaneEyeTargetFinder == null)
        {
            Debug.LogWarning(
                "Arcane Eye has no target finder."
            );

            LastInstructionSucceeded = false;
            yield break;
        }

        Transform target =
            arcaneEyeTargetFinder.FindTarget(
                player.transform.position
            );

        if (target == null)
        {
            Debug.Log(
                "Arcane Eye found no enemy."
            );

            LastInstructionSucceeded = false;
            yield break;
        }

        yield return
            spellController.UseArcaneEye(target);

        LastInstructionSucceeded =
            player.LastActionSucceeded;
    }
}
