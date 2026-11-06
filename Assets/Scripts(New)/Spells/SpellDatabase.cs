using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SpellDatabase", menuName = "Scroll/Spell Database")]
public class SpellDatabase : ScriptableObject
{
    [SerializeField] private List<SpellDefinition> spells = new List<SpellDefinition>();

    private Dictionary<string, SpellDefinition> runtimeLookup;

    public IReadOnlyList<SpellDefinition> Spells => spells;

    public bool TryFindSpell(string input, out SpellDefinition spell)
    {
        EnsureLookupExists();
        return runtimeLookup.TryGetValue(
            CommandTextNormalizer.Normalize(input),
            out spell);
    }

    private void EnsureLookupExists()
    {
        if (runtimeLookup != null)
            return;

        runtimeLookup = new Dictionary<string, SpellDefinition>();

        foreach (SpellDefinition spell in spells)
        {
            if (spell == null)
                continue;

            AddToLookup(spell.commandText, spell);
            foreach (string alias in spell.aliases)
                AddToLookup(alias, spell);
        }
    }

    private void AddToLookup(string text, SpellDefinition spell)
    {
        string normalized = CommandTextNormalizer.Normalize(text);
        if (string.IsNullOrWhiteSpace(normalized))
            return;

        if (runtimeLookup.ContainsKey(normalized))
        {
            Debug.LogWarning(
                "Duplicate spell text found: \"" + text + "\" in SpellDatabase " + name,
                this);
            return;
        }

        runtimeLookup.Add(normalized, spell);
    }

    private void OnValidate()
    {
        runtimeLookup = null;
    }
}
