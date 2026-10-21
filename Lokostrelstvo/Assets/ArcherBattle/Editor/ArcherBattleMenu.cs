using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ArcherBattle.EditorTools
{
    /// <summary>Menu "Archer Battle" in the Unity menu bar: puts the game object into the open scene.</summary>
    public static class ArcherBattleMenu
    {
        [MenuItem("Archer Battle/Add game to scene", false, 0)]
        static void AddToScene()
        {
            var existing = Object.FindAnyObjectByType<GameManager>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                EditorUtility.DisplayDialog("Archer Battle", "Igra je ze v sceni. Pritisni Play.", "OK");
                return;
            }
            var go = new GameObject("ArcherBattle");
            go.AddComponent<GameManager>();
            Undo.RegisterCreatedObjectUndo(go, "Add Archer Battle");
            Selection.activeGameObject = go;
            EditorSceneManager.MarkSceneDirty(go.scene);
        }

        [MenuItem("Archer Battle/Play", false, 1)]
        static void Play()
        {
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Archer Battle/Reset wins", false, 20)]
        static void ResetWins()
        {
            PlayerPrefs.DeleteKey("ArcherBattle.PlayerWins");
            PlayerPrefs.DeleteKey("ArcherBattle.AIWins");
            PlayerPrefs.Save();
        }
    }
}
