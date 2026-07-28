using UnityEngine;

namespace Maskbound.FX
{
	[AddComponentMenu("Maskbound/FX/Animator Int On Awake")]
	public class AnimatorIntOnAwake : MonoBehaviour
	{
		public Animator Animator;
		public string ParameterName = "IndexVFX";
		public int Value = 1;
		public bool ApplyOnAwake = true;
		public bool ApplyOnEnable;
		public bool LogDebug;

		protected virtual void Awake()
		{
			if (Animator == null)
			{
				Animator = GetComponentInChildren<Animator>(true);
			}

			if (ApplyOnAwake)
			{
				Apply();
			}
		}

		protected virtual void OnEnable()
		{
			if (ApplyOnEnable)
			{
				Apply();
			}
		}

		public virtual void Apply()
		{
			if (Animator == null || string.IsNullOrEmpty(ParameterName))
			{
				if (LogDebug)
				{
					Debug.LogWarning("AnimatorIntOnAwake could not apply because Animator or ParameterName is missing.", this);
				}

				return;
			}

			Animator.SetInteger(ParameterName, Value);
		}
	}
}
