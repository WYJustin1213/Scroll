using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class LaneShockBeamTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private readonly List<Object> objects = new List<Object>();
    private readonly Component[] lanes = new Component[3];
    private Component beam;

    [SetUp]
    public void SetUp()
    {
        GameObject grid = NewObject("Shock test grid");
        for (int laneIndex = 0; laneIndex < lanes.Length; laneIndex++)
        {
            lanes[laneIndex] = Add("LaneManager", "Lane " + laneIndex);
            lanes[laneIndex].transform.SetParent(grid.transform);
            lanes[laneIndex].transform.position = Vector3.right * laneIndex * 3f;
            for (int tileIndex = 0; tileIndex < 4; tileIndex++)
            {
                Component tile = Add("LaneTile", "Tile");
                tile.transform.SetParent(lanes[laneIndex].transform, false);
                tile.transform.localPosition = Vector3.forward * tileIndex * 3f;
            }

            Call(lanes[laneIndex], "CollectTiles");
        }

        beam = Add("LaneShockBeam", "Shock Beam");
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = objects.Count - 1; i >= 0; i--)
        {
            if (objects[i] != null)
                Object.DestroyImmediate(objects[i]);
        }
        objects.Clear();
    }

    [Test]
    public void BehindEnemyIsPreferredToAnAdjacentLane()
    {
        Component current = CreateEnemy(lanes[1], 1);
        Component behind = CreateEnemy(lanes[1], 2);
        CreateEnemy(lanes[2], 1);

        Assert.That(FindNext(current), Is.EqualTo(behind));
    }

    [Test]
    public void AdjacentLaneIsUsedWhenThereIsNoEnemyBehind()
    {
        Component current = CreateEnemy(lanes[1], 1);
        Component adjacent = CreateEnemy(lanes[2], 1);

        Assert.That(FindNext(current), Is.EqualTo(adjacent));
    }

    [Test]
    public void DoesNotTargetEnemiesMoreThanOneTileAway()
    {
        Component current = CreateEnemy(lanes[0], 1);
        CreateEnemy(lanes[0], 3);
        CreateEnemy(lanes[2], 1);

        Assert.That(FindNext(current), Is.Null);
    }

    [Test]
    public void SkipsPreviouslyHitEnemyWhenFindingTheNextTarget()
    {
        Component current = CreateEnemy(lanes[1], 1);
        Component behind = CreateEnemy(lanes[1], 2);
        Component adjacent = CreateEnemy(lanes[2], 1);
        HashSet<Component> hitEnemies = new HashSet<Component> { behind };

        Assert.That(FindNext(current, hitEnemies), Is.EqualTo(adjacent));
    }

    [UnityTest]
    public IEnumerator SpellDefinitionTransformsTravelAndChainTrailSprites()
    {
        Texture2D texture = new Texture2D(2, 2);
        objects.Add(texture);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f);
        objects.Add(sprite);
        Set(beam, "trailSprite", sprite);

        Object spell = ScriptableObject.CreateInstance(RuntimeType("SpellDefinition"));
        objects.Add(spell);
        Set(spell, "function", Enum.Parse(RuntimeType("SpellFunction"), "Beam"));
        Set(spell, "ShockBeamPrefab", beam);
        Vector3 offset = new Vector3(1f, 2f, -3f);
        Vector3 rotation = new Vector3(30f, 45f, 60f);
        Set(spell, "shockTrailPositionOffset", offset);
        Set(spell, "shockTrailRotation", rotation);

        Component resources = Add("PlayerResources", "Resources");
        Component controller = Add("LaneSpellController", "Controller");
        Set(controller, "playerResources", resources);
        Component point = Add("LaneSpellPoint", "Point");
        Set(point, "lane", lanes[0]);
        point.transform.position = new Vector3(0f, 1f, -1f);

        Assert.That(Call(controller, "CastSpell", spell, point), Is.True);
        Component clone = null;
        foreach (Object candidate in Object.FindObjectsByType(RuntimeType("LaneShockBeam"), FindObjectsSortMode.None))
        {
            Component component = (Component)candidate;
            if (component.name == beam.name + "(Clone)")
            {
                clone = component;
                break;
            }
        }

        Assert.That(clone, Is.Not.Null);
        objects.Add(clone.gameObject);
        Transform root = (Transform)clone.GetType().GetField("trailRoot", PrivateInstance).GetValue(clone);
        objects.Add(root.gameObject);
        AssertTrailTransform(root.GetChild(0), point.transform.position + offset, rotation);

        Vector3 chainEnd = new Vector3(3f, 1f, 5f);
        clone.GetType().GetMethod("AddTrailBetween", PrivateInstance)
            .Invoke(clone, new object[] { Vector3.zero, chainEnd });
        AssertTrailTransform(root.GetChild(root.childCount - 1), chainEnd + offset, rotation);
        yield return null;
    }

    private static void AssertTrailTransform(Transform segment, Vector3 position, Vector3 rotation)
    {
        Assert.That(Vector3.Distance(segment.position, position), Is.LessThan(0.0001f));
        Assert.That(Quaternion.Angle(segment.rotation, Quaternion.Euler(rotation)), Is.LessThan(0.01f));
    }

    private static void Set(Object instance, string field, object value) =>
        instance.GetType().GetField(field, PrivateInstance).SetValue(instance, value);

    private Component CreateEnemy(Component lane, int tileIndex)
    {
        Component enemy = Add("LaneTestEnemy", "Enemy");
        Assert.That(Call(enemy, "InitializeAtTile", lane, Call(lane, "GetTile", tileIndex)), Is.True);
        return enemy;
    }

    private object FindNext(Component current, HashSet<Component> hitEnemies = null)
    {
        Type enemyType = RuntimeType("LaneTestEnemy");
        var typedHitEnemies = Activator.CreateInstance(typeof(HashSet<>).MakeGenericType(enemyType));
        MethodInfo add = typedHitEnemies.GetType().GetMethod("Add");
        if (hitEnemies != null)
        {
            foreach (Component enemy in hitEnemies)
                add.Invoke(typedHitEnemies, new object[] { enemy });
        }

        return beam.GetType().GetMethod("FindNextTarget", PrivateInstance)
            .Invoke(beam, new[] { (object)current, typedHitEnemies });
    }

    private GameObject NewObject(string name)
    {
        var result = new GameObject(name);
        objects.Add(result);
        return result;
    }

    private Component Add(string type, string name) => NewObject(name).AddComponent(RuntimeType(type));
    private static Type RuntimeType(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Call(Component instance, string method, params object[] args) =>
        instance.GetType().GetMethod(method).Invoke(instance, args);
}
