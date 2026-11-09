using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class JumpOnSpellCastTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private readonly List<Object> objects = new List<Object>();
    private readonly Component[] lanes = new Component[3];
    private Component enemy;
    private Component controller;
    private Component point;
    private Component resources;
    private Object definition;
    private Object spell;

    [SetUp]
    public void SetUp()
    {
        GameObject grid = NewObject("Jump test grid");
        for (int laneIndex = 0; laneIndex < lanes.Length; laneIndex++)
        {
            lanes[laneIndex] = Add("LaneManager", "Lane " + laneIndex);
            lanes[laneIndex].transform.SetParent(grid.transform);
            lanes[laneIndex].transform.position = Vector3.right * laneIndex * 3f;
            for (int index = 0; index < 3; index++)
            {
                Component tile = Add("LaneTile", "Tile");
                tile.transform.SetParent(lanes[laneIndex].transform, false);
                tile.transform.localPosition = Vector3.forward * index * 3f;
            }
            Call(lanes[laneIndex], "CollectTiles");
        }

        definition = ScriptableObject.CreateInstance(TypeOf("EnemyDefinition"));
        objects.Add(definition);
        Set(definition, "ability", true);
        Set(definition, "moveInterval", 1000f);
        enemy = CreateEnemy(0, 1);
        enemy.gameObject.AddComponent(TypeOf("JumpOnSpellCast"));

        resources = Add("PlayerResources", "Resources");
        Call(resources, "SetMana", 10);
        controller = Add("LaneSpellController", "Controller");
        Set(controller, "playerResources", resources);
        point = Add("LaneSpellPoint", "Point");
        Set(point, "lane", lanes[0]);
        Component projectile = Add("LaneFireballProjectile", "Jump Test Projectile");
        spell = ScriptableObject.CreateInstance(TypeOf("SpellDefinition"));
        objects.Add(spell);
        Set(spell, "projectilePrefab", projectile);
        Set(spell, "cooldown", 0f);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (Object projectile in Object.FindObjectsByType(TypeOf("LaneFireballProjectile"), FindObjectsSortMode.None))
        {
            Component component = (Component)projectile;
            if (component.name == "Jump Test Projectile(Clone)")
                Object.DestroyImmediate(component.gameObject);
        }

        for (int i = objects.Count - 1; i >= 0; i--)
        {
            if (objects[i] != null)
                Object.DestroyImmediate(objects[i]);
        }
        objects.Clear();
    }

    [Test]
    public void CastUsesAvailableOtherLaneOnSameRow()
    {
        CreateEnemy(1, 1);
        Assert.That(Cast(), Is.True);
        AssertDestination(2, 1);
    }

    [Test]
    public void OpenLanesAreRandomlySelected()
    {
        Set(spell, "manaCost", 0);
        UnityEngine.Random.State previousState = UnityEngine.Random.state;
        try
        {
            UnityEngine.Random.InitState(12345);
            var destinations = new HashSet<object>();
            for (int i = 0; i < 30; i++)
            {
                Assert.That(Call(enemy, "InitializeAtTile", lanes[0], Call(lanes[0], "GetTile", 1)), Is.True);
                Assert.That(Cast(), Is.True);
                destinations.Add(enemy.GetType().GetProperty("Lane").GetValue(enemy));
            }
            Assert.That(destinations, Is.EquivalentTo(new object[] { lanes[1], lanes[2] }));
        }
        finally
        {
            UnityEngine.Random.state = previousState;
        }
    }

    [Test]
    public void FullRowUsesNearestAvailableFartherRow()
    {
        CreateEnemy(1, 1);
        CreateEnemy(2, 1);
        CreateEnemy(1, 2);
        Assert.That(Cast(), Is.True);
        AssertDestination(2, 2);
    }

    [Test]
    public void FullFartherRowsUseNearestAvailableCloserRow()
    {
        CreateEnemy(1, 1);
        CreateEnemy(2, 1);
        CreateEnemy(1, 2);
        CreateEnemy(2, 2);
        CreateEnemy(1, 0);
        Assert.That(Cast(), Is.True);
        AssertDestination(2, 0);
    }

    [Test]
    public void OccupiedPlayerTileIsNotAvailable()
    {
        CreateEnemy(1, 1);
        Component player = Add("LanePlayerMarker", "Player");
        Set(player, "lane", lanes[2]);
        Set(player, "tileIndex", 1);
        CreateEnemy(1, 2);
        Assert.That(Cast(), Is.True);
        AssertDestination(2, 2);
    }

    [Test]
    public void NoAvailableOtherTileLeavesEnemyInPlace()
    {
        for (int lane = 1; lane < 3; lane++)
            for (int index = 0; index < 3; index++)
                CreateEnemy(lane, index);
        Assert.That(Cast(), Is.True);
        AssertDestination(0, 1);
    }

    [Test]
    public void FailedCastAndDisabledAbilityDoNotMoveEnemy()
    {
        Call(resources, "SetMana", 0);
        LogAssert.Expect(LogType.Warning, "Not enough mana to cast \"\".");
        Assert.That(Cast(), Is.False);
        AssertDestination(0, 1);

        Call(resources, "SetMana", 10);
        Set(definition, "ability", false);
        Assert.That(Cast(), Is.True);
        AssertDestination(0, 1);
    }

    [Test]
    public void SapAlsoTriggersJump()
    {
        GameObject target = NewObject("Sap target prefab");
        Component marker = target.AddComponent(TypeOf("SapTarget"));
        Set(marker, "sourcePrefab", target);
        Set(spell, "function", Enum.Parse(TypeOf("SpellFunction"), "Sap"));
        Set(spell, "sapTargetPrefab", target);
        CreateEnemy(1, 1);
        Assert.That(Cast(), Is.True);
        AssertDestination(2, 1);
    }

    [Test]
    public void SapDeletesConfiguredPrefabAndDropManaTaggedObjects()
    {
        GameObject targetPrefab = NewObject("Sap target prefab");
        Component marker = targetPrefab.AddComponent(TypeOf("SapTarget"));
        Set(marker, "sourcePrefab", targetPrefab);

        GameObject taggedTarget = NewObject("Tagged mana drop");
        taggedTarget.tag = "DropMana";
        taggedTarget.SetActive(false);

        Set(spell, "function", Enum.Parse(TypeOf("SpellFunction"), "Sap"));
        Set(spell, "sapTargetPrefab", targetPrefab);
        Set(spell, "manaCost", 0);
        int initialMana = (int)resources.GetType().GetProperty("Mana").GetValue(resources);

        Assert.That(Cast(), Is.True);
        Assert.That((int)resources.GetType().GetProperty("Mana").GetValue(resources), Is.EqualTo(initialMana + 2));
    }

    [Test]
    public void SapRestoresOneManaWhenNoDropsExist()
    {
        Set(spell, "function", Enum.Parse(TypeOf("SpellFunction"), "Sap"));
        Set(spell, "manaCost", 0);
        int initialMana = (int)resources.GetType().GetProperty("Mana").GetValue(resources);

        Assert.That(Cast(), Is.True);
        Assert.That((int)resources.GetType().GetProperty("Mana").GetValue(resources), Is.EqualTo(initialMana + 1));
    }

    [UnityTest]
    public IEnumerator JumpAnimatesAndAnotherCastRetargetsFromCurrentPosition()
    {
        CreateEnemy(1, 1);
        Vector3 start = enemy.transform.position;
        Assert.That(Cast(), Is.True);
        AssertDestination(2, 1);
        Assert.That(enemy.transform.position, Is.EqualTo(start));
        yield return new WaitForSeconds(0.1f);
        Vector3 inFlight = enemy.transform.position;
        Assert.That(inFlight.y, Is.GreaterThan(start.y));
        Assert.That(inFlight.x, Is.GreaterThan(start.x));

        Assert.That(Cast(), Is.True);
        AssertDestination(0, 1);
        Assert.That(enemy.transform.position, Is.EqualTo(inFlight));
        yield return new WaitForSeconds(0.45f);
        Vector3 destination = ((Component)Call(lanes[0], "GetTile", 1)).transform.position;
        Assert.That(enemy.transform.position.x, Is.EqualTo(destination.x).Within(0.01f));
        Assert.That(enemy.transform.position.y, Is.EqualTo(start.y).Within(0.01f));
    }

    private Component CreateEnemy(int lane, int index)
    {
        Component result = Add("LaneTestEnemy", "Enemy");
        Set(result, "definition", definition);
        Assert.That(Call(result, "InitializeAtTile", lanes[lane], Call(lanes[lane], "GetTile", index)), Is.True);
        return result;
    }

    private void AssertDestination(int lane, int index)
    {
        Assert.That(enemy.GetType().GetProperty("Lane").GetValue(enemy), Is.EqualTo(lanes[lane]));
        Assert.That(enemy.GetType().GetProperty("CurrentTile").GetValue(enemy),
            Is.EqualTo(Call(lanes[lane], "GetTile", index)));
    }

    private bool Cast() => (bool)Call(controller, "CastSpell", spell, point);

    private GameObject NewObject(string name)
    {
        var result = new GameObject(name);
        objects.Add(result);
        return result;
    }

    private Component Add(string type, string name) => NewObject(name).AddComponent(TypeOf(type));
    private static Type TypeOf(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Call(Component instance, string method, params object[] args) =>
        instance.GetType().GetMethod(method).Invoke(instance, args);
    private static void Set(Object instance, string name, object value) =>
        instance.GetType().GetField(name, PrivateInstance).SetValue(instance, value);
}
