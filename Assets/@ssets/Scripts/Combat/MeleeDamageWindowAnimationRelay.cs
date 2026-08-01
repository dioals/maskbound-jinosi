using MoreMountains.CorgiEngine;
using UnityEngine;

namespace MaskboundJinosi.Combat
{
    [AddComponentMenu("Maskbound/Combat/Melee Damage Window Animation Relay")]
    public class MeleeDamageWindowAnimationRelay : MonoBehaviour
    {
        [SerializeField] private CharacterHandleWeapon characterHandleWeapon;
        [SerializeField] private string attack1WeaponId = "PrabuKlana_MeleeAttack1";
        [SerializeField] private string attack2WeaponId = "PrabuKlana_MeleeAttack2";
        [SerializeField] private bool logMissingWeapon = true;

        private void Awake()
        {
            ResolveHandleWeapon();
        }

        public void OpenDamageWindow()
        {
            AnimationEventMeleeWeapon weapon = GetCurrentAnimationEventWeapon();
            if (weapon != null)
            {
                weapon.OpenDamageWindow();
            }
        }

        public void OpenAttack1DamageWindow()
        {
            OpenDamageWindowForWeaponId(attack1WeaponId);
        }

        public void OpenAttack2DamageWindow()
        {
            OpenDamageWindowForWeaponId(attack2WeaponId);
        }

        public void CloseDamageWindow()
        {
            AnimationEventMeleeWeapon weapon = GetCurrentAnimationEventWeapon();
            if (weapon != null)
            {
                weapon.CloseDamageWindow();
            }
        }

        public void CloseAttack1DamageWindow()
        {
            CloseDamageWindowForWeaponId(attack1WeaponId);
        }

        public void CloseAttack2DamageWindow()
        {
            CloseDamageWindowForWeaponId(attack2WeaponId);
        }

        public void OpenDamageWindowForWeaponId(string expectedWeaponId)
        {
            AnimationEventMeleeWeapon weapon = GetCurrentAnimationEventWeapon();
            if (weapon == null || !CurrentWeaponMatches(expectedWeaponId))
            {
                return;
            }

            weapon.OpenDamageWindow();
        }

        public void CloseDamageWindowForWeaponId(string expectedWeaponId)
        {
            AnimationEventMeleeWeapon weapon = GetCurrentAnimationEventWeapon();
            if (weapon == null || !CurrentWeaponMatches(expectedWeaponId))
            {
                return;
            }

            weapon.CloseDamageWindow();
        }

        private AnimationEventMeleeWeapon GetCurrentAnimationEventWeapon()
        {
            ResolveHandleWeapon();

            if (characterHandleWeapon?.CurrentWeapon is AnimationEventMeleeWeapon weapon)
            {
                return weapon;
            }

            if (logMissingWeapon)
            {
                string currentWeaponName =
                    characterHandleWeapon?.CurrentWeapon != null
                        ? characterHandleWeapon.CurrentWeapon.name
                        : "None";

                Debug.LogWarning(
                    $"{name}: CurrentWeapon '{currentWeaponName}' bukan AnimationEventMeleeWeapon.",
                    this);
            }

            return null;
        }

        private bool CurrentWeaponMatches(string expectedWeaponId)
        {
            ResolveHandleWeapon();

            Weapon currentWeapon = characterHandleWeapon?.CurrentWeapon;
            if (currentWeapon == null)
            {
                return false;
            }

            bool matches =
                currentWeapon.WeaponID == expectedWeaponId
                || currentWeapon.name.Contains(expectedWeaponId);

            if (!matches && logMissingWeapon)
            {
                Debug.Log(
                    $"{name}: ignore damage window. CurrentWeapon '{currentWeapon.name}' bukan '{expectedWeaponId}'.",
                    this);
            }

            return matches;
        }

        private void ResolveHandleWeapon()
        {
            if (characterHandleWeapon != null)
            {
                return;
            }

            Character character = GetComponentInParent<Character>();
            characterHandleWeapon = character?.FindAbility<CharacterHandleWeapon>();
        }
    }
}
