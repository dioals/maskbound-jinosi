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
        private CorgiController _controller;
        private Health _health;

        protected override void Awake()
        {
            base.Awake();
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            if (_health != null)
            {
                _health.OnDeath -= StopMovement;
                _health.OnDeath += StopMovement;
            }
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.OnDeath -= StopMovement;
            }

            StopMovement();
        }

        public override void Initialization()
        {
            if (!ShouldInitialize)
            {
                return;
            }

            ResolveReferences();
            base.Initialization();
        }

        public override void PerformAction()
        {
            if (IsDead())
            {
                StopMovement();
                return;
            }

            MoveToPoint();
        }

        public override void OnEnterState()
        {
            base.OnEnterState();
            StopMovement();
        }

        public override void OnExitState()
        {
            base.OnExitState();
            StopMovement();
        }

        private void MoveToPoint()
        {
            if (_horizontalMovement == null)
            {
                Initialization();
            }

            if (IsDead())
            {
                StopMovement();
                return;
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

        private bool IsDead()
        {
            return _character != null
                && _character.ConditionState.CurrentState == CharacterStates.CharacterConditions.Dead;
        }

        private void StopMovement()
        {
            _horizontalMovement?.SetHorizontalMove(0f);
            _controller?.SetHorizontalForce(0f);
        }

        private void ResolveReferences()
        {
            if (_character == null)
            {
                _character = GetComponentInParent<Character>();
            }

            if (_horizontalMovement == null)
            {
                _horizontalMovement = _character?.FindAbility<CharacterHorizontalMovement>();
            }

            if (_controller == null)
            {
                _controller = _character != null ? _character.GetComponent<CorgiController>() : null;
            }

            if (_health == null)
            {
                _health = _character != null ? _character.GetComponent<Health>() : null;
            }
        }
    }
}
