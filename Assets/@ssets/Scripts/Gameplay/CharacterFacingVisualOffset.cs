using MoreMountains.CorgiEngine;
using UnityEngine;

namespace MaskboundJinosi.Gameplay
{
    /// <summary>
    /// Applies a visual-only offset based on character facing. Physics, colliders,
    /// and the character root remain stationary.
    /// </summary>
    [AddComponentMenu("Maskbound/Gameplay/Character Facing Visual Offset")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Character))]
    public class CharacterFacingVisualOffset : MonoBehaviour
    {
        [SerializeField] private Character character;
        [SerializeField] private Transform visualTarget;
        [SerializeField] private Vector3 rightFacingOffset = Vector3.zero;
        [SerializeField] private Vector3 leftFacingOffset = new Vector3(0.75f, 0f, 0f);

        private Vector3 _baseLocalPosition;
        private bool _initialized;

        private void Awake()
        {
            ResolveReferences();
            CacheBasePosition();
        }

        private void LateUpdate()
        {
            if (!_initialized)
            {
                ResolveReferences();
                CacheBasePosition();
            }

            if (!_initialized || character == null)
            {
                return;
            }

            visualTarget.localPosition = _baseLocalPosition
                + (character.IsFacingRight ? rightFacingOffset : leftFacingOffset);
        }

        private void OnDisable()
        {
            if (_initialized && visualTarget != null)
            {
                visualTarget.localPosition = _baseLocalPosition;
            }
        }

        private void ResolveReferences()
        {
            if (character == null)
            {
                character = GetComponent<Character>();
            }

            if (visualTarget == null && character != null && character.CharacterModel != null)
            {
                visualTarget = character.CharacterModel.transform;
            }
        }

        private void CacheBasePosition()
        {
            if (visualTarget == null)
            {
                return;
            }

            _baseLocalPosition = visualTarget.localPosition;
            _initialized = true;
        }

        private void Reset()
        {
            ResolveReferences();
        }
    }
}
