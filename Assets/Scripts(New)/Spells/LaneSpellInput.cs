using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class LaneSpellInput : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private LaneSpellController spellController;
    [SerializeField] private SpellDatabase spellDatabase;
    [Tooltip("Lanes that can be targeted. Filled from the scene if left empty.")]
    [SerializeField] private List<LaneSpellPoint> lanes = new List<LaneSpellPoint>();

    [Header("Behavior")]
    [Tooltip("Cast as soon as the typed text matches a spell and, when directional, a lane.")]
    [SerializeField] private bool castWhenTyped = true;
    [SerializeField] private bool castOnSubmit = true;
    [SerializeField] private bool clearAfterCast = true;

    private void Awake()
    {
        if (inputField == null)
            inputField = GetComponent<TMP_InputField>();

        if (lanes.Count == 0)
        {
            lanes.AddRange(FindObjectsByType<LaneSpellPoint>(FindObjectsSortMode.None));
        }

        lanes.Sort((a, b) =>
        {
            string aName = a != null ? CommandTextNormalizer.Normalize(a.LaneName) : string.Empty;
            string bName = b != null ? CommandTextNormalizer.Normalize(b.LaneName) : string.Empty;
            return bName.Length.CompareTo(aName.Length);
        });
    }

    private void OnEnable()
    {
        if (inputField == null)
            return;

        inputField.onValueChanged.AddListener(HandleValueChanged);
        inputField.onSubmit.AddListener(HandleSubmit);
    }

    private void OnDisable()
    {
        if (inputField == null)
            return;

        inputField.onValueChanged.RemoveListener(HandleValueChanged);
        inputField.onSubmit.RemoveListener(HandleSubmit);
    }

    public void FocusInput()
    {
        if (inputField == null)
        {
            Debug.LogError("LaneSpellInput needs an input field.", this);
            return;
        }

        if (EventSystem.current == null)
        {
            Debug.LogError("LaneSpellInput needs an EventSystem to focus the input field.", this);
            return;
        }

        if (!inputField.isActiveAndEnabled || !inputField.IsInteractable())
            return;

        EventSystem.current.SetSelectedGameObject(inputField.gameObject);
        inputField.ActivateInputField();
    }

    private void HandleValueChanged(string text)
    {
        if (castWhenTyped)
            TryCast(text, out _);
    }

    private void HandleSubmit(string text)
    {
        if (castOnSubmit && !TryCast(text, out bool matched) && !matched && !string.IsNullOrWhiteSpace(text))
            Debug.LogWarning("No spell matches \"" + text + "\".", this);
    }

    private bool TryCast(string text, out bool matched)
    {
        matched = false;
        if (spellDatabase == null)
        {
            Debug.LogError("LaneSpellInput needs a SpellDatabase.", this);
            return false;
        }

        if (!TryFindSpellAndLane(text, out SpellDefinition spell, out LaneSpellPoint point))
            return false;

        matched = true;
        if (spellController == null)
        {
            Debug.LogError("LaneSpellInput needs a LaneSpellController.", this);
            return false;
        }

        if (!spellController.CastSpell(spell, point))
            return false;

        inputField.DeactivateInputField();
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        if (clearAfterCast)
            inputField.SetTextWithoutNotify(string.Empty);

        return true;
    }

    private bool TryFindSpellAndLane(
        string text,
        out SpellDefinition spell,
        out LaneSpellPoint match)
    {
        spell = null;
        match = null;

        string typed = NormalizeInput(text);

        if (spellDatabase.TryFindSpell(typed, out spell) && !spell.Directional)
            return true;

        for (int i = 0; i < lanes.Count; i++)
        {
            LaneSpellPoint lane = lanes[i];
            if (lane == null)
                continue;

            string laneName = CommandTextNormalizer.Normalize(lane.LaneName);
            if (laneName.Length == 0 ||
                !TryGetSpellText(typed, laneName, out string spellText))
                continue;

            if (spellDatabase.TryFindSpell(spellText, out spell) && spell.Directional)
            {
                match = lane;
                return true;
            }
        }

        return false;
    }

    private static bool TryGetSpellText(string input, string laneName, out string spellText)
    {
        string spaceSuffix = " " + laneName;
        string plusSuffix = "+" + laneName;

        if (input.EndsWith(spaceSuffix))
        {
            spellText = input.Substring(0, input.Length - spaceSuffix.Length).Trim();
            return spellText.Length > 0;
        }

        if (input.EndsWith(plusSuffix))
        {
            spellText = input.Substring(0, input.Length - plusSuffix.Length).Trim();
            return spellText.Length > 0;
        }

        spellText = string.Empty;
        return false;
    }

    private static string NormalizeInput(string text)
    {
        return CommandTextNormalizer.Normalize(text)
            .Replace(" + ", "+")
            .Replace(" +", "+")
            .Replace("+ ", "+");
    }
}
