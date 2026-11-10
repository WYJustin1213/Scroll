using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public class EnemyManaDropTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private readonly List<Object> objects = new List<Object>();
    private Component enemy;
    private Object definition;
    private GameObject manaPrefab;

    [SetUp]
    public void SetUp()
    {
        definition = ScriptableObject.CreateInstance(TypeOf("EnemyDefinition"));
        objects.Add(definition);
        Set(definition, "health", 2);

        manaPrefab = new GameObject("Enemy mana drop test");
        objects.Add(manaPrefab);
        Set(definition, "manaPrefab", manaPrefab);

        var enemyObject = new GameObject("Enemy mana drop test enemy");
        objects.Add(enemyObject);
        enemy = enemyObject.AddComponent(TypeOf("LaneTestEnemy"));
        enemy.transform.position = new Vector3(4f, 5f, 6f);
        Set(enemy, "definition", definition);
        Set(enemy, "manaDropYOffset", 2f);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject drop in FindDrops())
            Object.DestroyImmediate(drop);

        for (int i = objects.Count - 1; i >= 0; i--)
        {
            if (objects[i] != null)
                Object.DestroyImmediate(objects[i]);
        }
        objects.Clear();
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(5)]
    public void DeathSpawnsDefinitionDropCountAtSeparatedPositions(int count)
    {
        Set(definition, "drop", count);
        Damage(2);
        Damage(2);

        GameObject[] drops = FindDrops();
        Assert.That(drops.Length, Is.EqualTo(count));
        Vector3 center = new Vector3(4f, 7f, 6f);
        Vector3 sum = Vector3.zero;
        for (int i = 0; i < drops.Length; i++)
        {
            sum += drops[i].transform.position;
            for (int j = 0; j < i; j++)
            {
                Vector3 difference = drops[i].transform.position - drops[j].transform.position;
                Assert.That(Mathf.Abs(difference.x), Is.GreaterThanOrEqualTo(1.75f - 0.0001f));
                Assert.That(Mathf.Abs(difference.y), Is.GreaterThan(0f));
                Assert.That(Mathf.Abs(difference.z), Is.GreaterThan(0f));
            }
        }

        if (count > 0)
            Assert.That(Vector3.Distance(sum / count, center), Is.LessThan(0.0001f));
    }

    [Test]
    public void NonlethalDamageDoesNotSpawnMana()
    {
        Set(definition, "drop", 3);
        Damage(1);
        Assert.That(FindDrops(), Is.Empty);
    }

    [Test]
    public void DeathWithoutManaPrefabDoesNotSpawnMana()
    {
        Set(definition, "drop", 3);
        Set(definition, "manaPrefab", null);
        Damage(2);
        Assert.That(FindDrops(), Is.Empty);
    }

    private void Damage(int amount)
    {
        enemy.GetType().GetMethod("TakeDamage").Invoke(
            enemy, new object[] { amount, Enum.Parse(TypeOf("SpellElement"), "None") });
    }

    private static GameObject[] FindDrops()
    {
        var drops = new List<GameObject>();
        foreach (GameObject candidate in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (candidate.name == "Enemy mana drop test(Clone)")
                drops.Add(candidate);
        }
        return drops.ToArray();
    }

    private static Type TypeOf(string name)
    {
        return Type.GetType(name + ", Assembly-CSharp", true);
    }

    private static void Set(Object instance, string field, object value)
    {
        instance.GetType().GetField(field, PrivateInstance).SetValue(instance, value);
    }
}
