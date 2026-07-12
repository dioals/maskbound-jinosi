using MoreMountains.CorgiEngine;
using UnityEngine;

namespace MaskboundJinosi.Combat
{
    [DisallowMultipleComponent]
    public class MaskboundMeleeWeaponStateDebug : MonoBehaviour
    {
        [SerializeField] private bool debugLogs = true;

        private Weapon _weapon;
        private Weapon.WeaponStates _lastState;
        private Collider2D _damageAreaCollider;

        private void Awake()
        {
            _weapon = GetComponent<Weapon>();
        }

        private void Update()
        {
            if (!debugLogs || _weapon == null || _weapon.WeaponState == null)
            {
                return;
            }

            if (_weapon.WeaponState.CurrentState != _lastState)
            {
                _lastState = _weapon.WeaponState.CurrentState;
                Debug.Log($"Weapon state: {_lastState}", this);
            }

            if (_damageAreaCollider == null)
            {
                _damageAreaCollider = FindDamageAreaCollider();
            }

            if (_damageAreaCollider != null && _damageAreaCollider.enabled)
            {
                Debug.Log($"DamageArea collider ACTIVE: {_damageAreaCollider.name}", _damageAreaCollider);
            }
        }

        private Collider2D FindDamageAreaCollider()
        {
            Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].name.Contains("DamageArea"))
                {
                    return colliders[i];
                }
            }

            return colliders.Length > 0 ? colliders[0] : null;
        }
    }
}
