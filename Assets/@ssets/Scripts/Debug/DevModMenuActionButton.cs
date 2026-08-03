using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MaskboundJinosi.Debugging
{
	[RequireComponent(typeof(Button))]
	[AddComponentMenu("Maskbound/Debug/Mod Menu Action Button")]
	public class DevModMenuActionButton : MonoBehaviour
	{
		public UnityEvent Action = new UnityEvent();
		public bool CloseMenuAfterAction;
		public DevModMenuController Menu;

		private Button _button;

		protected virtual void Awake()
		{
			_button = GetComponent<Button>();
			if (Menu == null)
			{
				Menu = GetComponentInParent<DevModMenuController>(true);
			}

			_button.onClick.AddListener(Execute);
		}

		public virtual void Execute()
		{
			Action?.Invoke();
			if (CloseMenuAfterAction && Menu != null)
			{
				Menu.CloseMenu();
			}
		}

		protected virtual void OnDestroy()
		{
			if (_button != null)
			{
				_button.onClick.RemoveListener(Execute);
			}
		}
	}
}
