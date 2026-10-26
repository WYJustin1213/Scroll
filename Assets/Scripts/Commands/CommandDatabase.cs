using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "CommandDatabase",
    menuName = "Command FPS/Command Database"
)]
public class CommandDatabase : ScriptableObject
{
    [SerializeField]
    private List<CommandDefinition> commands =
        new List<CommandDefinition>();

    private Dictionary<string, CommandDefinition>
        runtimeLookup;

    public IReadOnlyList<CommandDefinition>
        Commands => commands;

    public bool TryFindCommand(
        string input,
        out CommandDefinition definition)
    {
        EnsureLookupExists();

        string normalized =
            CommandTextNormalizer.Normalize(input);

        return runtimeLookup.TryGetValue(
            normalized,
            out definition
        );
    }

    private void EnsureLookupExists()
    {
        if (runtimeLookup != null)
            return;

        runtimeLookup =
            new Dictionary<string, CommandDefinition>();

        foreach (CommandDefinition command in commands)
        {
            if (command == null)
                continue;

            AddToLookup(
                command.commandText,
                command
            );

            foreach (string alias in command.aliases)
            {
                AddToLookup(
                    alias,
                    command
                );
            }
        }
    }

    private void AddToLookup(
        string commandText,
        CommandDefinition definition)
    {
        string normalized =
            CommandTextNormalizer.Normalize(commandText);

        if (string.IsNullOrWhiteSpace(normalized))
            return;

        if (runtimeLookup.ContainsKey(normalized))
        {
            Debug.LogWarning(
                "Duplicate command text found: \"" +
                commandText +
                "\" in CommandDatabase " +
                name
            );

            return;
        }

        runtimeLookup.Add(
            normalized,
            definition
        );
    }

    private void OnValidate()
    {
        // Rebuild after Inspector changes.
        runtimeLookup = null;
    }
}
