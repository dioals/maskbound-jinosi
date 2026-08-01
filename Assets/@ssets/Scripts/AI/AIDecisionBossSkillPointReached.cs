using MoreMountains.Tools;
using UnityEngine;

namespace MaskboundJinosi.AI
{
    [AddComponentMenu("Maskbound/AI/Decisions/AI Decision Boss Skill Point Reached")]
    public class AIDecisionBossSkillPointReached : AIDecision
    {
        [SerializeField] private string groupId = "prabu_klana_skill";
        [SerializeField] private int pointIndex;
        [SerializeField, Min(0.01f)] private float distance = 0.25f;
        [SerializeField] private bool horizontalOnly = true;

        public override bool Decide()
        {
            if (!BossSkillSpawnPointGroup.TryGet(groupId, out BossSkillSpawnPointGroup group))
            {
                return false;
            }

            Transform point = group.GetPoint(pointIndex);
            if (point == null)
            {
                return false;
            }

            if (horizontalOnly)
            {
                return Mathf.Abs(transform.position.x - point.position.x) <= distance;
            }

            return Vector2.Distance(transform.position, point.position) <= distance;
        }
    }
}
