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

public enum SpellFunction
{
    Projectile,
    Sap
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
    [SerializeField] private SpellFunction function;
    [SerializeField] private bool directional = true;
    [SerializeField] private LaneFireballProjectile projectilePrefab;
    [SerializeField, Tooltip("Prefab with a SapTarget component on its root.")] private GameObject sapTargetPrefab;
    [SerializeField, Min(0f)] private float cooldown = 0.5f;
    [SerializeField, Min(0)] private int manaCost = 1;

    public SpellElement spellElement;

    public SpellFunction Function => function;
    public bool Directional => directional;
    public LaneFireballProjectile ProjectilePrefab => projectilePrefab;
    public GameObject TargetPrefab => sapTargetPrefab;
    public float Cooldown => cooldown;
    public int ManaCost => Mathf.Max(0, manaCost);

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
