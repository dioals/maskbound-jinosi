using UnityEngine;

namespace MaskboundJinosi.Gameplay.Dialogue
{
	/// <summary>
	/// Menyalakan/mematikan sprite tutorial per id, dipanggil dari Fungus
	/// "Call Method" (pakai SendMessage, jadi method tanpa parameter).
	/// Isi daftar views di Inspector: tiap entry = 1 GameObject sprite tutorial.
	/// ShowTutorial1() = tampilkan views[0], sembunyikan sisanya, dst.
	/// </summary>
	[AddComponentMenu("Maskbound/Dialogue/Tutorial Views")]
	public class TutorialViews : MonoBehaviour
	{
		[Tooltip("Daftar GameObject sprite tutorial. Index 0 = Tutorial 1, dst.")]
		[SerializeField] private GameObject[] views;

		public virtual void ShowTutorial1() { Show(0); }
		public virtual void ShowTutorial2() { Show(1); }
		public virtual void ShowTutorial3() { Show(2); }
		public virtual void ShowTutorial4() { Show(3); }
		public virtual void ShowTutorial5() { Show(4); }

		public virtual void HideTutorial1() { Hide(0); }
		public virtual void HideTutorial2() { Hide(1); }
		public virtual void HideTutorial3() { Hide(2); }
		public virtual void HideTutorial4() { Hide(3); }
		public virtual void HideTutorial5() { Hide(4); }

		public virtual void HideAll()
		{
			SetAll(false);
		}

		protected virtual void Show(int index)
		{
			if (!IsValid(index))
			{
				return;
			}

			for (int i = 0; i < views.Length; i++)
			{
				if (views[i] != null)
				{
					views[i].SetActive(i == index);
				}
			}
		}

		protected virtual void Hide(int index)
		{
			if (!IsValid(index))
			{
				return;
			}

			views[index].SetActive(false);
		}

		protected virtual void SetAll(bool active)
		{
			if (views == null)
			{
				return;
			}

			for (int i = 0; i < views.Length; i++)
			{
				if (views[i] != null)
				{
					views[i].SetActive(active);
				}
			}
		}

		protected virtual bool IsValid(int index)
		{
			if (views == null || index < 0 || index >= views.Length || views[index] == null)
			{
				Debug.LogWarning("[TutorialViews] View index " + index + " kosong/belum diisi di Inspector.", this);
				return false;
			}

			return true;
		}
	}
}
