using MaskboundJinosi.AI;
using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using UnityEngine;

namespace MaskboundJinosi.AI
{
    [AddComponentMenu("Maskbound/AI/Actions/AI Action Move To Boss Skill Point")]
    public class AIActionMoveToBossSkillPoint : AIAction
    {
        [SerializeField] private string groupId = "prabu_klana_skill";
        [SerializeField] private int pointIndex;
        [SerializeField, Min(0.01f)] private float stopDistance = 0.2f;
        [SerializeField] private bool faceMoveDirection = true;

        private Character _character;
        private CharacterHorizontalMovement _horizontalMovement;

        public override void Initialization()
        {
            if (!ShouldInitialize)
            {
                return;
            }

            _character = GetComponentInParent<Character>();
            _horizontalMovement = _character?.FindAbility<CharacterHorizontalMovement>();
        }

        public override void PerformAction()
        {
            MoveToPoint();
        }

        public override void OnEnterState()
        {
            base.OnEnterState();
            _horizontalMovement?.SetHorizontalMove(0f);
        }

        public override void OnExitState()
        {
            base.OnExitState();
            _horizontalMovement?.SetHorizontalMove(0f);
        }

        private void MoveToPoint()
        {
            if (_horizontalMovement == null)
            {
                Initialization();
            }

            Transform point = ResolvePoint();
            if (_horizontalMovement == null || point == null)
            {
                return;
            }

            float deltaX = point.position.x - transform.position.x;
            if (Mathf.Abs(deltaX) <= stopDistance)
            {
                _horizontalMovement.SetHorizontalMove(0f);
                return;
            }

            float direction = Mathf.Sign(deltaX);
            _horizontalMovement.SetHorizontalMove(direction);

            if (faceMoveDirection && _character != null)
            {
                _character.Face(direction > 0f
                    ? Character.FacingDirections.Right
                    : Character.FacingDirections.Left);
            }
        }

        private Transform ResolvePoint()
        {
            return BossSkillSpawnPointGroup.TryGet(groupId, out BossSkillSpawnPointGroup group)
                ? group.GetPoint(pointIndex)
                : null;
        }
    }
}
