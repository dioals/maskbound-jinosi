using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using UnityEngine;

namespace MaskboundJinosi.AI
{
    /// <summary>
    /// Returns true when the paired AIActionPhaseAttack has finished: either the action asked
    /// the boss to chase again (player too far), or it finished its whole attack sequence.
    /// A hard timeout is kept as a safety net so the boss can never get stuck in the attack
    /// state if a weapon fails to report back.
    /// </summary>
    [AddComponentMenu("Maskbound/AI/Decisions/AI Decision Phase Attack Done")]
    public class AIDecisionPhaseAttackDone : AIDecision
    {
        /// <summary>
        /// The AIActionPhaseAttack this decision watches. Assigned in the inspector.
        /// </summary>
        [Tooltip("The AIActionPhaseAttack this decision watches.")]
        public AIActionPhaseAttack PhaseAttackAction;

        [Tooltip("Safety net: leave the attack state after this many seconds no matter what. Set to 0 to disable.")]
        [Min(0f)] public float MaximumStateDuration = 12f;

        public override bool Decide()
        {
            if (PhaseAttackAction == null)
            {
                PhaseAttackAction = GetComponent<AIActionPhaseAttack>();
            }

            if (PhaseAttackAction == null)
            {
                return false;
            }

            if (PhaseAttackAction.ShouldChase)
            {
                return true;
            }

            if (PhaseAttackAction.SequenceComplete)
            {
                return true;
            }

            if ((MaximumStateDuration > 0f) && (_brain != null) && (_brain.TimeInThisState >= MaximumStateDuration))
            {
                return true;
            }

            return false;
        }
    }
}
