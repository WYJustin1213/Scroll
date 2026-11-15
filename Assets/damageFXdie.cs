using UnityEngine;

public class damageFXdie : MonoBehaviour
{
    private float timer = 0f;
    [SerializeField] private float duration = 0.5f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        if(timer>= duration){
            Destroy(this.gameObject);
        }
    }
}
