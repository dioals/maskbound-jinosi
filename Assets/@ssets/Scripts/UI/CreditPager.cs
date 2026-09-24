using System.Collections.Generic;
using InControl;
using MoreMountains.CorgiEngine;
using UnityEngine;
using UnityEngine.Events;

namespace MaskboundJinosi.UI
{
	[AddComponentMenu("Maskbound/UI/Credit Pager")]
	public class CreditPager : MonoBehaviour
	{
		[Header("Pages (manual)")]
		[Tooltip("Urutan halaman credit. Isi manual di Inspector: Credit 1.1, Credit 1.2, Credit 2. Frame jangan dimasukkan, dia selalu nyala.")]
		public List<GameObject> Pages = new List<GameObject>();

		[Header("Behaviour")]
		[Tooltip("Kalau ON, Next di halaman terakhir balik ke halaman pertama. Kalau OFF, Next di akhir tidak ngapa-ngapain.")]
		public bool LoopPages = true;
		[Tooltip("Kalau ON, Next di halaman terakhir menutup credit dan memanggil OnFinished. Untuk credit game-over/game-selesai biarkan ON supaya next-next sampai habis lalu selesai.")]
		public bool FinishOnLastNext = true;

		[Header("Show / Hide")]
		[Tooltip("Root CreditUI yang di-toggle. Kosongkan = pakai parent dari MainCredit (otomatis).")]
		public GameObject CreditRoot;
		[Tooltip("Sembunyikan HUD gameplay saat credit tampil, kembalikan saat credit ditutup.")]
		public bool HideHudDuringCredits = true;
		[Tooltip("UI lain yang ikut di-hide saat credit tampil (opsional, isi manual). HUD otomatis, tidak perlu dimasukkan.")]
		public List<GameObject> HideOtherUI = new List<GameObject>();
		[Tooltip("Otomatis hide overlay GameOver/BossVictory saat credit tampil.")]
		public bool HideEndOverlays = true;
		[Tooltip("Kembalikan UI yang di-hide saat credit ditutup. Untuk flow game-over biarkan ON supaya HUD balik sebelum load main menu.")]
		public bool RestoreOnClose = true;

		[Header("Events")]
		public UnityEvent OnFinished;

		[Header("Input")]
		public KeyCode NextKey = KeyCode.F;
		public KeyCode PrevKey = KeyCode.Q;
		public bool UseBumpers = true;
		public bool UseDPadLeftRight = true;

		public bool IsShowing => _isShowing;

		private int _index;
		private bool _isShowing;
		private bool _hudHidden;
		private float _inputBlockUntil;
		private readonly List<GameObject> _hiddenOthers = new List<GameObject>();
		private readonly List<GameObject> _hiddenOverlays = new List<GameObject>();

		protected virtual void Reset()
		{
			CollectPages();
		}

		protected virtual void OnEnable()
		{
			if (Pages == null || Pages.Count == 0)
			{
				CollectPages();
			}

			_index = 0;
			ShowOnly(_index);

			if (!_isShowing)
			{
				_isShowing = true;
				HideForCredits();
			}
		}

		protected virtual void OnDisable()
		{
			if (!_isShowing)
			{
				return;
			}

			_isShowing = false;
			RestoreHidden();
		}

		protected virtual void Update()
		{
			if (!_isShowing || !isActiveAndEnabled)
			{
				return;
			}

			if (Time.unscaledTime < _inputBlockUntil)
			{
				return;
			}

			if (WasNextPressed())
			{
				Next();
			}
			else if (WasPrevPressed())
			{
				Prev();
			}
		}

		public virtual void ShowCredits()
		{
			if (_isShowing)
			{
				_index = 0;
				ShowOnly(_index);
				return;
			}

			_isShowing = true;
			HideForCredits();

			GameObject root = ResolveRoot();
			if (root != null && !root.activeSelf)
			{
				root.SetActive(true);
			}

			if (!gameObject.activeSelf)
			{
				gameObject.SetActive(true);
			}

			_index = 0;
			ShowOnly(_index);
			_inputBlockUntil = Time.unscaledTime + 0.3f;
		}

		public virtual void Close()
		{
			if (!_isShowing)
			{
				return;
			}

			_isShowing = false;

			if (RestoreOnClose)
			{
				RestoreHidden();
			}
			else
			{
				_hudHidden = false;
				_hiddenOthers.Clear();
				_hiddenOverlays.Clear();
			}

			GameObject root = ResolveRoot();
			if (root != null && root.activeSelf)
			{
				root.SetActive(false);
			}

			OnFinished?.Invoke();
		}

		public virtual void Next()
		{
			if (Pages == null || Pages.Count == 0)
			{
				return;
			}

			int next = _index + 1;
			if (next >= Pages.Count)
			{
				if (FinishOnLastNext)
				{
					Close();
					return;
				}

				next = LoopPages ? 0 : Pages.Count - 1;
			}

			ShowOnly(next);
		}

