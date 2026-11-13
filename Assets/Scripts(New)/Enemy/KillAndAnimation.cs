using Unity.VisualScripting;
using UnityEngine;

public class KillAndAnimation : MonoBehaviour
{
    private Animator anim;
    [SerializeField]
    private AnimationClip clip;

    private void Start()
    {
        anim = GetComponent<Animator>();
    }

    public void kill()
    {
        //Debug.Log("in here");
        anim.SetBool("dead", true);
        if (anim.GetBool("dead"))
        {
            Destroy(gameObject.transform.parent.gameObject, clip.length);
        }
    }

}
