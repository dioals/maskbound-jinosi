using UnityEngine;

namespace MaskboundJinosi.Gameplay
{
    /// <summary>
    /// Kept as a no-op Animation Event target so existing events on landing clips don't error.
    /// Landing no longer blocks horizontal movement — players can move immediately on touchdown.
    /// </summary>
    public class LandingMovementLock : MonoBehaviour
    {
        public void DisableHorizontalMovement()
        {
        }

        public void EnableHorizontalMovement()
        {
        }
    }
}
