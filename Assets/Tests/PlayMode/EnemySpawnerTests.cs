using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public class EnemySpawnerTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private readonly List<Object> objects = new List<Object>();
    private Component lane;
    private Component tile;
    private Component spawner;
    private Random.State randomState;

    [SetUp]
    public void SetUp()
    {
        randomState = Random.state;
        lane = CreateComponent("LaneManager", "Spawner test lane");
        tile = CreateComponent("LaneTile", "Spawner test tile");
        tile.transform.SetParent(lane.transform);
        Call(lane, "CollectTiles");
        spawner = CreateComponent("EnemySpawner", "Spawner test");
        ((Behaviour)spawner).enabled = false;
        Set(spawner, "spawnTile", tile);
    }

    [TearDown]
    public void TearDown()
    {
        Random.state = randomState;
        for (int i = objects.Count - 1; i >= 0; i--)
        {
            if (objects[i] != null)
                Object.DestroyImmediate(objects[i]);
        }
        objects.Clear();
    }

    [Test]
    public void EqualModeChoosesEachPrefabAtTheSameRate()
    {
        Component common = AddEnemy("Common", "Common");
        Component rare = AddEnemy("Rare", "Rare");
        int[] counts = Sample(common, rare);

        Assert.That(counts[0], Is.InRange(4700, 5300));
        Assert.That(counts[1], Is.EqualTo(10000 - counts[0]));
    }

    [Test]
    public void TieredModeRollsTierThenSharesItsChanceEqually()
    {
        Component commonA = AddEnemy("Common A", "Common");
        Component commonB = AddEnemy("Common B", "Common");
        Component rare = AddEnemy("Rare", "Rare");
        Component epic = AddEnemy("Epic", "Epic");
        SetEnum(spawner, "selectionMode", "Tiered");
        int[] counts = Sample(commonA, commonB, rare, epic);

        Assert.That(counts[0], Is.InRange(3200, 3800));
        Assert.That(counts[1], Is.InRange(3200, 3800));
        Assert.That(counts[2], Is.InRange(2200, 2800));
        Assert.That(counts[3], Is.InRange(400, 600));
    }

    [Test]
    public void EmptySelectedTierSkipsAndWarns()
    {
        AddEnemy("Common", "Common");
        SetEnum(spawner, "selectionMode", "Tiered");
        Set(spawner, "commonChance", 0);
        Set(spawner, "rareChance", 100);
        Set(spawner, "epicChance", 0);
        LogAssert.Expect(LogType.Warning, "EnemySpawner skipped a spawn because its Rare tier is empty.");
        Call(spawner, "Spawn");
        Assert.That(lane.transform.childCount, Is.EqualTo(1));
    }

    [Test]
    public void InvalidTierChancesDoNotSpawn()
    {
        AddEnemy("Common", "Common");
        SetEnum(spawner, "selectionMode", "Tiered");
        Set(spawner, "commonChance", 71);
        LogAssert.Expect(LogType.Error, "EnemySpawner tier chances must each be between 0 and 100 and total 100%.");
        Call(spawner, "Spawn");
        Assert.That(lane.transform.childCount, Is.EqualTo(1));
    }

    [Test]
    public void SpawnInitializesTheEnemyAndSkipsAnOccupiedTile()
    {
        AddEnemy("Common", "Common");
        Call(spawner, "Spawn");
        Assert.That(lane.transform.childCount, Is.EqualTo(2));
        Component enemy = lane.transform.GetChild(1).GetComponent(TypeOf("LaneTestEnemy"));
        Assert.That(enemy.GetType().GetProperty("Lane").GetValue(enemy), Is.EqualTo(lane));
        Assert.That(enemy.GetType().GetProperty("CurrentTile").GetValue(enemy), Is.EqualTo(tile));
        Call(spawner, "Spawn");
        Assert.That(lane.transform.childCount, Is.EqualTo(2));
    }

    [Test]
    public void SpawnSkipsATileOccupiedByThePlayer()
    {
        AddEnemy("Common", "Common");
        Component player = CreateComponent("LanePlayerMarker", "Spawner test player");
        Set(player, "lane", lane);
        Set(player, "tileIndex", 0);
        Call(spawner, "Spawn");
        Assert.That(lane.transform.childCount, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator FirstSpawnWaitsForConfiguredInterval()
    {
        AddEnemy("Common", "Common");
        Set(spawner, "spawnInterval", 0.3f);
        ((Behaviour)spawner).enabled = true;
        yield return new WaitForSeconds(0.1f);
        Assert.That(lane.transform.childCount, Is.EqualTo(1));
        yield return new WaitForSeconds(0.35f);
        Assert.That(lane.transform.childCount, Is.EqualTo(2));
        ((Behaviour)spawner).enabled = false;
    }

    [UnityTest]
    public IEnumerator Test1HasAConfiguredSpawnerAtEachLane()
    {
        yield return SceneManager.LoadSceneAsync("Test1", LoadSceneMode.Additive);
        Scene scene = SceneManager.GetSceneByName("Test1");
        int count = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Component sceneSpawner in root.GetComponentsInChildren(TypeOf("EnemySpawner"), true))
            {
                Component sceneTile = (Component)Get(sceneSpawner, "spawnTile");
                Assert.That(sceneTile, Is.Not.Null);
                object tileLane = sceneTile.GetType().GetProperty("Lane").GetValue(sceneTile);
                Assert.That(tileLane, Is.EqualTo(sceneSpawner.transform.parent.GetComponent(TypeOf("LaneManager"))));
                Assert.That((float)Get(sceneSpawner, "spawnInterval"), Is.EqualTo(5f));
                Assert.That((IList)Get(sceneSpawner, "enemyPrefabs"), Is.Empty);
                count++;
            }
        }
        Assert.That(count, Is.EqualTo(5));
        yield return SceneManager.UnloadSceneAsync(scene);
    }

    private int[] Sample(params Component[] candidates)
    {
        Random.InitState(42);
        int[] counts = new int[candidates.Length];
        for (int i = 0; i < 10000; i++)
        {
            object selected = Call(spawner, "SelectPrefab");
            int index = Array.IndexOf(candidates, selected);
            Assert.That(index, Is.GreaterThanOrEqualTo(0));
            counts[index]++;
        }
        return counts;
    }

    private Component AddEnemy(string name, string tier)
    {
        Object definition = ScriptableObject.CreateInstance(TypeOf("EnemyDefinition"));
        objects.Add(definition);
        SetEnum(definition, "spawnTier", tier);
        Component enemy = CreateComponent("LaneTestEnemy", name);
        Set(enemy, "definition", definition);
        ((IList)Get(spawner, "enemyPrefabs")).Add(enemy);
        return enemy;
    }

    private Component CreateComponent(string type, string name)
    {
        var gameObject = new GameObject(name);
        objects.Add(gameObject);
        return gameObject.AddComponent(TypeOf(type));
    }

    private static Type TypeOf(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Call(Object target, string method) =>
        target.GetType().GetMethod(method, PrivateInstance | BindingFlags.Public).Invoke(target, null);
    private static object Get(Object target, string field) =>
        target.GetType().GetField(field, PrivateInstance).GetValue(target);
    private static void Set(Object target, string field, object value) =>
        target.GetType().GetField(field, PrivateInstance).SetValue(target, value);
    private static void SetEnum(Object target, string field, string value)
    {
        FieldInfo info = target.GetType().GetField(field, PrivateInstance);
        info.SetValue(target, Enum.Parse(info.FieldType, value));
    }
}
