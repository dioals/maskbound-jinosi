using MoreMountains.Feedbacks;
using UnityEngine;

namespace MaskboundJinosi.Gameplay
{
    /// <summary>
    /// Hitstop relies on MMFreezeFrameEvent, which only does anything if an MMTimeManager is
    /// listening. Nothing in the project places one in a scene, so we spin one up once, before
    /// the first scene loads, and keep it alive for the life of the game.
    /// </summary>
    internal static class HitstopBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureTimeManagerExists()
        {
            if (MMTimeManager.HasInstance)
            {
                return;
            }

            GameObject timeManager = new GameObject("MMTimeManager_AutoCreated");
            timeManager.AddComponent<MMTimeManager>();
            Object.DontDestroyOnLoad(timeManager);
        }
    }
}
