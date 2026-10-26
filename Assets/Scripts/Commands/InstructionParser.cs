using System.Collections.Generic;
using UnityEngine;

public class InstructionParser : MonoBehaviour
{
    private const string HiddenPowerOverwhelmingCommand =
        "power overwhelming";

    [SerializeField]
    private CommandDatabase commandDatabase;

    public bool TryParsePlan(
        string input,
        out List<RuntimeInstruction> instructions,
        out string errorMessage)
    {
        instructions =
            new List<RuntimeInstruction>();

        errorMessage = string.Empty;

        if (commandDatabase == null)
        {
            errorMessage =
                "CommandDatabase is not assigned.";

            return false;
        }

        if (string.IsNullOrWhiteSpace(input))
        {
            errorMessage =
                "Command plan is empty.";

            return false;
        }

        string[] pieces = input.Split('+');

        for (int i = 0; i < pieces.Length; i++)
        {
            string rawPiece = pieces[i].Trim();

            if (string.IsNullOrWhiteSpace(rawPiece))
            {
                errorMessage =
                    "Instruction " +
                    (i + 1) +
                    " is empty.";

                instructions.Clear();
                return false;
            }

            // SECRET COMMAND:
            // It is recognized directly by the parser instead of being
            // stored in CommandDatabase. Because AvailableCommandsUI only
            // reads CommandDatabase.Commands, this never appears in the
            // visible command reference list.
            if (CommandTextNormalizer.Normalize(rawPiece) ==
                HiddenPowerOverwhelmingCommand)
            {
                instructions.Add(
                    new RuntimeInstruction(
                        InstructionType.PowerOverwhelming,
                        rawPiece
                    )
                );

                continue;
            }

            if (!commandDatabase.TryFindCommand(
                    rawPiece,
                    out CommandDefinition definition))
            {
                errorMessage =
                    "Unknown instruction at position " +
                    (i + 1) +
                    ": \"" +
                    rawPiece +
                    "\"";

                instructions.Clear();
                return false;
            }

            instructions.Add(
                new RuntimeInstruction(
                    definition.instructionType,
                    rawPiece
                )
            );
        }

        return true;
    }
}
