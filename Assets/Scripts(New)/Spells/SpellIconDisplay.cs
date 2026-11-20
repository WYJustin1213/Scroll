using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Scroll/Spell Icon Display")]
[DisallowMultipleComponent]
public class SpellIconDisplay : MonoBehaviour
{
    [Tooltip("Active UI parent for icon instances. Prefabs need a root Animator with a forward-playing, non-looping default state and no transitions.")]
    [SerializeField] private RectTransform iconParent;

    private sealed class IconInstance
    {
        public GameObject Prefab;
        public GameObject Object;
        public Animator Animator;
    }

    private readonly List<IconInstance> instances = new List<IconInstance>();

    public void Show(SpellDefinition spell)
    {
        if (spell == null || spell.IconPrefab == null)
        {
            Debug.LogWarning("SpellIconDisplay needs an icon prefab on the spell definition.", this);
            return;
        }

        if (!isActiveAndEnabled || iconParent == null || !iconParent.gameObject.activeInHierarchy)
        {
            Debug.LogError("SpellIconDisplay needs an active display and UI parent.", this);
            return;
        }

        GameObject prefab = spell.IconPrefab;
        Animator prefabAnimator = prefab.GetComponent<Animator>();
        if (!(prefab.transform is RectTransform) || prefabAnimator == null ||
            prefabAnimator.runtimeAnimatorController == null)
        {
            Debug.LogError("Spell icon prefab needs a RectTransform and Animator with a controller on its root.", prefab);
            return;
        }

        IconInstance instance = null;
        foreach (IconInstance candidate in instances)
        {
            if (candidate.Object != null && candidate.Prefab == prefab && !candidate.Object.activeSelf)
            {
                instance = candidate;
                break;
            }
        }

        if (instance == null)
        {
            GameObject icon = Instantiate(prefab, iconParent, false);
            instance = new IconInstance { Prefab = prefab, Object = icon, Animator = icon.GetComponent<Animator>() };
            instances.Add(instance);
        }

        instance.Object.SetActive(true);
        instance.Animator.enabled = true;
        instance.Animator.Rebind();
        instance.Animator.Update(0f);
        if (!HasValidAnimation(instance))
            instance.Object.SetActive(false);
    }

    private void LateUpdate()
    {
        foreach (IconInstance instance in instances)
        {
            if (instance.Object == null || !instance.Object.activeSelf)
                continue;

            if (!HasValidAnimation(instance) ||
                instance.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f)
                instance.Object.SetActive(false);
        }
    }

    private bool HasValidAnimation(IconInstance instance)
    {
        Animator animator = instance.Animator;
        if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null ||
            animator.layerCount == 0 || animator.GetCurrentAnimatorClipInfoCount(0) == 0)
        {
            Debug.LogError("Spell icon Animator needs an enabled controller with a default animation.", instance.Object);
            return false;
        }

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (state.loop || state.length <= 0f || animator.speed <= 0f ||
            state.speed * state.speedMultiplier <= 0f || animator.IsInTransition(0))
        {
            Debug.LogError("Spell icon Animator needs a forward-playing, non-looping default state without transitions.", instance.Object);
            return false;
        }

        return true;
    }

    private void OnDisable()
    {
        foreach (IconInstance instance in instances)
        {
            if (instance.Object != null)
                instance.Object.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        foreach (IconInstance instance in instances)
        {
            if (instance.Object != null)
                Destroy(instance.Object);
        }
    }
}
