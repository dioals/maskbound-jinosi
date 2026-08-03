using UnityEngine;
using UnityEngine.UI;

namespace MaskboundJinosi.UI
{
	[RequireComponent(typeof(Button))]
	[AddComponentMenu("Maskbound/UI/Main Menu Folder Button")]
	public class MainMenuFolderButton : MonoBehaviour
	{
		public MainMenuController Menu;
		public MainMenuPage TargetPage;
		private Button _button;

		protected virtual void Awake()
		{
			_button = GetComponent<Button>();
			if (Menu == null) Menu = GetComponentInParent<MainMenuController>(true);
			_button.onClick.AddListener(Open);
		}

		public virtual void Open()
		{
			if (Menu != null) Menu.OpenPage(TargetPage, _button);
		}

		protected virtual void OnDestroy()
		{
			if (_button != null) _button.onClick.RemoveListener(Open);
		}
	}
}
