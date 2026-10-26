using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "CommandDefinition",
    menuName = "Command FPS/Command Definition"
)]
public class CommandDefinition : ScriptableObject
{
    [Header("Display")]
    public string displayName;

    [TextArea(2, 4)]
    public string description;

    [Header("Command Text")]
    public string commandText;

    public List<string> aliases = new List<string>();

    [Header("Execution")]
    public InstructionType instructionType;

    public bool Matches(string input)
    {
        string normalizedInput =
            CommandTextNormalizer.Normalize(input);

        if (normalizedInput ==
            CommandTextNormalizer.Normalize(commandText))
        {
            return true;
        }

        foreach (string alias in aliases)
        {
            if (normalizedInput ==
                CommandTextNormalizer.Normalize(alias))
            {
                return true;
            }
        }

        return false;
    }
}
