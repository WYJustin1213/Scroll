using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SpellIconSceneTests
{
    [Test]
    public void GameplayControllerUsesActiveCanvasIconDisplay()
    {
        Scene scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/SkellyScroll/Test1.unity");
        try
        {
            Type controllerType = Type.GetType("LaneSpellController, Assembly-CSharp", true);
            Component controller = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Component candidate in root.GetComponentsInChildren(controllerType, true))
                {
                    Assert.That(controller, Is.Null);
                    controller = candidate;
                }
            }

            Assert.That(controller, Is.Not.Null);
            FieldInfo displayField = controllerType.GetField("iconDisplay", BindingFlags.Instance | BindingFlags.NonPublic);
            Component display = (Component)displayField.GetValue(controller);
            Assert.That(display, Is.Not.Null);
            Assert.That(((Behaviour)display).enabled, Is.True);
            Assert.That(display.gameObject.activeInHierarchy, Is.True);
            Assert.That(display.GetComponentInParent<Canvas>(), Is.Not.Null);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
