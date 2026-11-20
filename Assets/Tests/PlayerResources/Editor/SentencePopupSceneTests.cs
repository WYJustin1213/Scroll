using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SentencePopupSceneTests
{
    [Test]
    public void GameplaySceneHasValidPopupConfiguration()
    {
        Scene scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/SkellyScroll/Test1.unity");
        try
        {
            Type popupType = Type.GetType("SentencePopup, Assembly-CSharp", true);
            Component popup = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Component candidate in root.GetComponentsInChildren(popupType, true))
                {
                    Assert.That(popup, Is.Null, "Expected one sentence popup in the scene.");
                    popup = candidate;
                }

            }

            Assert.That(popup, Is.Not.Null);
            Assert.That(popup.gameObject.activeInHierarchy, Is.True);
            Assert.That(((Behaviour)popup).enabled, Is.True);
            MethodInfo validate = popupType.GetMethod("ValidateConfiguration", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(validate.Invoke(popup, null), Is.True);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    [Test]
    public void GameplaySceneConfiguresOneAutomaticSpellForEveryLane()
    {
        Scene scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/SkellyScroll/Test1.unity");
        try
        {
            Type automaticType = Type.GetType("SentencePopupLaneSpells, Assembly-CSharp", true);
            Type pointType = Type.GetType("LaneSpellPoint, Assembly-CSharp", true);
            Component automatic = null;
            var points = new System.Collections.Generic.HashSet<UnityEngine.Object>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Component point in root.GetComponentsInChildren(pointType, true))
                    points.Add(point);
                foreach (Component candidate in root.GetComponentsInChildren(automaticType, true))
                {
                    Assert.That(automatic, Is.Null);
                    automatic = candidate;
                }
            }

            Assert.That(automatic, Is.Not.Null);
            Assert.That(((Behaviour)automatic).enabled, Is.True);
            SerializedObject config = new SerializedObject(automatic);
            Assert.That(config.FindProperty("sentencePopup").objectReferenceValue, Is.Not.Null);
            Assert.That(config.FindProperty("spellController").objectReferenceValue, Is.Not.Null);
            SerializedProperty assignments = config.FindProperty("laneSpells");
            Assert.That(assignments.arraySize, Is.EqualTo(points.Count));
            for (int i = 0; i < assignments.arraySize; i++)
            {
                SerializedProperty entry = assignments.GetArrayElementAtIndex(i);
                Assert.That(points.Remove(entry.FindPropertyRelative("lane").objectReferenceValue), Is.True);
                Assert.That(entry.FindPropertyRelative("spell").objectReferenceValue, Is.Not.Null);
            }
            Assert.That(points, Is.Empty);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
