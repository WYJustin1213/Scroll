using System.Text.RegularExpressions;
using UnityEngine;

[DisallowMultipleComponent]
public class LaneSpellPoint : MonoBehaviour
{
    [SerializeField] private LaneManager lane;
    [Tooltip("The name the player types after the spell, e.g. \"Fireball Lane 1\". Not case sensitive.")]
    [SerializeField] private string laneName = "Lane";
    [Tooltip("Where spells cast into this lane appear.")]
    [SerializeField] private Transform spellOrigin;

    public LaneManager Lane => lane;
    public string LaneName => laneName;
    public Transform SpellOrigin => spellOrigin != null ? spellOrigin : transform;

    private void Reset()
    {
        lane = GetComponent<LaneManager>();
        laneName = gameObject.name;
    }

    public bool MatchesName(string typedName)
    {
        return Normalize(typedName) == Normalize(laneName);
    }

    public static string Normalize(string text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? string.Empty
            : Regex.Replace(text.Trim(), @"\s+", " ").ToLowerInvariant();
    }
}
