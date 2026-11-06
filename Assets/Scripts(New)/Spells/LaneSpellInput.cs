using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class LaneSpellInput : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private LaneSpellController spellController;
    [Tooltip("Lanes that can be targeted. Filled from the scene if left empty.")]
    [SerializeField] private List<LaneSpellPoint> lanes = new List<LaneSpellPoint>();

    [Header("Fireball Command")]
    [Tooltip("Typing this word followed by a lane name casts the spell, e.g. \"Fireball Lane 1\". A \"+\" between them is also accepted.")]
    [SerializeField] private string fireballKeyword = "Fireball";

    [Header("Behavior")]
    [Tooltip("Cast as soon as the typed text matches a command. Avoid lane names that start with another lane's name (e.g. \"Lane 1\" and \"Lane 10\") when this is on.")]
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
            lanes.Sort((a, b) => string.CompareOrdinal(a.LaneName, b.LaneName));
        }
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

    private void HandleValueChanged(string text)
    {
        if (castWhenTyped)
            TryCast(text);
    }

    private void HandleSubmit(string text)
    {
        if (castOnSubmit && !TryCast(text) && !string.IsNullOrWhiteSpace(text))
            Debug.LogWarning("No spell matches \"" + text + "\".", this);
    }

    private bool TryCast(string text)
    {
        if (!TryFindLane(text, out LaneSpellPoint point))
            return false;

        if (spellController == null)
        {
            Debug.LogError("LaneSpellInput needs a LaneSpellController.", this);
            return false;
        }

        if (!spellController.CastFireball(point))
            return false;

        if (clearAfterCast)
        {
            inputField.text = string.Empty;
            inputField.ActivateInputField();
        }

        return true;
    }

    private bool TryFindLane(string text, out LaneSpellPoint match)
    {
        match = null;

        string typed = LaneSpellPoint.Normalize(text);
        string keyword = LaneSpellPoint.Normalize(fireballKeyword);
        if (keyword.Length == 0 || !typed.StartsWith(keyword))
            return false;

        string laneText = LaneSpellPoint.Normalize(typed.Substring(keyword.Length).TrimStart('+', ' '));
        if (laneText.Length == 0)
            return false;

        for (int i = 0; i < lanes.Count; i++)
        {
            if (lanes[i] != null && lanes[i].MatchesName(laneText))
            {
                match = lanes[i];
                return true;
            }
        }

        return false;
    }
}
