using System.Collections;
using MoreMountains.CorgiEngine;
using UnityEngine;

namespace MaskboundJinosi.Combat
{
    [AddComponentMenu("Maskbound/Combat/Animation Event Lunge 2D")]
    public class AnimationEventLunge2D : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Character character;
        [SerializeField] private CorgiController controller;

        [Header("Lunge")]
        [SerializeField, Min(0f)] private float horizontalDistance = 1.5f;
        [SerializeField, Min(0.01f)] private float duration = 0.2f;
        [SerializeField] private bool useCharacterFacing = true;
        [SerializeField] private bool respectCorgiCollisions = true;
        [SerializeField] private bool stopAfterLunge = true;

        private Coroutine lungeCoroutine;

        private void Awake()
        {
            character ??= GetComponentInParent<Character>();
            controller ??= GetComponentInParent<CorgiController>();
        }

        public void Lunge()
        {
            StartDistanceLunge(horizontalDistance, duration);
        }

        // Kept for existing animation events that still pass a raw Corgi force value.
        public void LungeWithForce(float horizontalForce)
        {
            StartForceLunge(new Vector2(horizontalForce, 0f), duration);
        }

        public void LungeDistance(float distance)
        {
            StartDistanceLunge(Mathf.Abs(distance), duration);
        }

        public void StopLunge()
        {
            if (lungeCoroutine != null)
            {
                StopCoroutine(lungeCoroutine);
                lungeCoroutine = null;
            }

            if (controller != null)
            {
                controller.SetForce(Vector2.zero);
            }
        }

        private void StartDistanceLunge(float distance, float lungeDuration)
        {
            if (controller == null)
            {
                return;
            }

            if (lungeCoroutine != null)
            {
                StopCoroutine(lungeCoroutine);
            }

            lungeCoroutine = StartCoroutine(
                DistanceLungeCoroutine(Mathf.Abs(distance), Mathf.Max(0.01f, lungeDuration)));
        }

        private void StartForceLunge(Vector2 lungeForce, float lungeDuration)
        {
            if (controller == null)
            {
                return;
            }

            if (lungeCoroutine != null)
            {
                StopCoroutine(lungeCoroutine);
            }

            lungeCoroutine = StartCoroutine(
                ForceLungeCoroutine(lungeForce, Mathf.Max(0.01f, lungeDuration)));
        }

        private IEnumerator DistanceLungeCoroutine(float distance, float lungeDuration)
        {
            float direction = GetFacingMultiplier();
            float startX = controller.transform.position.x;
            float destinationX = startX + distance * direction;
            float elapsed = 0f;

            controller.SetHorizontalForce(0f);

            while (elapsed < lungeDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / lungeDuration);

                Vector3 currentPosition = controller.transform.position;
                Vector3 desiredPosition = new Vector3(
                    Mathf.Lerp(startX, destinationX, progress),
                    currentPosition.y,
                    currentPosition.z);

                if (respectCorgiCollisions)
                {
                    Vector2 safePosition = controller.GetClosestSafePosition(desiredPosition);
                    desiredPosition.x = safePosition.x;
                    desiredPosition.y = safePosition.y;
                }

                controller.SetTransformPosition(desiredPosition);
                yield return null;
            }

            if (stopAfterLunge)
            {
                controller.SetHorizontalForce(0f);
            }

            lungeCoroutine = null;
        }

        private IEnumerator ForceLungeCoroutine(Vector2 lungeForce, float lungeDuration)
        {
            float facingMultiplier = GetFacingMultiplier();
            controller.SetForce(new Vector2(lungeForce.x * facingMultiplier, lungeForce.y));
            yield return new WaitForSeconds(lungeDuration);

            if (stopAfterLunge)
            {
                controller.SetForce(Vector2.zero);
            }

            lungeCoroutine = null;
        }

        private float GetFacingMultiplier()
        {
            if (useCharacterFacing && character != null)
            {
                return character.IsFacingRight ? 1f : -1f;
            }

            return 1f;
        }
    }
}
