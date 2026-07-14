using UnityEngine;

namespace MaskboundJinosi.Debugging
{
	[AddComponentMenu("Maskbound/Debug/Developer Test Panel Controller")]
	public class DevTestPanelController : MonoBehaviour
	{
		[Header("References")]
		public GameObject PanelRoot;

		[Header("Shortcut")]
		public bool ListenForShortcut = true;
		public KeyCode ToggleKey = KeyCode.F1;
		public bool StartVisible;

		protected virtual void Start()
		{
			SetVisible(StartVisible);
		}

		protected virtual void Update()
		{
			if (ListenForShortcut && UnityEngine.Input.GetKeyDown(ToggleKey))
			{
				TogglePanel();
			}
		}

		public virtual void TogglePanel()
		{
			if (PanelRoot != null) SetVisible(!PanelRoot.activeSelf);
		}

		public virtual void ShowPanel()
		{
			SetVisible(true);
		}

		public virtual void HidePanel()
		{
			SetVisible(false);
		}

		public virtual void SetVisible(bool visible)
		{
			if (PanelRoot != null) PanelRoot.SetActive(visible);
		}
	}
}
