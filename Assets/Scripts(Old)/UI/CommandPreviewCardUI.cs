using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CommandPreviewCardUI : MonoBehaviour
{
    public enum CardState
    {
        Pending,
        Executing,
        Succeeded,
        Failed
    }

    [Header("Text")]
    [SerializeField]
    private TMP_Text numberText;

    [SerializeField]
    private TMP_Text commandText;

    [Header("Visual State")]
    [SerializeField]
    private Image stateImage;

    [SerializeField]
    private Color pendingColor = new Color(0.35f, 0.35f, 0.35f, 1f);

    [SerializeField]
    private Color executingColor = new Color(0.95f, 0.75f, 0.2f, 1f);

    [SerializeField]
    private Color succeededColor = new Color(0.25f, 0.75f, 0.35f, 1f);

    [SerializeField]
    private Color failedColor = new Color(0.85f, 0.25f, 0.25f, 1f);

    [Header("Optional")]
    [SerializeField]
    private GameObject arrowAfter;

    public void Setup(
        int index,
        RuntimeInstruction instruction,
        bool showArrowAfter)
    {
        if (numberText != null)
        {
            numberText.text = (index + 1).ToString();
        }

        if (commandText != null)
        {
            commandText.text = instruction != null
                ? instruction.originalText
                : "Unknown instruction";
        }

        if (arrowAfter != null)
        {
            arrowAfter.SetActive(showArrowAfter);
        }

        SetState(CardState.Pending);
    }

    public void SetState(CardState state)
    {
        if (stateImage == null)
            return;

        switch (state)
        {
            case CardState.Executing:
                stateImage.color = executingColor;
                break;

            case CardState.Succeeded:
                stateImage.color = succeededColor;
                break;

            case CardState.Failed:
                stateImage.color = failedColor;
                break;

            default:
                stateImage.color = pendingColor;
                break;
        }
    }
}
