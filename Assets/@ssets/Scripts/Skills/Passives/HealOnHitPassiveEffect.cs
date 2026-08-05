using MoreMountains.CorgiEngine;
using UnityEngine;

namespace MaskboundJinosi.Skills.Passives
{
    [CreateAssetMenu(fileName = "HealOnHitEffect", menuName = "Maskbound/Skills/Passive Effects/Heal On Hit")]
    public class HealOnHitPassiveEffect : PassiveEffectData
    {
        [Tooltip("0.08 berarti memulihkan 8% dari damage final.")]
        [Range(0f, 1f)] public float DamageRecoveryRatio = 0.08f;

        public override PassiveEffectRuntime CreateRuntime(PlayerPassiveSkillController controller)
        {
            return new Runtime(controller, this);
        }

        private sealed class Runtime : PassiveEffectRuntime
        {
            private readonly HealOnHitPassiveEffect _data;

            public Runtime(PlayerPassiveSkillController controller, HealOnHitPassiveEffect data) : base(controller)
            {
                _data = data;
            }

            public override void OnDamageDealt(MMDamageTakenEvent damageEvent)
            {
                Health health = Controller.PlayerHealth;
                if (health != null && damageEvent.DamageCaused > 0f)
                {
                    health.GetHealth(damageEvent.DamageCaused * _data.DamageRecoveryRatio, Controller.gameObject);
                }
            }
        }
    }
}
