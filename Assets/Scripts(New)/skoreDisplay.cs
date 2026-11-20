using TMPro;
using UnityEngine;

public class skoreDisplay : MonoBehaviour
{

    TextMeshProUGUI tmp;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        tmp = GetComponent<TextMeshProUGUI>();
        tmp.text = "Skore: \n" + pointTracker.instance.points.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
