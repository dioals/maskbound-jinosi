using UnityEngine;
using UnityEngine.UI;

namespace MaskboundJinosi.Debugging
{
	[AddComponentMenu("Maskbound/Debug/Mod Menu Page")]
	public class DevModMenuPage : MonoBehaviour
	{
		[Header("Page")]
		public string Title = "Menu";
		public Selectable FirstSelected;

		public virtual Selectable ResolveFirstSelected()
		{
			if (FirstSelected != null && FirstSelected.gameObject.activeInHierarchy && FirstSelected.IsInteractable())
			{
				return FirstSelected;
			}

			Selectable[] selectables = GetComponentsInChildren<Selectable>(false);
			foreach (Selectable selectable in selectables)
			{
				if (selectable != null && selectable.IsInteractable())
				{
					return selectable;
				}
			}

			return null;
		}
	}
}
