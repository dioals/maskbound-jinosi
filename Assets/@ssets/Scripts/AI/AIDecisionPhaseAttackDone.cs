using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using UnityEngine;

namespace MaskboundJinosi.AI
{
    /// <summary>
    /// Returns true when the paired AIActionPhaseAttack has finished: either the action asked
    /// the boss to chase again (player too far), or the chosen attack's duration has elapsed
    /// since entering the state.
    /// </summary>
    [AddComponentMenu("Maskbound/AI/Decisions/AI Decision Phase Attack Done")]
    public class AIDecisionPhaseAttackDone : AIDecision
    {
        /// <summary>
        /// The AIActionPhaseAttack this decision watches. Assigned in the inspector.
        /// </summary>
        [Tooltip("The AIActionPhaseAttack this decision watches.")]
        public AIActionPhaseAttack PhaseAttackAction;

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

            if (PhaseAttackAction.CurrentAttackDuration <= 0f)
            {
                return false;
            }

            return _brain != null && _brain.TimeInThisState >= PhaseAttackAction.CurrentAttackDuration;
        }
    }
}
