using MoreMountains.CorgiEngine;
using UnityEngine;

namespace MaskboundJinosi.AI
{
    [AddComponentMenu("Maskbound/AI/Animation Event Teleport Behind Player")]
    public class AnimationEventTeleportBehindPlayer : MonoBehaviour
    {
        [SerializeField] private Character bossCharacter;
        [SerializeField] private Transform targetPlayer;
        [SerializeField, Min(0f)] private float behindDistance = 2f;
        [SerializeField] private float verticalOffset;
        [SerializeField] private bool facePlayerAfterTeleport = true;
        [SerializeField] private bool useCorgiSafePosition = true;
        [SerializeField] private bool stopVelocity = true;
        [SerializeField] private bool logMissingTarget;

        private CorgiController _controller;

        private void Awake()
        {
            bossCharacter ??= GetComponentInParent<Character>();
            _controller = bossCharacter != null ? bossCharacter.GetComponent<CorgiController>() : GetComponentInParent<CorgiController>();
        }

        public void TeleportBehindPlayer()
        {
            ResolveTarget();
            if (bossCharacter == null || targetPlayer == null)
            {
                if (logMissingTarget)
                {
                    Debug.LogWarning($"{name}: boss atau player target belum ditemukan.", this);
                }
                return;
            }

            Character playerCharacter = targetPlayer.GetComponentInParent<Character>();
            Vector3 playerPosition = playerCharacter != null ? playerCharacter.transform.position : targetPlayer.position;
            bool playerFacingRight = playerCharacter == null || playerCharacter.IsFacingRight;
            float behindDirection = playerFacingRight ? -1f : 1f;
            Vector3 destination = playerPosition + new Vector3(behindDirection * behindDistance, verticalOffset, 0f);
            Vector3 originalPosition = bossCharacter.transform.position;

            if (useCorgiSafePosition && _controller != null)
            {
                Vector2 safePosition = _controller.GetClosestSafePosition(destination);
                // Corgi can return the current X when the destination overlaps a collider.
                // Keep the requested behind position in that case so teleporting is not silently cancelled.
                if (Mathf.Abs(safePosition.x - originalPosition.x) > 0.01f)
                {
                    destination.x = safePosition.x;
                }
                if (Mathf.Abs(safePosition.y - originalPosition.y) > 0.01f)
                {
                    destination.y = safePosition.y;
                }
            }

            if (_controller != null)
            {
                _controller.SetTransformPosition(destination);
            }
            bossCharacter.transform.position = destination;

            if (stopVelocity && _controller != null)
            {
                _controller.SetForce(Vector2.zero);
            }

            if (facePlayerAfterTeleport)
            {
                bool shouldFaceRight = playerPosition.x > bossCharacter.transform.position.x;
                if (bossCharacter.IsFacingRight != shouldFaceRight)
                {
                    bossCharacter.Flip(true);
                }
            }
        }

        public void ClearTarget()
        {
            targetPlayer = null;
        }

        private void ResolveTarget()
        {
            if (targetPlayer != null) { return; }

            if (LevelManager.Instance != null && LevelManager.Instance.Players != null && LevelManager.Instance.Players.Count > 0)
            {
                targetPlayer = LevelManager.Instance.Players[0].transform;
            }
        }
    }
}
