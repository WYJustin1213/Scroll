using System.Collections.Generic;
using UnityEngine;

public class AvailableCommandsUI : MonoBehaviour
{
    [SerializeField]
    private CommandDatabase commandDatabase;

    [SerializeField]
    private Transform contentRoot;

    [SerializeField]
    private CommandReferenceItemUI itemPrefab;

    private readonly List<CommandReferenceItemUI> spawnedItems =
        new List<CommandReferenceItemUI>();

    private void Start()
    {
        Rebuild();
    }

    [ContextMenu("Rebuild Command List")]
    public void Rebuild()
    {
        Clear();

        if (commandDatabase == null)
        {
            Debug.LogWarning(
                "AvailableCommandsUI has no CommandDatabase assigned.",
                this
            );

            return;
        }

        if (contentRoot == null || itemPrefab == null)
        {
            Debug.LogWarning(
                "AvailableCommandsUI is missing its content root or item prefab.",
                this
            );

            return;
        }

        IReadOnlyList<CommandDefinition> commands =
            commandDatabase.Commands;

        for (int i = 0; i < commands.Count; i++)
        {
            CommandDefinition definition = commands[i];

            if (definition == null)
                continue;

            CommandReferenceItemUI item =
                Instantiate(itemPrefab, contentRoot);

            item.Setup(definition);
            spawnedItems.Add(item);
        }
    }

    private void Clear()
    {
        for (int i = spawnedItems.Count - 1; i >= 0; i--)
        {
            if (spawnedItems[i] != null)
            {
                Destroy(spawnedItems[i].gameObject);
            }
        }

        spawnedItems.Clear();
    }
}
