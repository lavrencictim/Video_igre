using UnityEngine;

namespace ArcherBattle
{
    /// <summary>
    /// Starts the game automatically when you press Play in any scene of this project:
    /// if the scene has no GameManager yet, one is created and it builds everything else.
    /// (You can also add GameManager to an empty GameObject yourself.)
    /// </summary>
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Object.FindAnyObjectByType<GameManager>() != null) return;
            new GameObject("ArcherBattle").AddComponent<GameManager>();
        }
    }
}
