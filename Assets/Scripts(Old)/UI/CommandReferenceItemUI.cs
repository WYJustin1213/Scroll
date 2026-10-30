using System.Text;
using TMPro;
using UnityEngine;

public class CommandReferenceItemUI : MonoBehaviour
{
    [SerializeField]
    private TMP_Text commandText;

    [SerializeField]
    private TMP_Text descriptionText;

    [SerializeField]
    private TMP_Text aliasesText;

    public void Setup(CommandDefinition definition)
    {
        if (definition == null)
            return;

        if (commandText != null)
        {
            commandText.text =
                string.IsNullOrWhiteSpace(definition.commandText)
                    ? definition.displayName
                    : definition.commandText;
        }

        if (descriptionText != null)
        {
            descriptionText.text = definition.description;
        }

        if (aliasesText != null)
        {
            if (definition.aliases == null ||
                definition.aliases.Count == 0)
            {
                aliasesText.gameObject.SetActive(false);
            }
            else
            {
                aliasesText.gameObject.SetActive(true);

                StringBuilder builder = new StringBuilder();
                builder.Append("Aliases: ");

                for (int i = 0;
                     i < definition.aliases.Count;
                     i++)
                {
                    if (i > 0)
                        builder.Append(", ");

                    builder.Append(definition.aliases[i]);
                }

                aliasesText.text = builder.ToString();
            }
        }
    }
}
