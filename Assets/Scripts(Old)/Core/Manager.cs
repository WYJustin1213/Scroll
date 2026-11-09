using System.Collections;
using UnityEngine;

public class Manager : MonoBehaviour
{
    public bool commands = false;
    [SerializeField] GameObject CommandsUI;
    [SerializeField] CanvasGroup UICanvasGroup;
    
    private void Start()
    {
        UIVisible();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
            UIVisible();

        if (Input.GetKeyUp(KeyCode.Escape))
            ExitApplication();
    }

    public void UIVisible()
    {
        if (commands)
        {
            HideUI();
        }
        else
        {
            commands = true;
            UICanvasGroup.alpha = 1f;
            UICanvasGroup.interactable = true;

            LaneSpellInput spellInput = CommandsUI != null
                ? CommandsUI.GetComponentInChildren<LaneSpellInput>()
                : null;
            if (spellInput != null)
                StartCoroutine(FocusSpellInputNextFrame(spellInput));
        }
    }

    private IEnumerator FocusSpellInputNextFrame(LaneSpellInput spellInput)
    {
        yield return null;

        if (commands && spellInput != null && spellInput.isActiveAndEnabled)
            spellInput.FocusInput();
    }

    public void HideUI()
    {
        commands = false;
        UICanvasGroup.alpha = 0f;
        UICanvasGroup.interactable = false;
    }

    public void ExitApplication()
    {
        Application.Quit();
        
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #endif
    }
}
