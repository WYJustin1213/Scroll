using System;
using System.Reflection;
using NUnit.Framework;
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
}
