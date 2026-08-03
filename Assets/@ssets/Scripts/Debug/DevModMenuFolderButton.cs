using UnityEngine;
using UnityEngine.UI;

namespace MaskboundJinosi.Debugging
{
	[RequireComponent(typeof(Button))]
	[AddComponentMenu("Maskbound/Debug/Mod Menu Folder Button")]
	public class DevModMenuFolderButton : MonoBehaviour
	{
		public DevModMenuController Menu;
		public DevModMenuPage TargetPage;

		private Button _button;

		protected virtual void Awake()
		{
			_button = GetComponent<Button>();
			if (Menu == null)
			{
				Menu = GetComponentInParent<DevModMenuController>(true);
			}

			_button.onClick.AddListener(Open);
		}

		public virtual void Open()
		{
			if (Menu != null)
			{
				Menu.OpenPage(TargetPage, _button);
			}
		}

		protected virtual void OnDestroy()
		{
			if (_button != null)
			{
				_button.onClick.RemoveListener(Open);
			}
		}
	}
}
