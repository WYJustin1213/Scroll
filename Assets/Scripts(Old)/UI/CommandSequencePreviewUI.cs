using System.Collections.Generic;
using UnityEngine;

public class CommandSequencePreviewUI : MonoBehaviour
{
    [SerializeField]
    private Transform contentRoot;

    [SerializeField]
    private CommandPreviewCardUI cardPrefab;

    private readonly List<CommandPreviewCardUI> cards =
        new List<CommandPreviewCardUI>();

    public void ShowPlan(
        IReadOnlyList<RuntimeInstruction> instructions)
    {
        Clear();

        if (contentRoot == null ||
            cardPrefab == null ||
            instructions == null)
        {
            return;
        }

        for (int i = 0; i < instructions.Count; i++)
        {
            CommandPreviewCardUI card =
                Instantiate(cardPrefab, contentRoot);

            card.Setup(
                i,
                instructions[i],
                i < instructions.Count - 1
            );

            cards.Add(card);
        }
    }

    public void SetState(
        int index,
        CommandPreviewCardUI.CardState state)
    {
        if (index < 0 || index >= cards.Count)
            return;

        if (cards[index] == null)
            return;

        cards[index].SetState(state);
    }

    public void ResetToPending()
    {
        foreach (CommandPreviewCardUI card in cards)
        {
            if (card != null)
            {
                card.SetState(
                    CommandPreviewCardUI.CardState.Pending
                );
            }
        }
    }

    public void Clear()
    {
        for (int i = cards.Count - 1; i >= 0; i--)
        {
            if (cards[i] != null)
            {
                Destroy(cards[i].gameObject);
            }
        }

        cards.Clear();
    }
}
