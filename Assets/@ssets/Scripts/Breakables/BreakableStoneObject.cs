using System.Collections;
using UnityEngine;

namespace MaskboundJinosi.Breakables
{
	[AddComponentMenu("Maskbound/World/Breakable Stone Object")]
	public class BreakableStoneObject : BreakableObject
	{
		[Header("Destroy Animation")]
		[Tooltip("Animator that holds the destroy state. Falls back to this object / children.")]
		public Animator Animator;
		[Tooltip("Bool parameter on the Animator that triggers the destroy animation.")]
		public string DestroyParameter = "Hancur";
		[Tooltip("Disables the stone once the destroy animation has finished playing. Turn this OFF to keep the broken stone visible in the scene (the Animator freezes on the last frame of the destroy animation).")]
		public bool DisableAfterDestroyAnimation = true;

		protected override void Awake()
		{
			base.Awake();

			if (Animator == null)
			{
				Animator = GetComponentInChildren<Animator>();
			}
		}

		public override void Break()
		{
			if (_broken)
			{
				return;
			}

			_broken = true;
			SaveBrokenState();
			SpawnBreakEffect();

			if (_collider2D != null)
			{
				_collider2D.enabled = false;
			}

			if (Animator != null && !string.IsNullOrEmpty(DestroyParameter) && HasAnimatorParameter(DestroyParameter))
			{
				Animator.SetBool(DestroyParameter, true);
				StartCoroutine(HandleDestroyAnimationEndRoutine());
			}
			else if (_renderer != null)
			{
				_renderer.enabled = false;
			}
		}

		/// <summary>
		/// After the destroy animation finishes: either disable the stone, or (when
		/// DisableAfterDestroyAnimation is false) keep it in the scene so the broken
		/// remains stay visible, frozen on the last frame of the destroy animation.
		/// </summary>
		protected virtual IEnumerator HandleDestroyAnimationEndRoutine()
		{
			// Wait one frame so the Animator can transition into the destroy state.
			yield return null;

			float duration = 0f;
			if (Animator != null)
			{
				AnimatorStateInfo state = Animator.GetCurrentAnimatorStateInfo(0);
				duration = state.length;
			}

			if (duration <= 0f)
			{
				duration = 0.25f;
			}

			yield return new WaitForSeconds(duration);

			if (DisableAfterDestroyAnimation)
			{
				gameObject.SetActive(false);
			}
			else if (Animator != null)
			{
				// Keep the broken stone visible: seek to the end of the destroy state
				// and stop the Animator so the animation doesn't loop or restart.
				Animator.Play(Animator.GetCurrentAnimatorStateInfo(0).shortNameHash, 0, 1f);
				Animator.speed = 0f;
			}
		}

		protected virtual bool HasAnimatorParameter(string name)
		{
			if (Animator == null || string.IsNullOrEmpty(name))
			{
				return false;
			}

			for (int i = 0; i < Animator.parameterCount; i++)
			{
				if (Animator.GetParameter(i).name == name)
				{
					return true;
				}
			}

			return false;
		}
	}
}
