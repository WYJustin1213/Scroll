#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class SpellIconDisplayTests
{
    private readonly List<Object> objects = new List<Object>();
    private GameObject parent;
    private GameObject prefab;
    private Component display;
    private Object spell;

    [SetUp]
    public void SetUp()
    {
        parent = new GameObject("Icon parent", typeof(RectTransform), typeof(Canvas));
        objects.Add(parent);
        GameObject displayObject = new GameObject("Icon display");
        objects.Add(displayObject);
        display = displayObject.AddComponent(TypeOf("SpellIconDisplay"));
        Set(display, "iconParent", parent.GetComponent<RectTransform>());

        AnimationClip original = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/Spells/sleet_rune.anim");
        Assert.That(original, Is.Not.Null);
        AnimationClip clip = Object.Instantiate(original);
        objects.Add(clip);
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AnimatorController controller = new AnimatorController();
        objects.Add(controller);
        controller.AddLayer("Base Layer");
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        objects.Add(stateMachine);
        AnimatorState state = stateMachine.AddState("Icon");
        objects.Add(state);
        state.motion = clip;
        stateMachine.defaultState = state;

        prefab = new GameObject("Icon prefab", typeof(RectTransform), typeof(Animator));
        objects.Add(prefab);
        prefab.SetActive(false);
        Animator animator = prefab.GetComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.speed = 10f;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        spell = ScriptableObject.CreateInstance(TypeOf("SpellDefinition"));
        objects.Add(spell);
        Set(spell, "iconPrefab", prefab);
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = objects.Count - 1; i >= 0; i--)
            if (objects[i] != null)
                Object.DestroyImmediate(objects[i]);
        objects.Clear();
    }

    [UnityTest]
    public IEnumerator OverlappingIconsDeactivateAndAreReused()
    {
        Show();
        Show();
        Assert.That(parent.transform.childCount, Is.EqualTo(2));
        GameObject first = parent.transform.GetChild(0).gameObject;
        GameObject second = parent.transform.GetChild(1).gameObject;
        Assert.That(first.activeSelf, Is.True);
        Assert.That(second.activeSelf, Is.True);

        yield return new WaitForSeconds(1.5f);

        Assert.That(first.activeSelf, Is.False,
            "Animator progress: " + first.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).normalizedTime);
        Assert.That(second.activeSelf, Is.False);
        Show();
        Assert.That(parent.transform.childCount, Is.EqualTo(2));
        Assert.That(first.activeSelf, Is.True);
        Assert.That(second.activeSelf, Is.False);
        yield return new WaitForSeconds(1.5f);
        Assert.That(first.activeSelf, Is.False);
    }

    [UnityTest]
    public IEnumerator SparkPrefabAnimatesUIImageAndReturnsToPool()
    {
        yield return CheckIconAnimation("Spark");
    }

    [UnityTest]
    public IEnumerator SapPrefabAnimatesUIImageAndReturnsToPool()
    {
        yield return CheckIconAnimation("Sap");
    }

    [UnityTest]
    public IEnumerator ShockPrefabAnimatesUIImageAndReturnsToPool()
    {
        yield return CheckIconAnimation("Shock");
    }

    private IEnumerator CheckIconAnimation(string spellName)
    {
        Object definition = AssetDatabase.LoadAssetAtPath(
            "Assets/Scripts(New)/Spells/Definitions/" + spellName + ".asset", TypeOf("SpellDefinition"));
        Assert.That(definition, Is.Not.Null);
        Call(display, "Show", definition);

        Assert.That(parent.transform.childCount, Is.EqualTo(1));
        GameObject icon = parent.transform.GetChild(0).gameObject;
        Assert.That(icon.activeSelf, Is.True);
        Image image = icon.GetComponent<Image>();
        Assert.That(image, Is.Not.Null);
        Sprite firstFrame = image.sprite;
        Assert.That(firstFrame, Is.Not.Null);
        yield return new WaitForSeconds(0.25f);
        Assert.That(icon.activeSelf, Is.True);
        Assert.That(image.sprite, Is.Not.EqualTo(firstFrame));
        yield return new WaitForSeconds(2f);
        Assert.That(icon.activeSelf, Is.False);
    }

    [Test]
    public void OnlySuccessfulCastShowsIcon()
    {
        Component resources = Add("PlayerResources", "Resources");
        Component controller = Add("LaneSpellController", "Controller");
        Set(controller, "playerResources", resources);
        Set(controller, "iconDisplay", display);
        Component lane = Add("LaneManager", "Lane");
        Component tile = Add("LaneTile", "Tile");
        tile.transform.SetParent(lane.transform);
        Call(lane, "CollectTiles");
        Component point = Add("LaneSpellPoint", "Point");
        Set(point, "lane", lane);
        Set(spell, "function", Enum.Parse(TypeOf("SpellFunction"), "Effect"));
        Set(spell, "manaCost", 2);
        Call(resources, "SetMana", 0);

        LogAssert.Expect(LogType.Warning, "Not enough mana to cast \"\".");
        Assert.That(Call(controller, "CastSpell", spell, point), Is.False);
        Assert.That(parent.transform.childCount, Is.Zero);

        Call(resources, "SetMana", 2);
        Assert.That(Call(controller, "CastSpell", spell, point), Is.True);
        Assert.That(parent.transform.childCount, Is.EqualTo(1));
        Assert.That(parent.transform.GetChild(0).gameObject.activeSelf, Is.True);
    }

    [Test]
    public void ProjectileShowsIconOnlyAfterSuccessfulInitialization()
    {
        Component resources = Add("PlayerResources", "Resources");
        Component controller = Add("LaneSpellController", "Controller");
        Set(controller, "playerResources", resources);
        Set(controller, "iconDisplay", display);
        Component lane = Add("LaneManager", "Lane");
        Component tile = Add("LaneTile", "Tile");
        tile.transform.SetParent(lane.transform);
        tile.transform.position = Vector3.forward * 10f;
        Call(lane, "CollectTiles");
        Component point = Add("LaneSpellPoint", "Point");
        Set(point, "lane", lane);
        Call(resources, "SetMana", 2);

        LogAssert.Expect(LogType.Error, "SpellDefinition \"\" has no projectile prefab.");
        Assert.That(Call(controller, "CastSpell", spell, point), Is.False);
        Assert.That(parent.transform.childCount, Is.Zero);

        Component projectile = Add("LaneFireballProjectile", "Icon test projectile");
        Set(spell, "SparkProjectilePrefab", projectile);
        Assert.That(Call(controller, "CastSpell", spell, point), Is.True);
        Assert.That(parent.transform.childCount, Is.EqualTo(1));
        Assert.That(parent.transform.GetChild(0).gameObject.activeSelf, Is.True);
        foreach (Object clone in Object.FindObjectsByType(TypeOf("LaneFireballProjectile"), FindObjectsSortMode.None))
        {
            Component candidate = (Component)clone;
            if (candidate.name == "Icon test projectile(Clone)")
                objects.Add(candidate.gameObject);
        }
    }

    [Test]
    public void LoopingAnimationIsRejectedAndDeactivated()
    {
        prefab.GetComponent<Animator>().runtimeAnimatorController =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Spells/sleet_rune_0.controller");
        LogAssert.Expect(LogType.Error, "Spell icon Animator needs a forward-playing, non-looping default state without transitions.");
        Show();
        Assert.That(parent.transform.GetChild(0).gameObject.activeSelf, Is.False);
    }

    private void Show()
    {
        Call(display, "Show", spell);
    }

    private Component Add(string type, string name)
    {
        GameObject go = new GameObject(name);
        objects.Add(go);
        return go.AddComponent(TypeOf(type));
    }

    private static Type TypeOf(string name) => Type.GetType(name + ", Assembly-CSharp", true);

    private static void Set(object target, string field, object value)
    {
        target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }

    private static object Call(object target, string method, params object[] args)
    {
        return target.GetType().GetMethod(method).Invoke(target, args);
    }
}
#endif
