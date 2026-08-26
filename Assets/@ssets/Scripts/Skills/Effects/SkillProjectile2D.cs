using MaskboundJinosi.Skills;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace MaskboundJinosi.Skills.Effects
{
	[AddComponentMenu("Maskbound/Skills/Effects/Skill Projectile 2D")]
	[RequireComponent(typeof(Rigidbody2D))]
	public class SkillProjectile2D : MonoBehaviour, ISkillRuntimeReceiver
	{
		[Min(0f)] public float Speed = 10f;
		public Vector2 Direction = Vector2.right;
		public bool UseFacingDirection = true;
		public bool RotateToDirection;

		[Header("Spawn Feedback")]
		[Tooltip("Audio yang diputar begitu projectile dibuat (dipendek untuk 'spawn feedback').")]
		public AudioClip SpawnSound;
		[Tooltip("AudioSource untuk memutar SpawnSound. Kosong = otomatis dibuat saat runtime.")]
		public AudioSource SfxSource;
		[Tooltip("Putar feedback (sound + MMFeedbacks) begitu projectile dibuat.")]
		public bool PlayFeedbackOnSpawn = true;

		protected Rigidbody2D _rigidbody2D;
		protected bool _feedbackPlayed;

		protected virtual void Awake()
		{
			_rigidbody2D = GetComponent<Rigidbody2D>();
			_rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
			_rigidbody2D.simulated = true;

			if (PlayFeedbackOnSpawn)
			{
				PlaySpawnFeedback();
			}
		}

		/// <summary>
		/// Memainkan feedback sesuai pola Corgi/MoreMountains: suara spawn via AudioSource
		/// (atau AudioSource.PlayClipAtPoint) plus MMFeedbacks bila terpasang di prefab,
		/// sehingga bisa menambah partikel/efek lain tanpa ganti kode.
		/// </summary>
		public virtual void PlaySpawnFeedback()
		{
			if (_feedbackPlayed)
			{
				return;
			}

			_feedbackPlayed = true;

			PlaySpawnSound();
			PlayFeedbacks();
		}

		protected virtual void PlaySpawnSound()
		{
			if (SpawnSound == null)
			{
				return;
			}

			if (SfxSource != null)
			{
				SfxSource.PlayOneShot(SpawnSound);
				return;
			}

			AudioSource.PlayClipAtPoint(SpawnSound, transform.position);
		}

		/// <summary>
		/// Memainkan MMFeedbacks di prefab (partikel, scale, dst.) bila terpasang.
		/// Ini meniru leverage MMFeedbacks yang dipakai CorgiEngine untuk feedback spawn.
		/// </summary>
		protected virtual void PlayFeedbacks()
		{
			MMFeedbacks feedbacks = GetComponentInChildren<MMFeedbacks>(true);
			if (feedbacks != null)
			{
				feedbacks.PlayFeedbacks();
			}
		}

		public virtual void Initialize(SkillRuntimeContext context)
		{
			Vector2 direction = Direction.sqrMagnitude > 0f ? Direction.normalized : Vector2.right;
			if (UseFacingDirection)
			{
				direction.x = Mathf.Abs(direction.x) * (context.FacingRight ? 1f : -1f);
			}

			_rigidbody2D.linearVelocity = direction * Speed;

			if (RotateToDirection && direction.sqrMagnitude > 0f)
			{
				float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
				transform.rotation = Quaternion.Euler(0f, 0f, angle);
			}
		}
	}
}