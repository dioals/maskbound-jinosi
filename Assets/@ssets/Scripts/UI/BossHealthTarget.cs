using System;
using MoreMountains.CorgiEngine;
using UnityEngine;

namespace MaskboundJinosi.UI
{
    [AddComponentMenu("Maskbound/UI/Boss Health Target")]
    public class BossHealthTarget : MonoBehaviour
    {
        public static event Action<BossHealthTarget> Registered;
        public static event Action<BossHealthTarget> Unregistered;

        public static BossHealthTarget Current { get; private set; }

        [Header("Boss")]
        [SerializeField] private string displayName = "Boss";
        [SerializeField] private Health health;
        [SerializeField] private bool unregisterOnDeath = true;
        [SerializeField] private bool logRegistration;

        public string DisplayName => displayName;
        public Health Health => health;

        private bool _registered;

        private void Awake()
        {
            ResolveHealth();
        }

        private void OnEnable()
        {
            ResolveHealth();

            if (health != null)
            {
                health.OnDeath += HandleDeath;
            }

            Register();
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.OnDeath -= HandleDeath;
            }

            Unregister();
        }

        private void HandleDeath()
        {
            if (unregisterOnDeath)
            {
                Unregister();
            }
        }

        private void Register()
        {
            if (_registered || health == null)
            {
                return;
            }

            _registered = true;
            Current = this;

            if (logRegistration)
            {
                Debug.Log($"{name}: register boss health '{displayName}'.", this);
            }

            Registered?.Invoke(this);
        }

        private void Unregister()
        {
            if (!_registered)
            {
                return;
            }

            _registered = false;

            if (Current == this)
            {
                Current = null;
            }

            if (logRegistration)
            {
                Debug.Log($"{name}: unregister boss health '{displayName}'.", this);
            }

            Unregistered?.Invoke(this);
        }

        private void ResolveHealth()
        {
            health ??= GetComponentInParent<Health>();
        }
    }
}
