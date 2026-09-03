using MaskboundJinosi.Breakables;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MaskboundJinosi.EditorTools
{
	/// <summary>
	/// BreakableObject only persists its broken state when SaveId is set (see
	/// BreakableObject.WasBrokenInSave). Placed prefab instances (Stone, Kendi, ...)
	/// don't get one automatically, so this fills every empty SaveId in the open
	/// scene(s) with a stable id derived from scene name + object name + position.
	/// </summary>
	public static class BreakableSaveIdAssigner
	{
		[MenuItem("Maskbound/Breakables/Assign Missing Save IDs In Open Scenes")]
		public static void AssignMissingSaveIds()
		{
			int assigned = 0;

			for (int i = 0; i < SceneManager.sceneCount; i++)
			{
				Scene scene = SceneManager.GetSceneAt(i);
				if (!scene.isLoaded)
				{
					continue;
				}

				string sceneSlug = Slugify(scene.name);
				bool sceneChanged = false;

				foreach (GameObject root in scene.GetRootGameObjects())
				{
					foreach (BreakableObject breakable in root.GetComponentsInChildren<BreakableObject>(true))
					{
						if (!string.IsNullOrEmpty(breakable.SaveId))
						{
							continue;
						}

						Vector3 pos = breakable.transform.position;
						string objectName = Slugify(breakable.gameObject.name.Replace("(Clone)", ""));
						string id = $"{sceneSlug}_{objectName}_{Mathf.RoundToInt(pos.x)}_{Mathf.RoundToInt(pos.y)}";

						Undo.RecordObject(breakable, "Assign Breakable Save ID");
						breakable.SaveId = id;
						EditorUtility.SetDirty(breakable);
						assigned++;
						sceneChanged = true;
					}
				}

				if (sceneChanged)
				{
					EditorSceneManager.MarkSceneDirty(scene);
				}
			}

			Debug.Log(assigned > 0
				? $"[BreakableSaveIdAssigner] Assigned {assigned} SaveId(s). Save the scene(s) to keep them."
				: "[BreakableSaveIdAssigner] No breakables with a missing SaveId found.");
		}

		private static string Slugify(string value)
		{
			return value.Trim().ToLowerInvariant().Replace(" ", "_");
		}
	}
}
