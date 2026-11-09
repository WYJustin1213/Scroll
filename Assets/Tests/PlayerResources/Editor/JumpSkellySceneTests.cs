using System;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class JumpSkellySceneTests
{
    [Test]
    public void SceneInstanceHasEditableJumpAbilityAndValidLane()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/JumpSkelly.prefab");
        Type abilityType = Type.GetType("JumpOnSpellCast, Assembly-CSharp", true);
        Type enemyType = Type.GetType("LaneTestEnemy, Assembly-CSharp", true);
        Assert.That(prefab.GetComponent(abilityType), Is.Not.Null);

        Scene scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/SkellyScroll/Test1.unity");
        try
        {
            Component found = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Component candidate in root.GetComponentsInChildren(abilityType, true))
                {
                    if (candidate.gameObject.name == "JumpSkelly")
                    {
                        Assert.That(found, Is.Null, "Expected one JumpSkelly in the scene.");
                        found = candidate;
                    }
                }
            }

            Assert.That(found, Is.Not.Null);
            Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(found.gameObject), Is.EqualTo(prefab));
            SerializedObject ability = new SerializedObject(found);
            Assert.That(ability.FindProperty("jumpDuration").floatValue, Is.GreaterThan(0f));
            Assert.That(ability.FindProperty("jumpHeight").floatValue, Is.GreaterThan(0f));
            SerializedObject enemy = new SerializedObject(found.GetComponent(enemyType));
            Assert.That(enemy.FindProperty("lane").objectReferenceValue, Is.Not.Null);
            var definition = new SerializedObject(enemy.FindProperty("definition").objectReferenceValue);
            Assert.That(definition.FindProperty("ability").boolValue, Is.True);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
