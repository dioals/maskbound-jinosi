using MaskboundJinosi.Gameplay.Scene;
using UnityEngine;

namespace MaskboundJinosi.Gameplay.Dialogue
{
    /// <summary>
    /// Same-scene bridge so a Fungus "Call Method" command can show the
    /// game-over overlay. Attach to the Flowchart GameObject itself: the Unity
    /// editor cannot reference the persistent BootstrapRoot across scenes, so
    /// this resolves PlayerGameOverOverlayController at runtime instead.
    /// Fungus: Call Method, target = this GameObject, method = ShowGameOver.
    /// </summary>
    [AddComponentMenu("Maskbound/Dialogue/Game Over Fungus Bridge")]
    public class GameOverFungusBridge : MonoBehaviour
    {
        public virtual void ShowGameOver()
        {
            PlayerGameOverOverlayController controller =
                FindFirstObjectByType<PlayerGameOverOverlayController>(FindObjectsInactive.Include);

            if (controller == null)
            {
                Debug.LogWarning("[GameOverFungusBridge] PlayerGameOverOverlayController not found.", this);
                return;
            }

            controller.ShowGameOverFromFungus();
        }
    }
}
