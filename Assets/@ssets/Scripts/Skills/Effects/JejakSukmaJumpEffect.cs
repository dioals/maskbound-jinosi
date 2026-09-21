using System.Collections;
using MoreMountains.CorgiEngine;
using UnityEngine;

namespace MaskboundJinosi.Skills.Effects
{
	[AddComponentMenu("Maskbound/Skills/Effects/Jejak Sukma Jump Effect")]
	public class JejakSukmaJumpEffect : MonoBehaviour, ISkillRuntimeReceiver
	{
		[Tooltip("Bonus NumberOfJumps yang diberikan ke player selama skill aktif. Default 2 agar player mendapat double jump (1 baseline + 1 bonus).")]
		[Min(1)] public int BonusNumberOfJumps = 2;

		[Tooltip("Trigger lompatan otomatis ke atas saat skill diaktifkan. Cocok untuk skill lompat seperti Jejak Sukma.")]
		public bool TriggerJumpOnInitialize = true;

		[Tooltip("Kecepatan vertikal awal lompatan yang di-trigger otomatis. 0 = pakai JumpHeight default CharacterJump (lewat JumpStart).")]
		[Min(0f)] public float JumpLaunchVelocity;

		[Tooltip("Fallback durasi bonus (detik) jika ActiveSkillData.Duration <= 0. Dipakai sebagai jendela kemampuan double jump.")]
		[Min(0f)] public float FallbackBonusDuration = 1.2f;

		[Tooltip("Batas waktu tunggu (detik) sampai cast skill selesai (IsCasting false) sebelum lompatan otomatis dipaksa jalan. Mencegah skill tidak pernah melompat kalau animasi cast tidak mengirim event selesai.")]
		[Min(0f)] public float MaxCastWaitDuration = 1.5f;

		[Tooltip("Habiskan sisa jump setelah lompatan skill, supaya satu cast = satu lompatan (tidak ada jump gratis sisa selama Duration). Jump skill baru pulih saat menyentuh tanah.")]
		public bool ConsumeJumpsAfterLaunch = true;

		protected SkillRuntimeContext _context;
		protected CharacterJump _jump;
		protected CorgiController _controller;
		protected CharacterSkillCaster _caster;
		protected int _savedNumberOfJumps;
		protected bool _hasSaved;
		protected bool _jumpsConsumed;
		protected Coroutine _releaseCo;
		protected Coroutine _jumpCo;

		public virtual void Initialize(SkillRuntimeContext context)
		{
			_context = context;
			Character character = context.Character;
			if (character == null)
			{
				return;
			}

			_jump = character.FindAbility<CharacterJump>();
			_controller = character.GetComponentInParent<CorgiController>();
			_caster = character.GetComponentInChildren<CharacterSkillCaster>(true);
			if (_jump == null)
			{
				return;
			}

			if (!_hasSaved)
			{
				_savedNumberOfJumps = _jump.NumberOfJumps;
				_hasSaved = true;
			}

			_jump.NumberOfJumps = BonusNumberOfJumps;
			_jump.ResetNumberOfJumps();

			if (TriggerJumpOnInitialize)
			{
				if (_jumpCo != null)
				{
					StopCoroutine(_jumpCo);
				}
				_jumpCo = StartCoroutine(TriggerJumpImmediately());
			}

			if (_releaseCo != null)
			{
				StopCoroutine(_releaseCo);
			}

			float duration = context.Duration > 0f ? context.Duration : FallbackBonusDuration;
			if (duration > 0f)
			{
				_releaseCo = StartCoroutine(ReleaseAfterCo(duration));
			}
		}

		/// <summary>
		/// Lompat langsung tanpa menunggu animasi cast selesai. Menghentikan cast
		/// (clear IsCasting/IsCastingSkill) dulu karena CharacterJump menolak jump
		/// selama IsCastingSkill true. Ada timeout pengaman kalau cast macet.
		/// </summary>
		protected virtual IEnumerator TriggerJumpImmediately()
		{
			float waitStartedAt = Time.time;
			// cast selesai sesegera mungkin supaya JumpStart tidak ditolak
			if (_caster != null && _caster.IsCasting)
			{
				_caster.StopCastingAnimation();
			}

			// tunggu sampai IsCasting benar-benar clear (setter sinkron, biasanya frame ini juga)
			while (_caster != null && _caster.IsCasting)
			{
				if (Time.time - waitStartedAt >= MaxCastWaitDuration)
				{
					break;
				}
				yield return null;
			}

			if (TriggerJumpOnInitialize)
			{
				ApplyLaunchVelocity();

				if (ConsumeJumpsAfterLaunch)
				{
					ConsumeRemainingJumps();
				}
			}
			_jumpCo = null;
		}

		/// <summary>
		/// Satu cast = satu lompatan. Kembalikan NumberOfJumps ke nilai asli lalu kosongkan sisa jump,
		/// jadi tidak ada lompatan gratis tersisa selama Duration. CharacterJump.ProcessAbility sendiri
		/// yang memulihkan jump begitu karakter menyentuh tanah.
		/// </summary>
		protected virtual void ConsumeRemainingJumps()
		{
			if (_jump == null)
			{
				return;
			}

			// Jendela bonus berdurasi tidak dipakai lagi di mode ini - hentikan supaya ReleaseBonus
			// nanti tidak memanggil ResetNumberOfJumps dan mengembalikan lompatan gratis.
			if (_releaseCo != null)
			{
				StopCoroutine(_releaseCo);
				_releaseCo = null;
			}

			if (_hasSaved)
			{
				_jump.NumberOfJumps = _savedNumberOfJumps;
			}

			_jump.NumberOfJumpsLeft = 0;
			_jumpsConsumed = true;
		}

		protected virtual void ApplyLaunchVelocity()
		{
			if (_controller == null || _jump == null)
			{
				return;
			}

			// Kalau ada override kecepatan eksplisit, pakai langsung.
			if (JumpLaunchVelocity > 0f)
			{
				_controller.SetVerticalForce(JumpLaunchVelocity);
				return;
			}

			// Lewat JumpStart supaya state Jumping, gravity, feedback, dan jump count
			// konsisten dengan sistem jump CorgiEngine, dan kecepatan dihitung dari
			// JumpHeight * Gravity dengan rumus yang benar (bukan JumpHeight mentah).
			_jump.JumpStart();
		}

		protected virtual IEnumerator ReleaseAfterCo(float duration)
		{
			yield return new WaitForSeconds(duration);
			ReleaseBonus();
		}

		public virtual void ReleaseBonus()
		{
			// Kalau sisa jump sudah sengaja dihabiskan (satu cast = satu lompatan), jangan panggil
			// ResetNumberOfJumps: summon ini mati beberapa detik kemudian (TimedSkillObject) dan itu
			// akan memberi lompatan udara gratis di tengah-tengah.
			if (_jump != null && _hasSaved && !_jumpsConsumed)
			{
				_jump.NumberOfJumps = _savedNumberOfJumps;
				_jump.ResetNumberOfJumps();
			}

			if (_jumpCo != null)
			{
				StopCoroutine(_jumpCo);
				_jumpCo = null;
			}

			_releaseCo = null;
		}

		protected virtual void OnDestroy()
		{
			ReleaseBonus();
		}
	}
}
