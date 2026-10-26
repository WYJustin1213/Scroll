using UnityEngine;

public class CommandPlanDebugTester : MonoBehaviour
{
    [SerializeField]
    private CommandSequenceRunner sequenceRunner;

    [Header("Temporary Phase 3-4 Test Input")]

    [TextArea(3, 8)]
    [SerializeField]
    private string testPlan =
        "Move forward. + Move left. + Fireball right.";

    [ContextMenu("Run Test Plan")]
    public void RunTestPlan()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Enter Play Mode before running the command plan.");

            return;
        }

        if (sequenceRunner == null)
        {
            Debug.LogError("CommandSequenceRunner is not assigned.");

            return;
        }

        sequenceRunner.SubmitPlan(testPlan);
    }
}
