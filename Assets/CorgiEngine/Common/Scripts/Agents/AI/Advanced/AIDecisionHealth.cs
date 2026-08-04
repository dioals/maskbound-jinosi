using MoreMountains.Tools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoreMountains.CorgiEngine
{
	/// <summary>
	/// This decision will return true if the specified Health conditions are met. You can have it be lower, strictly lower, equal, higher or strictly higher than the specified value.
	/// </summary>
	[AddComponentMenu("Corgi Engine/Character/AI/Decisions/AI Decision Health")]
	// [RequireComponent(typeof(Health))]
	public class AIDecisionHealth : AIDecision
	{
		/// the different comparison modes
		public enum ComparisonModes { StrictlyLowerThan, LowerThan, Equals, GreatherThan, StrictlyGreaterThan }
		/// whether the threshold is expressed as raw health or as a percentage of maximum health
		public enum HealthValueModes { Absolute, Percentage }

		[Tooltip("Choose whether to compare against a raw health value or a percentage of MaximumHealth.")]
		public HealthValueModes ValueMode = HealthValueModes.Absolute;
		/// the comparison mode with which we'll evaluate the HealthValue
		[Tooltip("the comparison mode with which we'll evaluate the HealthValue")]
		public ComparisonModes TrueIfHealthIs;
		/// the Health value to compare to
		[Tooltip("the Health value to compare to")]
		public int HealthValue;
		/// the percentage of MaximumHealth to compare to
		[Tooltip("Percentage of MaximumHealth to compare to when Value Mode is Percentage.")]
		[Range(0f, 100f)] public float HealthPercentage = 50f;
		/// whether we want this comparison to be done only once or not
		[Tooltip("whether we want this comparison to be done only once or not")]
		public bool OnlyOnce = true;

		protected Health _health;
		protected bool _once = false;

		/// <summary>
		/// On init we grab our Health component
		/// </summary>
		public override void Initialization()
		{
			_health = _brain.gameObject.GetComponentInParent<Health>();
		}

		/// <summary>
		/// On Decide we evaluate our current Health level
		/// </summary>
		/// <returns></returns>
		public override bool Decide()
		{
			return EvaluateHealth();
		}

		/// <summary>
		/// Compares our health value and returns true if the condition is met
		/// </summary>
		/// <returns></returns>
		protected virtual bool EvaluateHealth()
		{
			bool returnValue = false;

			if (OnlyOnce && _once)
			{
				return false;
			}

			if (_health == null)
			{
				Debug.LogWarning("You've added an AIDecisionHealth to " + this.gameObject.name + "'s AI Brain, but this object doesn't have a Health component.");
				return false;
			}

			if (!_health.isActiveAndEnabled)
			{
				return false;
			}
            
			float comparisonValue = ValueMode == HealthValueModes.Percentage
				? _health.MaximumHealth * (HealthPercentage / 100f)
				: HealthValue;

			if (TrueIfHealthIs == ComparisonModes.StrictlyLowerThan)
			{
				returnValue = (_health.CurrentHealth < comparisonValue);
			}

			if (TrueIfHealthIs == ComparisonModes.LowerThan)
			{
				returnValue = (_health.CurrentHealth <= comparisonValue);
			}

			if (TrueIfHealthIs == ComparisonModes.Equals)
			{
				returnValue = Mathf.Approximately(_health.CurrentHealth, comparisonValue);
			}

			if (TrueIfHealthIs == ComparisonModes.GreatherThan)
			{
				returnValue = (_health.CurrentHealth >= comparisonValue);
			}

			if (TrueIfHealthIs == ComparisonModes.StrictlyGreaterThan)
			{
				returnValue = (_health.CurrentHealth > comparisonValue);
			}

			if (returnValue)
			{
				_once = true;
				return true;
			}
			else
			{
				return false;
			}
		}
	}
}
