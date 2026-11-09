using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
public class PlayerResourcesDisplay : MonoBehaviour
{
    [SerializeField] private PlayerResources resources;
    [SerializeField] private Image healthPrefab;
    [SerializeField] private Image manaPrefab;
    [SerializeField] private RectTransform healthRow;
    [SerializeField] private RectTransform manaRow;
    [Tooltip("First icon position relative to the top-left of the health row.")]
    [SerializeField] private Vector2 healthStart = new Vector2(40, -40);
    [Tooltip("First icon position relative to the top-left of the mana row.")]
    [SerializeField] private Vector2 manaStart = new Vector2(40, -40);
    [Tooltip("Offset from one health icon to the next.")]
    [SerializeField] private Vector2 healthStep = new Vector2(80, 0);
    [Tooltip("Offset from one mana icon to the next.")]
    [SerializeField] private Vector2 manaStep = new Vector2(80, 0);

    [SerializeField, HideInInspector] private List<Image> healthIcons = new List<Image>();
    [SerializeField, HideInInspector] private List<Image> manaIcons = new List<Image>();
    [SerializeField, HideInInspector] private Image generatedHealthPrefab;
    [SerializeField, HideInInspector] private Image generatedManaPrefab;

    private PlayerResources subscribedResources;
    private int displayedHealth = -1;
    private int displayedMana = -1;
    private bool layoutDirty = true;

    private void OnEnable()
    {
        Subscribe();
        layoutDirty = true;
        if (Application.IsPlaying(gameObject))
        {
            if (!IsConfigured())
            {
                Debug.LogError("Player resource display requires resources, two icon prefabs, and two row transforms.", this);
                enabled = false;
                return;
            }

            Refresh();
        }
    }

    private void OnDisable()
    {
        if (subscribedResources != null)
            subscribedResources.Changed -= OnResourcesChanged;
        subscribedResources = null;
    }

    private void OnValidate()
    {
        layoutDirty = true;
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.IsPlaying(gameObject) && EditorUtility.IsPersistent(gameObject))
            return;
#endif
        Subscribe();
        if (!IsConfigured())
            return;

        if (layoutDirty || resources.Health != displayedHealth || resources.Mana != displayedMana ||
            healthIcons.Contains(null) || manaIcons.Contains(null))
            Refresh();
    }

    private void Subscribe()
    {
        if (subscribedResources == resources)
            return;

        if (subscribedResources != null)
            subscribedResources.Changed -= OnResourcesChanged;
        subscribedResources = resources;
        if (subscribedResources != null)
            subscribedResources.Changed += OnResourcesChanged;
        layoutDirty = true;
    }

    private bool IsConfigured()
    {
        return resources != null && healthPrefab != null && manaPrefab != null &&
            healthRow != null && manaRow != null && healthRow != manaRow;
    }

    private void OnResourcesChanged()
    {
        if (Application.IsPlaying(gameObject) && IsConfigured())
            Refresh();
        else
        {
            layoutDirty = true;
#if UNITY_EDITOR
            EditorApplication.QueuePlayerLoopUpdate();
#endif
        }
    }

    private void Refresh()
    {
        displayedHealth = resources.Health;
        displayedMana = resources.Mana;
        UpdateIcons(healthIcons, healthPrefab, ref generatedHealthPrefab, healthRow, healthStart, healthStep, displayedHealth);
        UpdateIcons(manaIcons, manaPrefab, ref generatedManaPrefab, manaRow, manaStart, manaStep, displayedMana);
        layoutDirty = false;
    }

    private void UpdateIcons(List<Image> icons, Image prefab, ref Image generatedPrefab, RectTransform row,
        Vector2 start, Vector2 step, int count)
    {
        if (generatedPrefab != null && generatedPrefab != prefab)
        {
            foreach (Image icon in icons)
            {
                if (icon != null)
                    DestroyIcon(icon.gameObject);
            }
            icons.Clear();
        }

        if (generatedPrefab != prefab)
        {
            generatedPrefab = prefab;
            MarkChanged(this);
        }

        if (icons.RemoveAll(icon => icon == null) > 0)
            MarkChanged(this);

        while (icons.Count < count)
        {
            Image icon;
#if UNITY_EDITOR
            if (!Application.IsPlaying(gameObject))
                icon = ((GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, row)).GetComponent<Image>();
            else
#endif
                icon = Instantiate(prefab, row);
            icons.Add(icon);
            MarkChanged(this);
        }

        for (int i = 0; i < icons.Count; i++)
        {
            Image icon = icons[i];
            RectTransform rect = icon.rectTransform;
            Vector2 position = start + step * i;
            bool visible = i < count;
            if (rect.parent != row || rect.anchorMin != Vector2.up || rect.anchorMax != Vector2.up ||
                rect.anchoredPosition != position)
            {
                rect.SetParent(row, false);
                rect.anchorMin = Vector2.up;
                rect.anchorMax = Vector2.up;
                rect.anchoredPosition = position;
                MarkChanged(rect);
            }
            if (icon.enabled != visible)
            {
                icon.enabled = visible;
                MarkChanged(icon);
            }
        }
    }

    private void DestroyIcon(GameObject icon)
    {
#if UNITY_EDITOR
        if (!Application.IsPlaying(gameObject))
        {
            DestroyImmediate(icon);
            MarkChanged(this);
            return;
        }
#endif
        Destroy(icon);
    }

    private void MarkChanged(Object changed)
    {
#if UNITY_EDITOR
        if (!Application.IsPlaying(gameObject))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(changed);
            EditorUtility.SetDirty(changed);
            if (gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
    }
}
