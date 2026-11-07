using System.Collections.Generic;
using UnityEngine;

public enum SpellElement
{
    Fire,
    Water,
    Shock,
    Ice,
    None
}

[CreateAssetMenu(fileName = "SpellDefinition", menuName = "Scroll/Spell Definition")]
public class SpellDefinition : ScriptableObject
{
    [Header("Display")]
    public string displayName;

    [TextArea(2, 4)]
    public string description;

    [Header("Spell Text")]
    public string commandText;
    public List<string> aliases = new List<string>();

    [Header("Casting")]
    [SerializeField] private LaneFireballProjectile projectilePrefab;
    [SerializeField, Min(0f)] private float cooldown = 0.5f;

    public SpellElement spellElement;

    public LaneFireballProjectile ProjectilePrefab => projectilePrefab;
    public float Cooldown => cooldown;

    public bool Matches(string input)
    {
        string normalizedInput = CommandTextNormalizer.Normalize(input);

        if (normalizedInput == CommandTextNormalizer.Normalize(commandText))
            return true;

        foreach (string alias in aliases)
        {
            if (normalizedInput == CommandTextNormalizer.Normalize(alias))
                return true;
        }

        return false;
    }
}
