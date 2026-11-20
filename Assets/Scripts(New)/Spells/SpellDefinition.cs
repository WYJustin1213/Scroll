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
    Effect,
    Beam
}

[CreateAssetMenu(fileName = "SpellDefinition", menuName = "Scroll/Spell Definition")]
public class SpellDefinition : ScriptableObject
{
    [Header("Display")]
    public string displayName;

    [TextArea(2, 4)]
    public string description;

    [Tooltip("UI prefab with a root Animator playing a non-looping default state.")]
    [SerializeField] private GameObject iconPrefab;

    [Header("Spell Text")]
    public string commandText;
    public List<string> aliases = new List<string>();

    [Header("Casting")]
    [SerializeField] private SpellFunction function;
    public SpellElement spellElement;
    [SerializeField, Min(0)] private int manaCost = 1;
    [SerializeField] private bool directional = true;

    [Header("Projectile")]
    [SerializeField] private LaneFireballProjectile SparkProjectilePrefab;
    [SerializeField] private LaneSleetProjectile SleetProjectilePrefab;

    [Header("Effect")]
    [SerializeField, Tooltip("Prefab with a SapTarget component on its root.")] private GameObject sapTargetPrefab;

    [Header("Beam")]
    [SerializeField] private LaneSpoutBeam SpoutBeamPrefab;
    [SerializeField] private LaneShockBeam ShockBeamPrefab;
    [SerializeField, Tooltip("World-space offset applied to each shock trail sprite.")]
    private Vector3 shockTrailPositionOffset;
    [SerializeField, Tooltip("Euler rotation offset applied after aligning shock trail sprites with their travel direction.")]
    private Vector3 shockTrailRotation;

    public SpellFunction Function => function;
    public GameObject IconPrefab => iconPrefab;
    public bool Directional => directional;
    public LaneFireballProjectile sparkProjectilePrefab => SparkProjectilePrefab;
    public LaneSleetProjectile sleetProjectilePrefab => SleetProjectilePrefab;
    public LaneSpoutBeam spoutBeamPrefab => SpoutBeamPrefab;
    public LaneShockBeam shockBeamPrefab => ShockBeamPrefab;
    public Vector3 ShockTrailPositionOffset => shockTrailPositionOffset;
    public Vector3 ShockTrailRotation => shockTrailRotation;
    public GameObject TargetPrefab => sapTargetPrefab;
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