		public virtual void Prev()
		{
			if (Pages == null || Pages.Count == 0)
			{
				return;
			}

			int prev = _index - 1;
			if (prev < 0)
			{
				prev = LoopPages ? Pages.Count - 1 : 0;
			}

			ShowOnly(prev);
		}

		public virtual void ShowOnly(int index)
		{
			if (Pages == null || Pages.Count == 0)
			{
				return;
			}

			_index = Mathf.Clamp(index, 0, Pages.Count - 1);
			for (int i = 0; i < Pages.Count; i++)
			{
				GameObject page = Pages[i];
				if (page != null)
				{
					page.SetActive(i == _index);
				}
			}
		}

		public static CreditPager FindInScene()
		{
			return Object.FindFirstObjectByType<CreditPager>(FindObjectsInactive.Include);
		}

		private GameObject ResolveRoot()
		{
			if (CreditRoot != null)
			{
				return CreditRoot;
			}

			if (transform.parent != null)
			{
				return transform.parent.gameObject;
			}

			return gameObject;
		}

		private void HideForCredits()
		{
			HideHud();
			HideOthers();
			HideOverlays();
		}

		private void RestoreHidden()
		{
			RestoreOverlays();
			RestoreOthers();
			RestoreHud();
		}

		private void HideHud()
		{
			if (!HideHudDuringCredits || _hudHidden)
			{
				return;
			}

			GameObject hud = GetHud();
			if (hud != null && hud.activeSelf)
			{
				_hudHidden = true;
				hud.SetActive(false);
			}
		}

		private void RestoreHud()
		{
			if (!_hudHidden)
			{
				return;
			}

			GameObject hud = GetHud();
			if (hud != null)
			{
				hud.SetActive(true);
			}

			_hudHidden = false;
		}

		private void HideOthers()
		{
			if (HideOtherUI == null)
			{
				return;
			}

			foreach (GameObject target in HideOtherUI)
			{
				if (target != null && target.activeSelf && !_hiddenOthers.Contains(target))
				{
					_hiddenOthers.Add(target);
					target.SetActive(false);
				}
			}
		}

		private void RestoreOthers()
		{
			foreach (GameObject target in _hiddenOthers)
			{
				if (target != null)
				{
					target.SetActive(true);
				}
			}

			_hiddenOthers.Clear();
		}

		private void HideOverlays()
		{
			if (!HideEndOverlays)
			{
				return;
			}

			HideOverlayByName("GameOverOverlay");
			HideOverlayByName("BossVictoryOverlay");
		}

		private void HideOverlayByName(string overlayName)
		{
			GameObject overlay = GameObject.Find(overlayName);
			if (overlay != null && overlay.activeSelf && !_hiddenOverlays.Contains(overlay))
			{
				_hiddenOverlays.Add(overlay);
				overlay.SetActive(false);
			}
		}

		private void RestoreOverlays()
		{
			foreach (GameObject overlay in _hiddenOverlays)
			{
				if (overlay != null)
				{
					overlay.SetActive(true);
				}
			}

			_hiddenOverlays.Clear();
		}

		private static GameObject GetHud()
		{
			if (GUIManager.HasInstance && GUIManager.Instance.HUD != null)
			{
				return GUIManager.Instance.HUD;
			}

			GameObject[] objects = FindObjectsByType<GameObject>(
				FindObjectsInactive.Include,
				FindObjectsSortMode.None);

			foreach (GameObject target in objects)
			{
				if (target != null && target.name == "HUD")
				{
					return target;
				}
			}

			return null;
		}

		private bool WasNextPressed()
		{
			if (UnityEngine.Input.GetKeyDown(NextKey)
				|| UnityEngine.Input.GetKeyDown(KeyCode.E)
				|| UnityEngine.Input.GetKeyDown(KeyCode.Space)
				|| UnityEngine.Input.GetKeyDown(KeyCode.Return)
				|| UnityEngine.Input.GetKeyDown(KeyCode.RightArrow))
			{
				return true;
			}

			InputDevice device = InControl.InputManager.ActiveDevice;
			if (device == null)
			{
				return false;
			}

			if (device.GetControl(InputControlType.Action1).WasPressed)
			{
				return true;
			}

			if (UseDPadLeftRight && device.DPadRight.WasPressed)
			{
				return true;
			}

			return UseBumpers && device.RightBumper.WasPressed;
		}

		private bool WasPrevPressed()
		{
			if (UnityEngine.Input.GetKeyDown(PrevKey)
				|| UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow))
			{
				return true;
			}

			InputDevice device = InControl.InputManager.ActiveDevice;
			if (device == null)
			{
				return false;
			}

			if (UseDPadLeftRight && device.DPadLeft.WasPressed)
			{
				return true;
			}

			return UseBumpers && device.LeftBumper.WasPressed;
		}

		private void CollectPages()
		{
			Pages.Clear();
			foreach (Transform child in transform)
			{
				if (child == null)
				{
					continue;
				}

				if (child.name.ToLowerInvariant().Contains("credit"))
				{
					Pages.Add(child.gameObject);
				}
			}

			Pages.Sort((a, b) => string.Compare(a.name, b.name));
		}
	}
}
