using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.TestTools;

public class PlayerResourcesTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private Scene scene;
    private string prefabPath;
    private GameObject player;
    private GameObject displayObject;
    private Component resources;
    private Component display;
    private RectTransform healthRow;
    private RectTransform manaRow;
    private Image healthPrefab;
    private Image manaPrefab;

    [SetUp]
    public void SetUp()
    {
        scene = EditorSceneManager.NewPreviewScene();
        prefabPath = AssetDatabase.GenerateUniqueAssetPath("Assets/PlayerResourcesDisplayTest.prefab");
        player = new GameObject("Player");
        SceneManager.MoveGameObjectToScene(player, scene);
        resources = player.AddComponent(FindType("PlayerResources"));
        displayObject = new GameObject("Display", typeof(RectTransform));
        SceneManager.MoveGameObjectToScene(displayObject, scene);
        displayObject.SetActive(false);
        display = displayObject.AddComponent(FindType("PlayerResourcesDisplay"));
        healthRow = CreateRow("Health");
        manaRow = CreateRow("Mana");
        healthPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/HealthUI.prefab").GetComponent<Image>();
        manaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ManaUI.prefab").GetComponent<Image>();
        SetField("resources", resources);
        SetField("healthPrefab", healthPrefab);
        SetField("manaPrefab", manaPrefab);
        SetField("healthRow", healthRow);
        SetField("manaRow", manaRow);
        displayObject.SetActive(true);
        Tick();
    }

    public class PlayerResourcesPlayModeTests
    {
        [UnityTest]
        public IEnumerator RuntimeUpdatesCreateAndReusePrefabIcons()
        {
            yield return new EnterPlayMode();

            GameObject player = new GameObject("Player");
            Type resourcesType = Type.GetType("PlayerResources, Assembly-CSharp");
            Component resources = player.AddComponent(resourcesType);
            GameObject root = new GameObject("Display", typeof(RectTransform));
            root.SetActive(false);
            Component display = root.AddComponent(Type.GetType("PlayerResourcesDisplay, Assembly-CSharp"));
            GameObject healthRow = new GameObject("Health", typeof(RectTransform));
            GameObject manaRow = new GameObject("Mana", typeof(RectTransform));
            healthRow.transform.SetParent(root.transform, false);
            manaRow.transform.SetParent(root.transform, false);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            Type displayType = display.GetType();
            displayType.GetField("resources", flags).SetValue(display, resources);
            displayType.GetField("healthRow", flags).SetValue(display, healthRow.transform);
            displayType.GetField("manaRow", flags).SetValue(display, manaRow.transform);
            displayType.GetField("healthPrefab", flags).SetValue(display,
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/HealthUI.prefab").GetComponent<Image>());
            displayType.GetField("manaPrefab", flags).SetValue(display,
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ManaUI.prefab").GetComponent<Image>());
            root.SetActive(true);

            Assert.That(healthRow.transform.childCount, Is.EqualTo(5));
            Assert.That(manaRow.transform.childCount, Is.EqualTo(5));
            resourcesType.GetMethod("SetMana").Invoke(resources, new object[] { 8 });
            Assert.That(manaRow.transform.childCount, Is.EqualTo(8));
            GameObject extra = manaRow.transform.GetChild(7).gameObject;
            resourcesType.GetMethod("SetMana").Invoke(resources, new object[] { 1 });
            Assert.That(extra.GetComponent<Image>().enabled, Is.False);
            resourcesType.GetMethod("SetMana").Invoke(resources, new object[] { 8 });
            Assert.That(manaRow.transform.GetChild(7).gameObject, Is.EqualTo(extra));
            Assert.That(extra.GetComponent<Image>().enabled, Is.True);

            UnityEngine.Object.Destroy(root);
            UnityEngine.Object.Destroy(player);
            yield return new ExitPlayMode();
        }
    }

    [TearDown]
    public void TearDown()
    {
        if (scene.IsValid() && scene.isLoaded)
            EditorSceneManager.ClosePreviewScene(scene);
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            AssetDatabase.DeleteAsset(prefabPath);
    }

    [Test]
    public void GeneratesIndependentPrefabRowsAndClampsCounts()
    {
        Assert.That(healthRow.childCount, Is.EqualTo(5));
        Assert.That(manaRow.childCount, Is.EqualTo(5));
        Assert.That(Visible(healthRow), Is.EqualTo(5));
        Assert.That(Visible(manaRow), Is.EqualTo(5));
        Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(healthRow.GetChild(0).gameObject), Is.EqualTo(healthPrefab.gameObject));
        Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(manaRow.GetChild(0).gameObject), Is.EqualTo(manaPrefab.gameObject));

        SetCount("SetHealth", 2);
        SetCount("SetMana", -1);
        Tick();
        Assert.That(Visible(healthRow), Is.EqualTo(2));
        Assert.That(Visible(manaRow), Is.EqualTo(0));
        Assert.That(resources.GetType().GetProperty("Mana").GetValue(resources), Is.EqualTo(0));

        SetCount("SetMana", 4);
        Tick();
        Assert.That(Visible(healthRow), Is.EqualTo(2));
        Assert.That(Visible(manaRow), Is.EqualTo(4));
    }

    [Test]
    public void GrowsAndReusesIconsAcrossDisableAndEnable()
    {
        SetCount("SetHealth", 7);
        Tick();
        Assert.That(healthRow.childCount, Is.EqualTo(7));
        GameObject extra = healthRow.GetChild(6).gameObject;
        SetCount("SetHealth", 0);
        Tick();
        Assert.That(Visible(healthRow), Is.Zero);
        displayObject.SetActive(false);
        SetCount("SetHealth", 6);
        displayObject.SetActive(true);
        Tick();
        Assert.That(healthRow.childCount, Is.EqualTo(7));
        Assert.That(healthRow.GetChild(6).gameObject, Is.EqualTo(extra));
        Assert.That(Visible(healthRow), Is.EqualTo(6));
    }

    [UnityTest]
    public IEnumerator ResourceChangesRefreshAutomaticallyInEditMode()
    {
        SetCount("SetHealth", 8);
        yield return null;
        yield return null;
        Assert.That(healthRow.childCount, Is.EqualTo(8));
        Assert.That(Visible(healthRow), Is.EqualTo(8));
    }

    [Test]
    public void LayoutAndGeneratedObjectsPersistOutsidePlayMode()
    {
        SerializedObject serialized = new SerializedObject(display);
        serialized.FindProperty("healthStart").vector2Value = new Vector2(30, -25);
        serialized.FindProperty("healthStep").vector2Value = new Vector2(90, -10);
        serialized.ApplyModifiedProperties();
        SetCount("SetHealth", 6);
        Tick();
        RectTransform icon = (RectTransform)healthRow.GetChild(5);
        Assert.That(icon.anchoredPosition, Is.EqualTo(new Vector2(480, -75)));
        Assert.That(icon.sizeDelta, Is.EqualTo(new Vector2(80, 80)));
        player.transform.SetParent(displayObject.transform, false);
        Assert.That(PrefabUtility.SaveAsPrefabAsset(displayObject, prefabPath), Is.Not.Null);
        GameObject savedDisplay = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            RectTransform savedRow = (RectTransform)savedDisplay.transform.Find("Health");
            Assert.That(savedRow.childCount, Is.EqualTo(6));
            Assert.That(((RectTransform)savedRow.GetChild(5)).anchoredPosition, Is.EqualTo(new Vector2(480, -75)));
            Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(savedRow.GetChild(5).gameObject), Is.EqualTo(healthPrefab.gameObject));
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(savedDisplay);
        }
    }

    [Test]
    public void ReplacingPrefabRebuildsOnlyItsRow()
    {
        SetField("healthPrefab", manaPrefab);
        display.GetType().GetMethod("OnValidate", PrivateInstance).Invoke(display, null);
        Tick();
        Assert.That(healthRow.childCount, Is.EqualTo(5));
        Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(healthRow.GetChild(0).gameObject), Is.EqualTo(manaPrefab.gameObject));
        Assert.That(manaRow.childCount, Is.EqualTo(5));
    }

    private RectTransform CreateRow(string name)
    {
        GameObject row = new GameObject(name, typeof(RectTransform));
        row.transform.SetParent(displayObject.transform, false);
        return (RectTransform)row.transform;
    }

    private static Type FindType(string name)
    {
        Type type = Type.GetType(name + ", Assembly-CSharp");
        Assert.That(type, Is.Not.Null, name + " was not found");
        return type;
    }

    private void SetField(string name, object value)
    {
        display.GetType().GetField(name, PrivateInstance).SetValue(display, value);
    }

    private void SetCount(string method, int value)
    {
        resources.GetType().GetMethod(method).Invoke(resources, new object[] { value });
    }

    private void Tick()
    {
        display.GetType().GetMethod("Update", PrivateInstance).Invoke(display, null);
    }

    private static int Visible(Transform row)
    {
        int count = 0;
        foreach (Transform child in row)
        {
            if (child.GetComponent<Image>().enabled)
                count++;
        }
        return count;
    }
}
