using InControl;
using UnityEngine;

namespace MaskboundJinosi.Input
{
	[DefaultExecutionOrder(-10000)]
	[AddComponentMenu("Maskbound/Input/InControl Duplicate Destroyer")]
	public class InControlDuplicateDestroyer : MonoBehaviour
	{
		private void Awake()
		{
			InControlManager[] managers = FindObjectsByType<InControlManager>(
				FindObjectsInactive.Include,
				FindObjectsSortMode.InstanceID);

			for (int i = 0; i < managers.Length; i++)
			{
				InControlManager manager = managers[i];
				if (manager == null || manager.gameObject == gameObject)
				{
					continue;
				}

				Destroy(gameObject);
				return;
			}
		}
	}
}
