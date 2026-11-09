using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class SpellManaTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private readonly List<Object> objects = new List<Object>();
    private Component resources;
    private Component controller;
    private Component point;
    private Component projectilePrefab;
    private Object spell;

    [SetUp]
    public void SetUp()
    {
        resources = CreateComponent("PlayerResources", "Player");
        controller = CreateComponent("LaneSpellController", "Controller");
        SetField(controller, "playerResources", resources);

        Component lane = CreateComponent("LaneManager", "Lane");
        Component tile = CreateComponent("LaneTile", "Tile");
        tile.transform.SetParent(lane.transform);
        tile.transform.localPosition = Vector3.forward * 10f;
        Call(lane, "CollectTiles");
        point = CreateComponent("LaneSpellPoint", "Spell Point");
        SetField(point, "lane", lane);

        projectilePrefab = CreateComponent("LaneFireballProjectile", "Test Projectile");
        spell = ScriptableObject.CreateInstance(RuntimeType("SpellDefinition"));
        spell.name = "Test Spell";
        objects.Add(spell);
        SetField(spell, "projectilePrefab", projectilePrefab);
        SetField(spell, "cooldown", 0f);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (Object clone in Object.FindObjectsByType(RuntimeType("LaneFireballProjectile"), FindObjectsSortMode.None))
        {
            Component projectile = (Component)clone;
            if (projectile.name == "Test Projectile(Clone)")
                Object.DestroyImmediate(projectile.gameObject);
        }

        for (int i = objects.Count - 1; i >= 0; i--)
        {
            if (objects[i] != null)
                Object.DestroyImmediate(objects[i]);
        }
        objects.Clear();
    }

    [Test]
    public void SuccessfulCastSpendsConfiguredManaAndInsufficientManaBlocksProjectile()
    {
        SetField(spell, "manaCost", 2);
        SetMana(3);

        Assert.That(Cast(), Is.True);
        Assert.That(Mana, Is.EqualTo(1));
        Assert.That(ProjectileCount(), Is.EqualTo(1));

        LogAssert.Expect(LogType.Warning, "Not enough mana to cast \"Test Spell\".");
        Assert.That(Cast(), Is.False);
        Assert.That(Mana, Is.EqualTo(1));
        Assert.That(ProjectileCount(), Is.EqualTo(1));
    }

    [Test]
    public void DefaultCostAndExactBalanceAllowCastThenBlockFurtherCasts()
    {
        Assert.That(spell.GetType().GetProperty("ManaCost").GetValue(spell), Is.EqualTo(1));
        SetMana(1);
        Assert.That(Cast(), Is.True);
        Assert.That(Mana, Is.Zero);

        LogAssert.Expect(LogType.Warning, "Not enough mana to cast \"Test Spell\".");
        Assert.That(Cast(), Is.False);
        Assert.That(ProjectileCount(), Is.EqualTo(1));
    }

    [Test]
    public void FreeSpellCanCastWithoutMana()
    {
        SetField(spell, "manaCost", 0);
        SetMana(0);
        Assert.That(Cast(), Is.True);
        Assert.That(Mana, Is.Zero);
        Assert.That(ProjectileCount(), Is.EqualTo(1));
    }

    [Test]
    public void DefaultKeyCastUsesSameManaPool()
    {
        Component lane = (Component)point.GetType().GetProperty("Lane").GetValue(point);
        Component nextTile = CreateComponent("LaneTile", "Next Tile");
        nextTile.transform.SetParent(lane.transform);
        nextTile.transform.localPosition = Vector3.forward * 20f;
        Call(lane, "CollectTiles");
        Component marker = CreateComponent("LanePlayerMarker", "Player Marker");
        SetField(marker, "lane", lane);
        SetField(controller, "player", marker);
        SetField(controller, "spellOrigin", marker.transform);
        SetField(controller, "defaultSpell", spell);
        SetMana(1);

        Assert.That(controller.GetType().GetMethod("CastFireball", Type.EmptyTypes).Invoke(controller, null), Is.True);
        Assert.That(Mana, Is.Zero);
        LogAssert.Expect(LogType.Warning, "Not enough mana to cast \"Test Spell\".");
        Assert.That(Cast(), Is.False);
        Assert.That(ProjectileCount(), Is.EqualTo(1));
    }

    [Test]
    public void CooldownAndInvalidProjectileDoNotSpendMana()
    {
        SetField(spell, "cooldown", 60f);
        Assert.That(Cast(), Is.True);
        Assert.That(Mana, Is.EqualTo(4));
        Assert.That(Cast(), Is.False);
        Assert.That(Mana, Is.EqualTo(4));
        Assert.That(ProjectileCount(), Is.EqualTo(1));

        SetField(controller, "nextCastTime", 0f);
        SetField(spell, "projectilePrefab", null);
        LogAssert.Expect(LogType.Error, "SpellDefinition \"Test Spell\" has no projectile prefab.");
        Assert.That(Cast(), Is.False);
        Assert.That(Mana, Is.EqualTo(4));
        Assert.That(ProjectileCount(), Is.EqualTo(1));
    }

    [Test]
    public void MissingResourcesRejectsCast()
    {
        SetField(controller, "playerResources", null);
        LogAssert.Expect(LogType.Error, "LaneSpellController needs the player's PlayerResources component.");
        Assert.That(Cast(), Is.False);
        Assert.That(ProjectileCount(), Is.Zero);
    }

    private int Mana => (int)resources.GetType().GetProperty("Mana").GetValue(resources);

    private void SetMana(int value)
    {
        Call(resources, "SetMana", value);
    }

    private bool Cast()
    {
        return (bool)Call(controller, "CastSpell", spell, point);
    }

    private int ProjectileCount()
    {
        int count = 0;
        foreach (Object projectile in Object.FindObjectsByType(RuntimeType("LaneFireballProjectile"), FindObjectsSortMode.None))
        {
            if (projectile.name == "Test Projectile(Clone)")
                count++;
        }
        return count;
    }

    private Component CreateComponent(string typeName, string objectName)
    {
        GameObject gameObject = new GameObject(objectName);
        objects.Add(gameObject);
        return gameObject.AddComponent(RuntimeType(typeName));
    }

    private static Type RuntimeType(string name)
    {
        return Type.GetType(name + ", Assembly-CSharp", true);
    }

    private static object Call(Component component, string method, params object[] arguments)
    {
        return component.GetType().GetMethod(method).Invoke(component, arguments);
    }

    private static void SetField(Object instance, string name, object value)
    {
        instance.GetType().GetField(name, PrivateInstance).SetValue(instance, value);
    }
}
