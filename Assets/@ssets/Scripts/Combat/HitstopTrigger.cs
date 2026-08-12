using MoreMountains.Tools;
using UnityEngine;

namespace MaskboundJinosi.Combat
{
    /// <summary>
    /// Shared entry point for hitstop (freeze frame on hit). Wraps MMFreezeFrameEvent so every
    /// weapon/skill can expose its own tunable duration without duplicating the timescale logic.
    /// </summary>
    public static class HitstopTrigger
    {
        public static void Trigger(float duration)
        {
            if ((duration > 0f) && (Time.timeScale > 0f))
            {
                MMFreezeFrameEvent.Trigger(duration);
            }
        }
    }
}
