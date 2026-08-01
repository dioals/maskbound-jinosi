using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using UnityEngine;

namespace MaskboundJinosi.AI
{
    [AddComponentMenu("Maskbound/AI/Actions/AI Action Toggle Damage On Touch")]
    public class AIActionToggleDamageOnTouch : AIAction
    {
        public enum ToggleMoment
        {
            OnEnterState,
            EveryFrame
        }

        [Header("Target")]
        [SerializeField] private GameObject targetRoot;
        [SerializeField] private bool includeInactive = true;

        [Header("Toggle")]
        [SerializeField] private bool damageOnTouchEnabled;
        [SerializeField] private ToggleMoment toggleMoment = ToggleMoment.OnEnterState;
        [SerializeField] private bool restoreOnExit;
        [SerializeField] private bool restoredEnabledState = true;

        private DamageOnTouch[] _damageOnTouches;

        public override void Initialization()
        {
            if (!ShouldInitialize)
            {
                return;
            }

            ResolveDamageOnTouches();
        }

        public override void OnEnterState()
        {
            base.OnEnterState();

            if (toggleMoment == ToggleMoment.OnEnterState)
            {
                SetDamageOnTouchEnabled(damageOnTouchEnabled);
            }
        }

        public override void PerformAction()
        {
            if (toggleMoment == ToggleMoment.EveryFrame)
            {
                SetDamageOnTouchEnabled(damageOnTouchEnabled);
            }
        }

        public override void OnExitState()
        {
            base.OnExitState();

            if (restoreOnExit)
            {
                SetDamageOnTouchEnabled(restoredEnabledState);
            }
        }

        public void SetDamageOnTouchEnabled(bool enabledState)
        {
            ResolveDamageOnTouches();

            for (int i = 0; i < _damageOnTouches.Length; i++)
            {
                if (_damageOnTouches[i] != null)
                {
                    _damageOnTouches[i].enabled = enabledState;
                }
            }
        }

        private void ResolveDamageOnTouches()
        {
            if (_damageOnTouches != null && _damageOnTouches.Length > 0)
            {
                return;
            }

            GameObject root = targetRoot != null ? targetRoot : gameObject;
            _damageOnTouches = root.GetComponentsInChildren<DamageOnTouch>(includeInactive);
        }
    }
}
