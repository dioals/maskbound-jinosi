using UnityEngine;
using UnityEngine.UI;

namespace MaskboundJinosi.UI
{
	[AddComponentMenu("Maskbound/UI/Main Menu Page")]
	public class MainMenuPage : MonoBehaviour
	{
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
