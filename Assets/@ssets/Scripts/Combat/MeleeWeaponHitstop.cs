using MoreMountains.CorgiEngine;
using UnityEngine;

namespace MaskboundJinosi.Combat
{
    /// <summary>
    /// Stock Corgi MeleeWeapon with a per-weapon hitstop duration on top, so each attack can
    /// freeze the game briefly on a successful hit without any extra wiring.
    /// </summary>
    [AddComponentMenu("Maskbound/Combat/Melee Weapon (Hitstop)")]
    public class MeleeWeaponHitstop : MeleeWeapon
    {
        [Header("Hitstop")]
        [Tooltip("How long (in real seconds) to freeze the game when this weapon lands a hit. 0 disables hitstop for this attack.")]
        [SerializeField, Min(0f)] private float hitstopDuration = 0.05f;

        public override void WeaponHitDamageable()
        {
            base.WeaponHitDamageable();
            HitstopTrigger.Trigger(hitstopDuration);
        }
    }
}
