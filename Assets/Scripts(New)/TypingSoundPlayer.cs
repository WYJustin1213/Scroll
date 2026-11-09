using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_InputField), typeof(AudioSource))]
public class TypingSoundPlayer : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private AudioSource audioSource;
    [SerializeField, Min(0.01f)] private float minimumPitch = 0.9f;
    [SerializeField, Min(0.01f)] private float maximumPitch = 1.1f;

    private string previousText;

    private void Awake()
    {
        if (inputField == null)
            inputField = GetComponent<TMP_InputField>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (inputField == null)
            Debug.LogError("TypingSoundPlayer needs a TMP_InputField.", this);

        if (audioSource == null || audioSource.clip == null)
            Debug.LogError("TypingSoundPlayer needs an AudioSource with a typing clip.", this);

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
        }

        previousText = inputField != null ? inputField.text : string.Empty;
    }

    private void OnEnable()
    {
        if (inputField == null)
            inputField = GetComponent<TMP_InputField>();

        if (inputField == null)
            return;

        previousText = inputField.text;
        inputField.onValueChanged.AddListener(HandleValueChanged);
        inputField.onSubmit.AddListener(HandleSubmit);
    }

    private void OnDisable()
    {
        if (inputField != null)
        {
            inputField.onValueChanged.RemoveListener(HandleValueChanged);
            inputField.onSubmit.RemoveListener(HandleSubmit);
        }
    }

    private void HandleValueChanged(string text)
    {
        CountTextChanges(previousText, text, out int charactersAdded, out int charactersRemoved);
        previousText = text;

        if (charactersAdded > 0)
            PlayTypingSounds(charactersAdded);
        else if (charactersRemoved > 0)
            PlayTypingSounds(1);
    }

    private void HandleSubmit(string text)
    {
        PlayTypingSounds(1);
    }

    private void PlayTypingSounds(int count)
    {
        if (audioSource == null || audioSource.clip == null)
            return;

        float lowPitch = Mathf.Min(minimumPitch, maximumPitch);
        float highPitch = Mathf.Max(minimumPitch, maximumPitch);

        for (int i = 0; i < count; i++)
        {
            audioSource.pitch = Random.Range(lowPitch, highPitch);
            audioSource.PlayOneShot(audioSource.clip);
        }
    }

    private static void CountTextChanges(
        string previous,
        string current,
        out int charactersAdded,
        out int charactersRemoved)
    {
        int prefixLength = 0;
        int sharedLength = Mathf.Min(previous.Length, current.Length);
        while (prefixLength < sharedLength && previous[prefixLength] == current[prefixLength])
            prefixLength++;

        int suffixLength = 0;
        while (suffixLength < previous.Length - prefixLength &&
               suffixLength < current.Length - prefixLength &&
               previous[previous.Length - suffixLength - 1] == current[current.Length - suffixLength - 1])
        {
            suffixLength++;
        }

        charactersAdded = current.Length - prefixLength - suffixLength;
        charactersRemoved = previous.Length - prefixLength - suffixLength;
    }
}
