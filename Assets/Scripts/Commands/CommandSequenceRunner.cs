using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommandSequenceRunner : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private InstructionParser parser;

    [SerializeField]
    private CommandExecutor executor;

    [SerializeField]
    private GameRulesConfig rules;

    public bool IsExecuting
    {
        get;
        private set;
    }

    public bool LastPlanAccepted
    {
        get;
        private set;
    }

    public string LastError
    {
        get;
        private set;
    }

    // Phase 5 UI events. Existing Phase 3-4 code can ignore these.
    public event Action<string> PlanRejected;
    public event Action<IReadOnlyList<RuntimeInstruction>> PlanStarted;
    public event Action<int, int, RuntimeInstruction> InstructionStarted;
    public event Action<int, int, RuntimeInstruction, bool> InstructionFinished;
    public event Action<bool, string> PlanFinished;

    public void SubmitPlan(string input)
    {
        if (IsExecuting)
        {
            RejectPlan("A command plan is already executing.");
            return;
        }

        if (parser == null)
        {
            RejectPlan("InstructionParser is not assigned.");
            return;
        }

        if (executor == null)
        {
            RejectPlan("CommandExecutor is not assigned.");
            return;
        }

        if (!parser.TryParsePlan(
                input,
                out List<RuntimeInstruction> instructions,
                out string errorMessage))
        {
            RejectPlan(errorMessage);
            return;
        }

        LastPlanAccepted = true;
        LastError = string.Empty;

        StartCoroutine(RunPlan(instructions));
    }

    private void RejectPlan(string errorMessage)
    {
        LastPlanAccepted = false;
        LastError = errorMessage;

        Debug.LogWarning(
            "Command plan rejected: " +
            errorMessage
        );

        PlanRejected?.Invoke(errorMessage);
    }

    private IEnumerator RunPlan(
        List<RuntimeInstruction> instructions)
    {
        IsExecuting = true;
        PlanStarted?.Invoke(instructions);

        Debug.Log(
            "Executing command plan with " +
            instructions.Count +
            " instruction(s)."
        );

        bool completedAllInstructions = true;
        string finishMessage = "Command plan finished.";

        for (int i = 0; i < instructions.Count; i++)
        {
            RuntimeInstruction instruction =
                instructions[i];

            InstructionStarted?.Invoke(
                i,
                instructions.Count,
                instruction
            );

            Debug.Log(
                "Executing instruction " +
                (i + 1) +
                "/" +
                instructions.Count +
                ": " +
                instruction.type
            );

            yield return executor.Execute(instruction);

            bool succeeded =
                executor.LastInstructionSucceeded;

            InstructionFinished?.Invoke(
                i,
                instructions.Count,
                instruction,
                succeeded
            );

            if (!succeeded)
            {
                completedAllInstructions = false;

                finishMessage =
                    "Plan stopped at instruction " +
                    (i + 1) +
                    ": " +
                    instruction.originalText;

                Debug.LogWarning(
                    "Instruction failed: " +
                    instruction.type
                );

                if (rules == null ||
                    rules.failedActionStopsSequence)
                {
                    Debug.LogWarning(
                        "Remaining instructions cancelled."
                    );

                    break;
                }
            }
        }

        IsExecuting = false;

        if (completedAllInstructions)
        {
            finishMessage = "Command plan finished.";
        }

        Debug.Log(finishMessage);
        PlanFinished?.Invoke(
            completedAllInstructions,
            finishMessage
        );
    }
}
