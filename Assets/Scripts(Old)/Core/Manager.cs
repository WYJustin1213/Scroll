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
            commands = false;
            UICanvasGroup.alpha = 0f;
            UICanvasGroup.interactable = false;
        }
        else
        {
            commands = true;
            UICanvasGroup.alpha = 1f;
            UICanvasGroup.interactable = true;
        }
    }

    public void ExitApplication()
    {
        Application.Quit();
        
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #endif
    }
}
