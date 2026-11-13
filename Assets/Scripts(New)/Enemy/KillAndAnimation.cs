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
        anim.SetBool("dead", true);
        Destroy(gameObject.transform.parent.gameObject, clip.length);
    }

}
