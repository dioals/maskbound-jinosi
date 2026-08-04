using MoreMountains.CorgiEngine;
using UnityEngine;

namespace MaskboundJinosi.AI
{
    [AddComponentMenu("Maskbound/AI/Boss Skill Point Group Facing Mirror")]
    public class BossSkillPointGroupFacingMirror : MonoBehaviour
    {
        [Tooltip("Prabu Klana secara default menghadap kiri. Saat menghadap kanan, local scale X grup dibuat negatif.")]
        [SerializeField] private bool defaultFacingLeft = true;

        private Character _character;
        private float _absoluteScaleX;

        private void Awake()
        {
            _character = GetComponentInParent<Character>();
            _absoluteScaleX = Mathf.Abs(transform.localScale.x);
            ApplyFacingScale();
        }

        private void LateUpdate()
        {
            ApplyFacingScale();
        }

        private void ApplyFacingScale()
        {
            if (_character == null)
            {
                _character = GetComponentInParent<Character>();
                if (_character == null)
                {
                    return;
                }
            }

            bool useNegativeScale = defaultFacingLeft
                ? _character.IsFacingRight
                : !_character.IsFacingRight;

            Vector3 scale = transform.localScale;
            scale.x = useNegativeScale ? -_absoluteScaleX : _absoluteScaleX;
            transform.localScale = scale;
        }
    }
}
