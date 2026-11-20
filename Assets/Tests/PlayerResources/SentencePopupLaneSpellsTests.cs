using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class SentencePopupLaneSpellsTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private readonly List<Object> objects = new List<Object>();
    private Component popup;
    private Component automatic;
    private Component spellInput;
    private Component controller;
    private Component resources;
    private Component uiManager;
    private CanvasGroup commandsGroup;
    private Component firstPoint;
    private Component secondPoint;
    private Object spell;

    [SetUp]
    public void SetUp()
    {
        resources = Add("PlayerResources", "Resources");
        Call(resources, "SetMana", 0);
        controller = Add("LaneSpellController", "Controller");
        Set(controller, "playerResources", resources);
        firstPoint = CreateLane("First lane", 0f);
        secondPoint = CreateLane("Second lane", 3f);

        Component projectile = Add("LaneFireballProjectile", "Automatic test projectile");
        spell = ScriptableObject.CreateInstance(TypeOf("SpellDefinition"));
        objects.Add(spell);
        spell.name = "Automatic test spell";
        Set(spell, "SparkProjectilePrefab", projectile);
        Set(spell, "manaCost", 7);

        GameObject popupRoot = NewObject("Popup");
        popupRoot.SetActive(false);
        popup = popupRoot.AddComponent(TypeOf("SentencePopup"));
        automatic = popupRoot.AddComponent(TypeOf("SentencePopupLaneSpells"));
        GameObject textObject = NewObject("Sentence text", typeof(RectTransform));
        textObject.transform.SetParent(popupRoot.transform);
        Component text = textObject.AddComponent(Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro", true));
        Component input = NewObject("Input", typeof(RectTransform))
            .AddComponent(Type.GetType("TMPro.TMP_InputField, Unity.TextMeshPro", true));
        spellInput = Add("LaneSpellInput", "Spell input");
        uiManager = Add("Manager", "UI manager");
        commandsGroup = NewObject("Commands").AddComponent<CanvasGroup>();
        Set(uiManager, "UICanvasGroup", commandsGroup);
        Set(controller, "uiManager", uiManager);
        Set(spellInput, "inputField", input);
        Set(popup, "sentenceText", text);
        Set(popup, "inputField", input);
        Set(popup, "playerResources", resources);
        Set(popup, "spellInput", spellInput);
        Set(popup, "spellController", controller);
        Set(popup, "uiManager", uiManager);
        Set(popup, "popupObjects", new List<GameObject> { textObject });
        Set(popup, "sentences", new List<string> { "Exact sentence." });
        Set(popup, "manaReward", 2);
        Set(automatic, "sentencePopup", popup);
        Set(automatic, "spellController", controller);
        AddAssignment(firstPoint, spell);
        AddAssignment(secondPoint, spell);
        popupRoot.SetActive(true);
        Set(popup, "nextPopupTime", Time.time);
        InvokePrivate(popup, "Update");
    }

    [TearDown]
    public void TearDown()
    {
        foreach (Object clone in Object.FindObjectsByType(TypeOf("LaneFireballProjectile"), FindObjectsSortMode.None))
        {
            Component projectile = (Component)clone;
            if (projectile.name == "Automatic test projectile(Clone)")
                Object.DestroyImmediate(projectile.gameObject);
        }

        for (int i = objects.Count - 1; i >= 0; i--)
            if (objects[i] != null)
                Object.DestroyImmediate(objects[i]);
        objects.Clear();
    }

    [Test]
    public void PopupBlocksCommandsUntilExactSubmission()
    {
        Call(resources, "SetMana", 10);
        Call(uiManager, "UIVisible");
        Assert.That((bool)uiManager.GetType().GetField("commands").GetValue(uiManager), Is.False);
        Assert.That(commandsGroup.interactable, Is.False);
        Assert.That((bool)Call(controller, "CastSpell", spell, firstPoint), Is.False);

        InvokePrivate(spellInput, "HandleSubmit", "wrong");
        Assert.That(commandsGroup.interactable, Is.False);
        Assert.That((bool)Call(controller, "CastSpell", spell, firstPoint), Is.False);

        InvokePrivate(spellInput, "HandleSubmit", "Exact sentence.");
        Assert.That((bool)uiManager.GetType().GetField("commands").GetValue(uiManager), Is.True);
        Assert.That(commandsGroup.alpha, Is.EqualTo(1f));
        Assert.That(commandsGroup.interactable, Is.True);
        Assert.That((bool)Call(controller, "CastSpell", spell, firstPoint), Is.True);
    }

    [Test]
    public void ExactCompletionCastsEachLaneWithoutSpendingReward()
    {
        Call(popup, "Submit", "wrong");
        Assert.That(ProjectileCount(), Is.Zero);
        Assert.That(Mana, Is.Zero);

        Call(popup, "Submit", "Exact sentence.");
        Assert.That(ProjectileCount(), Is.EqualTo(2));
        Assert.That(Mana, Is.EqualTo(2));
        Assert.That((bool)uiManager.GetType().GetField("commands").GetValue(uiManager), Is.True);
        Assert.That(commandsGroup.interactable, Is.True);
        Call(popup, "Submit", "Exact sentence.");
        Assert.That(ProjectileCount(), Is.EqualTo(2));
    }

    [Test]
    public void FailedCastRefundsTemporaryManaAndOtherLanesStillCast()
    {
        Set(firstPoint, "lane", null);
        LogAssert.Expect(LogType.Error, "LaneSpellController needs a lane spell point with a valid lane.");
        LogAssert.Expect(LogType.Warning, "Automatic spell \"Automatic test spell\" could not be cast in Lane.");
        Call(popup, "Submit", "Exact sentence.");
        Assert.That(ProjectileCount(), Is.EqualTo(1));
        Assert.That(Mana, Is.EqualTo(2));
    }

    [Test]
    public void EffectKeepsItsOwnManaGainWithoutKeepingTemporaryMana()
    {
        Set(spell, "function", Enum.Parse(TypeOf("SpellFunction"), "Effect"));
        Call(popup, "Submit", "Exact sentence.");
        Assert.That(Mana, Is.EqualTo(4));
    }

    [Test]
    public void LanesCanUseDifferentSpells()
    {
        Object effect = ScriptableObject.CreateInstance(TypeOf("SpellDefinition"));
        objects.Add(effect);
        Set(effect, "function", Enum.Parse(TypeOf("SpellFunction"), "Effect"));
        Set(effect, "manaCost", 3);
        IList assignments = (IList)automatic.GetType().GetField("laneSpells", PrivateInstance).GetValue(automatic);
        assignments[1].GetType().GetField("spell").SetValue(assignments[1], effect);

        Call(popup, "Submit", "Exact sentence.");
        Assert.That(ProjectileCount(), Is.EqualTo(1));
        Assert.That(Mana, Is.EqualTo(3));
    }

    [Test]
    public void DisabledComponentDoesNotCastAndReenabledComponentCastsAgain()
    {
        ((Behaviour)automatic).enabled = false;
        Call(popup, "Submit", "Exact sentence.");
        Assert.That(ProjectileCount(), Is.Zero);
        ((Behaviour)automatic).enabled = true;
        Set(popup, "nextPopupTime", Time.time);
        InvokePrivate(popup, "Update");
        Call(popup, "Submit", "Exact sentence.");
        Assert.That(ProjectileCount(), Is.EqualTo(2));
        Assert.That(Mana, Is.EqualTo(4));
    }

    [Test]
    public void CompletionAtMaximumManaDoesNotOverflow()
    {
        Call(resources, "SetMana", int.MaxValue);
        Call(popup, "Submit", "Exact sentence.");
        Assert.That(ProjectileCount(), Is.EqualTo(2));
        Assert.That(Mana, Is.EqualTo(int.MaxValue));
    }

    [Test]
    public void EffectAtMaximumManaDoesNotOverflow()
    {
        Set(spell, "function", Enum.Parse(TypeOf("SpellFunction"), "Effect"));
        Call(resources, "SetMana", int.MaxValue - 1);
        Set(popup, "manaReward", 0);
        Call(popup, "Submit", "Exact sentence.");
        Assert.That(Mana, Is.EqualTo(int.MaxValue));
    }

    private int Mana => (int)resources.GetType().GetProperty("Mana").GetValue(resources);

    private Component CreateLane(string name, float x)
    {
        Component lane = Add("LaneManager", name);
        lane.transform.position = Vector3.right * x;
        Component tile = Add("LaneTile", name + " tile");
        tile.transform.SetParent(lane.transform, false);
        tile.transform.localPosition = Vector3.forward * 10f;
        Call(lane, "CollectTiles");
        Component point = Add("LaneSpellPoint", name + " point");
        point.transform.position = Vector3.right * x;
        Set(point, "lane", lane);
        return point;
    }

    private void AddAssignment(Component point, Object assignedSpell)
    {
        FieldInfo field = automatic.GetType().GetField("laneSpells", PrivateInstance);
        IList assignments = (IList)field.GetValue(automatic);
        object entry = Activator.CreateInstance(field.FieldType.GetGenericArguments()[0], true);
        entry.GetType().GetField("lane").SetValue(entry, point);
        entry.GetType().GetField("spell").SetValue(entry, assignedSpell);
        assignments.Add(entry);
    }

    private int ProjectileCount()
    {
        int count = 0;
        foreach (Object item in Object.FindObjectsByType(TypeOf("LaneFireballProjectile"), FindObjectsSortMode.None))
            if (item.name == "Automatic test projectile(Clone)")
                count++;
        return count;
    }

    private GameObject NewObject(string name, params Type[] components)
    {
        GameObject result = new GameObject(name, components);
        objects.Add(result);
        return result;
    }

    private Component Add(string type, string name) => NewObject(name).AddComponent(TypeOf(type));
    private static Type TypeOf(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Call(Component instance, string method, params object[] args) =>
        instance.GetType().GetMethod(method).Invoke(instance, args);
    private static void InvokePrivate(Component instance, string method, params object[] args) =>
        instance.GetType().GetMethod(method, PrivateInstance).Invoke(instance, args);
    private static void Set(Object instance, string name, object value) =>
        instance.GetType().GetField(name, PrivateInstance).SetValue(instance, value);
}
