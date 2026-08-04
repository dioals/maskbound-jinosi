using MoreMountains.CorgiEngine;
using UnityEngine;

namespace MaskboundJinosi.Combat
{
    /// <summary>
    /// Keeps a weapon root transform aligned with its owner's facing direction.
    /// Generated melee damage areas are children of this transform, so they mirror
    /// together with the weapon instead of only flipping the sprite renderer.
    /// </summary>
    [AddComponentMenu("Maskbound/Combat/Weapon Facing Transform Sync")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Weapon))]
    public class WeaponFacingTransformSync : MonoBehaviour
    {
        [SerializeField] private bool rightFacingUsesPositiveScale = true;

        private Weapon[] _weapons;
        private float _scaleMagnitudeX;

        private void Awake()
        {
            _weapons = GetComponents<Weapon>();
            _scaleMagnitudeX = Mathf.Max(0.0001f, Mathf.Abs(transform.localScale.x));
        }

        private void LateUpdate()
        {
            Character owner = ResolveOwner();
            if (owner == null)
            {
                return;
            }

            bool positiveScale = rightFacingUsesPositiveScale
                ? owner.IsFacingRight
                : !owner.IsFacingRight;

            Vector3 scale = transform.localScale;
            scale.x = _scaleMagnitudeX * (positiveScale ? 1f : -1f);
            transform.localScale = scale;

            bool flipped = !owner.IsFacingRight;
            for (int i = 0; i < _weapons.Length; i++)
            {
                if (_weapons[i] != null)
                {
                    _weapons[i].Flipped = flipped;
                }
            }
        }

        private Character ResolveOwner()
        {
            if (_weapons == null)
            {
                return null;
            }

            for (int i = 0; i < _weapons.Length; i++)
            {
                if (_weapons[i] != null && _weapons[i].Owner != null)
                {
                    return _weapons[i].Owner;
                }
            }

            return null;
        }
    }
}
