using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class ProjectileCollisionTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private readonly List<Object> objects = new List<Object>();
    private Component lane;
    private Component projectile;

    [SetUp]
    public void SetUp()
    {
        lane = CreateComponent("LaneManager", "Lane");
        for (int i = 0; i < 4; i++)
        {
            Component tile = CreateComponent("LaneTile", "Tile");
            tile.transform.SetParent(lane.transform);
            tile.transform.localPosition = Vector3.forward * (2f * (i + 1));
        }

        Call(lane, "CollectTiles");
        projectile = CreateComponent("LaneFireballProjectile", "Projectile");
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
    public void SweepDetectsEnemyMovingAcrossProjectileBetweenFrames()
    {
        Component enemy = CreateEnemy(lane, 2, 5f);
        RememberPositions();
        enemy.transform.position = Vector3.forward;

        Assert.That(Sweep(2f, 3f), Is.EqualTo(enemy));
    }

    [Test]
    public void SweepHitsClosestEnemyAndIgnoresOtherLanes()
    {
        CreateEnemy(lane, 2, 5f);
        Component nearEnemy = CreateEnemy(lane, 1, 3f);
        Component otherLane = CreateComponent("LaneManager", "Other Lane");
        Component otherTile = CreateComponent("LaneTile", "Other Tile");
        otherTile.transform.SetParent(otherLane.transform);
        Call(otherLane, "CollectTiles");
        CreateEnemy(otherLane, 0, 2f);
        RememberPositions();

        Assert.That(Sweep(0f, 6f), Is.EqualTo(nearEnemy));
    }

    [Test]
    public void SweepDoesNotHitEnemiesBehindOrBeyondTheMovement()
    {
        CreateEnemy(lane, 0, 1f);
        CreateEnemy(lane, 2, 5f);
        RememberPositions();

        Assert.That(Sweep(2f, 3f), Is.Null);
    }

    [Test]
    public void SweepDetectsEnemySpawnedAfterPreviousSample()
    {
        RememberPositions();
        Component enemy = CreateEnemy(lane, 1, 3f);

        Assert.That(Sweep(2f, 4f), Is.EqualTo(enemy));
    }

    [UnityTest]
    public IEnumerator ProjectileDamagesEnemyBetweenTileCenters()
    {
        Component enemy = CreateEnemy(lane, 2, 3f);
        GiveHealthAndStopMoving(enemy);
        SetField(projectile, "moveSpeed", 1000f);

        Assert.That(Call(projectile, "Initialize", lane, 0, Enum.Parse(RuntimeType("SpellElement"), "Fire")), Is.True);
        yield return WaitForProjectile();

        Assert.That((int)enemy.GetType().GetProperty("Health").GetValue(enemy), Is.EqualTo(9));
        Assert.That(projectile == null, Is.True);
    }

    [UnityTest]
    public IEnumerator ProjectileDoesNotHitBeyondItsTileRange()
    {
        Component enemy = CreateEnemy(lane, 2, 3f);
        GiveHealthAndStopMoving(enemy);
        SetField(projectile, "moveSpeed", 1000f);
        SetField(projectile, "maxTileRange", 1);

        Assert.That(Call(projectile, "Initialize", lane, 0, Enum.Parse(RuntimeType("SpellElement"), "Fire")), Is.True);
        yield return WaitForProjectile();

        Assert.That((int)enemy.GetType().GetProperty("Health").GetValue(enemy), Is.EqualTo(10));
        Assert.That(projectile == null, Is.True);
    }

    [UnityTest]
    public IEnumerator ProjectileStillRespectsElementalImmunity()
    {
        Component enemy = CreateEnemy(lane, 2, 3f);
        Object definition = GiveHealthAndStopMoving(enemy);
        SetField(definition, "immunities", Enum.Parse(RuntimeType("ElementImmunity"), "Fire"));
        SetField(projectile, "moveSpeed", 1000f);

        Assert.That(Call(projectile, "Initialize", lane, 0, Enum.Parse(RuntimeType("SpellElement"), "Fire")), Is.True);
        yield return WaitForProjectile();

        Assert.That((int)enemy.GetType().GetProperty("Health").GetValue(enemy), Is.EqualTo(10));
        Assert.That(projectile == null, Is.True);
    }

    private IEnumerator WaitForProjectile()
    {
        float deadline = Time.realtimeSinceStartup + 2f;
        while (projectile != null && Time.realtimeSinceStartup < deadline)
            yield return null;
    }

    private Object GiveHealthAndStopMoving(Component enemy)
    {
        var definition = ScriptableObject.CreateInstance(RuntimeType("EnemyDefinition"));
        objects.Add(definition);
        SetField(enemy, "definition", definition);
        ((Behaviour)enemy).enabled = false;
        return definition;
    }

    private Component CreateEnemy(Component enemyLane, int tileIndex, float z)
    {
        Component enemy = CreateComponent("LaneTestEnemy", "Enemy");
        object tile = Call(enemyLane, "GetTile", tileIndex);
        Assert.That(Call(enemy, "InitializeAtTile", enemyLane, tile), Is.True);
        enemy.transform.position = new Vector3(enemyLane.transform.position.x, 0f, z);
        return enemy;
    }

    private Component CreateComponent(string typeName, string objectName)
    {
        var gameObject = new GameObject(objectName);
        objects.Add(gameObject);
        return gameObject.AddComponent(RuntimeType(typeName));
    }

    private void RememberPositions()
    {
        projectile.GetType().GetMethod("RememberEnemyPositions", PrivateInstance).Invoke(projectile, new object[] { lane });
    }

    private object Sweep(float start, float end)
    {
        return projectile.GetType().GetMethod("FindCrossedEnemy", PrivateInstance).Invoke(
            projectile, new object[] { lane, Vector3.forward * start, Vector3.forward * end, Vector3.forward });
    }

    private static Type RuntimeType(string name)
    {
        // Test assemblies cannot directly reference Unity's predefined Assembly-CSharp.
        return Type.GetType(name + ", Assembly-CSharp", true);
    }

    private static object Call(Component component, string method, params object[] arguments)
    {
        return component.GetType().GetMethod(method).Invoke(component, arguments);
    }

    private static void SetField(Object instance, string field, object value)
    {
        instance.GetType().GetField(field, PrivateInstance).SetValue(instance, value);
    }
}
