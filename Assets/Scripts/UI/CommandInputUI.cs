using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CommandInputUI : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private TMP_InputField inputField;

    [SerializeField]
    private Button executeButton;

    [SerializeField] private TMP_Text statusText;

    [Header("Command System")]
    [SerializeField] private InstructionParser parser;
    [SerializeField] Manager manager;

    [SerializeField]
    private CommandSequenceRunner sequenceRunner;

    [Header("Preview")]
    [SerializeField] private CommandSequencePreviewUI sequencePreview;

    [Header("Startup")]
    [SerializeField] private bool focusInputOnStart = true;

    [SerializeField]
    private string idleMessage = "Type commands separated with +";

    private bool inputChangedDuringExecution;

    private void Awake()
    {
        ConfigureInputField();
    }

    private void OnEnable()
    {
        if (inputField != null)
        {
            inputField.onValueChanged.AddListener(
                HandleInputChanged
            );

            inputField.onSubmit.AddListener(
                HandleInputSubmitted
            );
        }

        if (executeButton != null)
        {
            executeButton.onClick.AddListener(
                HandleExecuteClicked
            );
        }

        if (sequenceRunner != null)
        {
            sequenceRunner.PlanRejected +=
                HandlePlanRejected;

            sequenceRunner.PlanStarted +=
                HandlePlanStarted;

            sequenceRunner.InstructionStarted +=
                HandleInstructionStarted;

            sequenceRunner.InstructionFinished +=
                HandleInstructionFinished;

            sequenceRunner.PlanFinished +=
                HandlePlanFinished;
        }
    }

    private void Start()
    {
        RefreshPreviewAndValidation();

        if (focusInputOnStart)
        {
            StartCoroutine(FocusInputNextFrame(false));
        }
    }

    private void OnDisable()
    {
        if (inputField != null)
        {
            inputField.onValueChanged.RemoveListener(
                HandleInputChanged
            );

            inputField.onSubmit.RemoveListener(
                HandleInputSubmitted
            );
        }

        if (executeButton != null)
        {
            executeButton.onClick.RemoveListener(
                HandleExecuteClicked
            );
        }

        if (sequenceRunner != null)
        {
            sequenceRunner.PlanRejected -=
                HandlePlanRejected;

            sequenceRunner.PlanStarted -=
                HandlePlanStarted;

            sequenceRunner.InstructionStarted -=
                HandleInstructionStarted;

            sequenceRunner.InstructionFinished -=
                HandleInstructionFinished;

            sequenceRunner.PlanFinished -=
                HandlePlanFinished;
        }
    }

    private void ConfigureInputField()
    {
        if (inputField == null)
            return;

        // These settings make the field a real editable command line.
        // TMP_InputField supplies the visible blinking caret/text cursor.
        inputField.readOnly = false;
        inputField.lineType = TMP_InputField.LineType.SingleLine;
        inputField.contentType = TMP_InputField.ContentType.Standard;
        inputField.characterLimit = 0;
    }

    private void HandleInputChanged(string value)
    {
        if (sequenceRunner != null &&
            sequenceRunner.IsExecuting)
        {
            inputChangedDuringExecution = true;

            SetStatus(
                "Executing. Your edits will be used the next time you submit."
            );

            return;
        }

        RefreshPreviewAndValidation();
    }

    private void HandleInputSubmitted(string value)
    {
        TrySubmitCurrentText();
        StartCoroutine(FocusInputNextFrame(true));
    }

    private void HandleExecuteClicked()
    {
        TrySubmitCurrentText();
        StartCoroutine(FocusInputNextFrame(true));
    }

    private void TrySubmitCurrentText()
    {
        if (sequenceRunner == null)
        {
            SetStatus(
                "CommandSequenceRunner is not assigned."
            );

            return;
        }

        if (parser == null)
        {
            SetStatus(
                "InstructionParser is not assigned."
            );

            return;
        }

        if (sequenceRunner.IsExecuting)
        {
            SetStatus(
                "A plan is already executing."
            );

            return;
        }

        string text =
            inputField != null
                ? inputField.text
                : string.Empty;

        if (!parser.TryParsePlan(
                text,
                out List<RuntimeInstruction> instructions,
                out string errorMessage))
        {
            if (sequencePreview != null)
            {
                sequencePreview.Clear();
            }

            SetStatus(errorMessage);
            UpdateExecuteButton(false);
            return;
        }

        if (sequencePreview != null)
        {
            sequencePreview.ShowPlan(instructions);
        }

        sequenceRunner.SubmitPlan(text);
    }

    private void RefreshPreviewAndValidation()
    {
        if (inputField == null || parser == null)
        {
            UpdateExecuteButton(false);
            return;
        }

        string text = inputField.text;

        if (string.IsNullOrWhiteSpace(text))
        {
            if (sequencePreview != null)
            {
                sequencePreview.Clear();
            }

            SetStatus(idleMessage);
            UpdateExecuteButton(false);
            return;
        }

        if (parser.TryParsePlan(
                text,
                out List<RuntimeInstruction> instructions,
                out string errorMessage))
        {
            if (sequencePreview != null)
            {
                sequencePreview.ShowPlan(instructions);
            }

            

            SetStatus(
                "Ready: " +
                instructions.Count +
                " instruction" +
                (instructions.Count == 1 ? "" : "s")
            );
            
            UpdateExecuteButton(true);
            
        }
        else
        {
            if (sequencePreview != null)
            {
                sequencePreview.Clear();
            }

            SetStatus(errorMessage);
            UpdateExecuteButton(false);
        }
    }

    private void HandlePlanRejected(string errorMessage)
    {
        SetStatus(errorMessage);
        UpdateExecuteButton(false);
    }

    private void HandlePlanStarted(
        IReadOnlyList<RuntimeInstruction> instructions)
    {
        manager.UIVisible();

        inputChangedDuringExecution = false;

        if (sequencePreview != null)
        {
            sequencePreview.ShowPlan(instructions);
            sequencePreview.ResetToPending();
        }

        SetStatus(
            "Executing " +
            instructions.Count +
            " instruction" +
            (instructions.Count == 1 ? "" : "s") +
            "..."
        );

        // Keep the input field editable. Only prevent a second execution.
        UpdateExecuteButton(false);
    }

    private void HandleInstructionStarted(
        int index,
        int total,
        RuntimeInstruction instruction)
    {
        if (sequencePreview != null)
        {
            sequencePreview.SetState(
                index,
                CommandPreviewCardUI.CardState.Executing
            );
        }

        SetStatus(
            "Executing " +
            (index + 1) +
            "/" +
            total +
            ": " +
            instruction.originalText
        );
    }

    private void HandleInstructionFinished(
        int index,
        int total,
        RuntimeInstruction instruction,
        bool succeeded)
    {
        if (sequencePreview == null)
            return;

        sequencePreview.SetState(
            index,
            succeeded
                ? CommandPreviewCardUI.CardState.Succeeded
                : CommandPreviewCardUI.CardState.Failed
        );
    }

    private void HandlePlanFinished(
        bool completedAllInstructions,
        string message)
    {
        if (inputChangedDuringExecution)
        {
            inputChangedDuringExecution = false;
            RefreshPreviewAndValidation();
            return;
        }

        SetStatus(message);

        bool canExecuteAgain =
            CurrentTextIsValid();

        UpdateExecuteButton(canExecuteAgain);
    }

    private bool CurrentTextIsValid()
    {
        if (inputField == null || parser == null)
            return false;

        if (string.IsNullOrWhiteSpace(inputField.text))
            return false;

        return parser.TryParsePlan(
            inputField.text,
            out _,
            out _
        );
    }

    private void UpdateExecuteButton(bool textIsValid)
    {
        if (executeButton == null)
            return;

        bool isExecuting =
            sequenceRunner != null &&
            sequenceRunner.IsExecuting;
        
        executeButton.interactable =
            textIsValid && !isExecuting;
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private IEnumerator FocusInputNextFrame(
        bool moveCaretToEnd)
    {
        yield return null;

        if (inputField == null)
            yield break;

        inputField.ActivateInputField();

        if (moveCaretToEnd)
        {
            int end = inputField.text.Length;
            inputField.caretPosition = end;
            inputField.selectionAnchorPosition = end;
            inputField.selectionFocusPosition = end;
        }
    }
}
