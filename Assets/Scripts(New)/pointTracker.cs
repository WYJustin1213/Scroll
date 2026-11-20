using UnityEngine;

public class pointTracker : MonoBehaviour
{

    public static pointTracker instance;
    [HideInInspector] public int points;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        DontDestroyOnLoad(gameObject);
    }

    public void Flush()
    {
        points = 0;
    }

}
