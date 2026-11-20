using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine;

[DisallowMultipleComponent]
public class SentencePopup : MonoBehaviour
{
    private static readonly HashSet<SentencePopup> visiblePopups = new HashSet<SentencePopup>();

    [Header("References")]
    [SerializeField] private TMP_Text sentenceText;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private PlayerResources playerResources;
    [SerializeField] private LaneSpellInput spellInput;
    [SerializeField] private LaneSpellController spellController;
    [SerializeField] private Manager uiManager;
    [Tooltip("Popup visual objects. Keep this component's GameObject active to run the timer.")]
    [SerializeField] private List<GameObject> popupObjects = new List<GameObject>();

    [Header("Popup")]
    [SerializeField, Min(0f)] private float interval = 30f;
    [SerializeField, Min(0)] private int manaReward = 5;
    [Tooltip("Enemy movement speed while this popup is visible (0 stops movement, 1 is normal speed).")]
    [SerializeField, Range(0f, 1f)] private float enemyMovementMultiplier = 0.5f;
    [SerializeField, TextArea] private List<string> sentences = new List<string> { };

    private float nextPopupTime;
    private string currentSentence;
    private bool configured;
    public bool IsVisible { get; private set; }
    public event Action Completed;

    public static float EnemyMovementMultiplier
    {
        get
        {
            float multiplier = 1f;
            foreach (SentencePopup popup in visiblePopups)
                multiplier = Mathf.Min(multiplier, Mathf.Clamp01(popup.enemyMovementMultiplier));
            return multiplier;
        }
    }

    private void LateUpdate()
    {
        if (IsVisible || inputField == null || !inputField.isActiveAndEnabled || !inputField.IsInteractable()
            || EventSystem.current == null
            || EventSystem.current.currentSelectedGameObject == inputField.gameObject)
            return;

        EventSystem.current.SetSelectedGameObject(inputField.gameObject);
        inputField.ActivateInputField();
    }

    private void FocusInput()
    {
        if (inputField == null || !inputField.isActiveAndEnabled || !inputField.IsInteractable()
            || EventSystem.current == null)
            return;

        EventSystem.current.SetSelectedGameObject(inputField.gameObject);
        inputField.ActivateInputField();
    }

    private void OnEnable()
    {
        if (!ValidateConfiguration())
        {
            enabled = false;
            return;
        }

        configured = true;
        spellInput.SentencePopup = this;
        spellController.SentencePopup = this;
        uiManager.SentencePopup = this;
        SetPopupVisible(false);
        nextPopupTime = Time.time + Mathf.Max(0f, interval);
    }

    private void OnDisable()
    {
        if (!configured)
            return;

        configured = false;
        if (spellInput != null && spellInput.SentencePopup == this)
            spellInput.SentencePopup = null;
        if (spellController != null && spellController.SentencePopup == this)
            spellController.SentencePopup = null;
        if (uiManager != null && uiManager.SentencePopup == this)
            uiManager.SentencePopup = null;

        IsVisible = false;
        visiblePopups.Remove(this);
        if (popupObjects != null)
        {
            foreach (GameObject popupObject in popupObjects)
            {
                if (popupObject != null && popupObject != gameObject &&
                    !transform.IsChildOf(popupObject.transform))
                    popupObject.SetActive(false);
            }
        }
    }

    private void Update()
    {
        if (IsVisible || Time.time < nextPopupTime)
            return;

        currentSentence = sentences[UnityEngine.Random.Range(0, sentences.Count)];
        sentenceText.text = currentSentence;
        uiManager.HideUI();
        SetPopupVisible(true);
    }

    public void Submit(string text)
    {
        if (!IsVisible || !string.Equals(text, currentSentence, StringComparison.Ordinal))
            return;

        int reward = Mathf.Max(0, manaReward);
        playerResources.SetMana((int)Math.Min(int.MaxValue, (long)playerResources.Mana + reward));
        inputField.SetTextWithoutNotify(string.Empty);
        FocusInput();
        SetPopupVisible(false);
        nextPopupTime = Time.time + Mathf.Max(0f, interval);
        Completed?.Invoke();
    }

    private void SetPopupVisible(bool visible)
    {
        IsVisible = visible;
        if (visible)
            visiblePopups.Add(this);
        else
            visiblePopups.Remove(this);
        foreach (GameObject popupObject in popupObjects)
            popupObject.SetActive(visible);
    }

    private bool ValidateConfiguration()
    {
        if (sentenceText == null || inputField == null || playerResources == null ||
            spellInput == null || spellController == null || uiManager == null || popupObjects == null ||
            popupObjects.Count == 0 || sentences == null || sentences.Count == 0)
        {
            Debug.LogError("SentencePopup needs its references, popup objects, and at least one sentence.", this);
            return false;
        }

        if (spellInput.InputField != inputField)
        {
            Debug.LogError("SentencePopup and LaneSpellInput must reference the same input field.", this);
            return false;
        }

        bool textIsInPopup = false;
        foreach (GameObject popupObject in popupObjects)
        {
            if (popupObject == null || popupObject == gameObject ||
                transform.IsChildOf(popupObject.transform) ||
                inputField.transform.IsChildOf(popupObject.transform) ||
                spellInput.transform.IsChildOf(popupObject.transform) ||
                spellController.transform.IsChildOf(popupObject.transform))
            {
                Debug.LogError("SentencePopup popup objects must not be empty or contain the timer or spell input/controller.", this);
                return false;
            }

            if (sentenceText.transform.IsChildOf(popupObject.transform))
                textIsInPopup = true;
        }

        if (!textIsInPopup)
        {
            Debug.LogError("SentencePopup's sentence text must be inside a popup object.", this);
            return false;
        }

        foreach (string sentence in sentences)
        {
            if (string.IsNullOrWhiteSpace(sentence))
            {
                Debug.LogError("SentencePopup sentences must not be empty.", this);
                return false;
            }
        }

        return true;
    }
}
