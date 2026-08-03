#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using InControl;
using MaskboundJinosi.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MaskboundJinosi.Debugging.Editor
{
	public static class DevModMenuBootstrapBuilder
	{
		private const string BootstrapPath = "Assets/@ssets/Scenes/Levels/Bootstrap.unity";
		private static readonly Color PanelColor = new Color(0.035f, 0.045f, 0.055f, 0.96f);
		private static readonly Color HeaderColor = new Color(0.08f, 0.11f, 0.13f, 1f);
		private static readonly Color ButtonColor = new Color(0.12f, 0.16f, 0.18f, 1f);
		private static readonly Color HighlightColor = new Color(0.1f, 0.72f, 0.78f, 1f);

		[MenuItem("Maskbound/Debug/Build Mod Menu In Bootstrap")]
		public static void Build()
		{
			if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
			{
				return;
			}

			Scene scene = EditorSceneManager.OpenScene(BootstrapPath, OpenSceneMode.Single);
			Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
			DevTestHub hub = UnityEngine.Object.FindFirstObjectByType<DevTestHub>(FindObjectsInactive.Include);

			if (canvas == null || hub == null)
			{
				Debug.LogError("[DevModMenuBuilder] Bootstrap requires a Canvas and DevTestHub.");
				return;
			}

			Transform existing = canvas.transform.Find("DevModMenuHost");
			if (existing != null)
			{
				UnityEngine.Object.DestroyImmediate(existing.gameObject);
			}

			GameObject host = CreateUIObject("DevModMenuHost", canvas.transform);
			Stretch(host.GetComponent<RectTransform>());
			DevModMenuController controller = host.AddComponent<DevModMenuController>();

			GameObject menuRoot = CreateUIObject("MenuRoot", host.transform);
			RectTransform menuRect = menuRoot.GetComponent<RectTransform>();
			menuRect.anchorMin = new Vector2(0f, 0.5f);
			menuRect.anchorMax = new Vector2(0f, 0.5f);
			menuRect.pivot = new Vector2(0f, 0.5f);
			menuRect.anchoredPosition = new Vector2(28f, 0f);
			menuRect.sizeDelta = new Vector2(390f, 620f);
			Image panelImage = menuRoot.AddComponent<Image>();
			panelImage.color = PanelColor;

			VerticalLayoutGroup rootLayout = menuRoot.AddComponent<VerticalLayoutGroup>();
			rootLayout.padding = new RectOffset(14, 14, 14, 14);
			rootLayout.spacing = 10f;
			rootLayout.childControlHeight = true;
			rootLayout.childControlWidth = true;
			rootLayout.childForceExpandHeight = false;

			GameObject header = CreateUIObject("Header", menuRoot.transform);
			header.AddComponent<Image>().color = HeaderColor;
			header.AddComponent<LayoutElement>().preferredHeight = 74f;
			VerticalLayoutGroup headerLayout = header.AddComponent<VerticalLayoutGroup>();
			headerLayout.padding = new RectOffset(12, 12, 8, 8);
			headerLayout.spacing = 2f;
			headerLayout.childForceExpandHeight = false;
			TMP_Text title = CreateText("TitleText", header.transform, "DEVELOPER MENU", 22f, FontStyles.Bold);
			TMP_Text breadcrumb = CreateText("BreadcrumbText", header.transform, "Home", 12f, FontStyles.Normal);
			breadcrumb.color = new Color(0.65f, 0.72f, 0.74f, 1f);

			GameObject pagesRoot = CreateUIObject("Pages", menuRoot.transform);
			pagesRoot.AddComponent<LayoutElement>().flexibleHeight = 1f;
			Stretch(pagesRoot.GetComponent<RectTransform>());

			List<DevModMenuPage> pages = new List<DevModMenuPage>();
			DevModMenuPage home = CreatePage("HomePage", "Home", pagesRoot.transform, pages);
			DevModMenuPage player = CreatePage("PlayerPage", "Player", pagesRoot.transform, pages);
			DevModMenuPage boss = CreatePage("BossPage", "Boss", pagesRoot.transform, pages);
			DevModMenuPage skills = CreatePage("SkillsPage", "Skills", pagesRoot.transform, pages);
			DevModMenuPage currency = CreatePage("CurrencyPage", "Currency", pagesRoot.transform, pages);
			DevModMenuPage world = CreatePage("WorldPage", "World", pagesRoot.transform, pages);

			CreateFolderButton(home, "PlayerFolder", "PLAYER", controller, player);
			CreateFolderButton(home, "BossFolder", "BOSS", controller, boss);
			CreateFolderButton(home, "SkillsFolder", "SKILLS", controller, skills);
			CreateFolderButton(home, "CurrencyFolder", "CURRENCY", controller, currency);
			CreateFolderButton(home, "WorldFolder", "WORLD", controller, world);

			CreateAction(player, "DamagePlayer", "Damage Player", hub.DamagePlayer);
			CreateAction(player, "HealPlayer", "Heal Player", hub.HealPlayer);
			CreateAction(player, "HealPlayerMax", "Restore Full Health", hub.HealPlayerToMaximum);
			CreateAction(player, "KillPlayer", "Kill Player", hub.KillPlayer);
			CreateAction(player, "RevivePlayer", "Revive Here", hub.RevivePlayerHere);
			CreateAction(player, "RespawnCheckpoint", "Respawn Checkpoint", hub.RespawnAtCheckpoint);
			CreateToggle(player, "Invincibility", "Invincibility", hub.EnableInvincibility, hub.DisableInvincibility);
			CreateBackButton(player, controller);

			CreateAction(boss, "RefreshBoss", "Refresh Boss Reference", hub.RefreshBossReference);
			CreateAction(boss, "DamageBoss", "Damage Boss", hub.DamageBoss);
			CreateAction(boss, "HealBoss", "Heal Boss", hub.HealBoss);
			CreateAction(boss, "HealBossMax", "Restore Boss Health", hub.HealBossToMaximum);
			CreateAction(boss, "KillBoss", "Kill Boss", hub.KillBoss);
			CreateBackButton(boss, controller);

			CreateAction(skills, "ActivateSkill", "Activate Selected Skill", hub.ActivateSelectedSkill);
			CreateAction(skills, "EquipPassive", "Equip Placeholder Passive", hub.EquipPlaceholderPassive);
			CreateAction(skills, "EquipActive", "Equip Placeholder Active", hub.EquipPlaceholderActive);
			CreateAction(skills, "UnequipSkill", "Unequip Selected Skill", hub.UnequipSelectedSkill);
			CreateAction(skills, "AddSkillSlot", "Add Skill Slot", hub.AddSkillSlot);
			CreateAction(skills, "RemoveSkillSlot", "Remove Last Skill Slot", hub.RemoveLastSkillSlot);
			CreateBackButton(skills, controller);

			CreateAction(currency, "AddSoul", "Add Soul", hub.AddSoul);
			CreateAction(currency, "SpendSoul", "Spend Soul", hub.SpendSoul);
			CreateAction(currency, "ResetSoul", "Reset Soul", hub.ResetSoul);
			CreateBackButton(currency, controller);

			CreateToggle(world, "SlowMotion", "Slow Motion", hub.EnableSlowMotion, hub.DisableSlowMotion);
			CreateAction(world, "RestartScene", "Restart Current Scene", hub.RestartCurrentScene, true);
			CreateAction(world, "RestartAndResetSoul", "Restart And Reset Soul", hub.RestartCurrentSceneAndResetSoul, true);
			CreateBackButton(world, controller);

			controller.MenuRoot = menuRoot;
			controller.HomePage = home;
			controller.Pages = pages.ToArray();
			controller.TitleText = title;
			controller.BreadcrumbText = breadcrumb;
			controller.StartVisible = false;
			controller.PauseWhileOpen = false;
			controller.ManageCursor = true;
			controller.KeyboardToggleKey = KeyCode.F1;
			controller.ControllerToggleButton = InputControlType.Command;
			controller.ControllerBackButton = InputControlType.Action2;

			InControlMenuSelectionInput input = menuRoot.AddComponent<InControlMenuSelectionInput>();
			input.PanelRoot = menuRoot;
			input.FirstSelected = home.ResolveFirstSelected();
			input.SubmitButton = InputControlType.Action1;
			input.CancelButton = InputControlType.Action2;
			input.UseDPad = true;
			input.UseLeftStick = true;

			foreach (DevModMenuPage page in pages)
			{
				page.gameObject.SetActive(page == home);
			}

			DevTestPanelController[] oldControllers = UnityEngine.Object.FindObjectsByType<DevTestPanelController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			foreach (DevTestPanelController oldController in oldControllers)
			{
				oldController.ListenForShortcut = false;
			}

			EditorUtility.SetDirty(canvas.gameObject);
			EditorSceneManager.MarkSceneDirty(scene);
			EditorSceneManager.SaveScene(scene);
			Selection.activeGameObject = host;
			Debug.Log("[DevModMenuBuilder] Developer mod menu created in Bootstrap.", host);
		}

		private static DevModMenuPage CreatePage(string objectName, string title, Transform parent, List<DevModMenuPage> pages)
		{
			GameObject pageObject = CreateUIObject(objectName, parent);
			Stretch(pageObject.GetComponent<RectTransform>());
			VerticalLayoutGroup layout = pageObject.AddComponent<VerticalLayoutGroup>();
			layout.spacing = 7f;
			layout.childControlHeight = true;
			layout.childControlWidth = true;
			layout.childForceExpandHeight = false;
			DevModMenuPage page = pageObject.AddComponent<DevModMenuPage>();
			page.Title = title;
			pages.Add(page);
			return page;
		}

		private static void CreateFolderButton(DevModMenuPage owner, string objectName, string label, DevModMenuController menu, DevModMenuPage target)
		{
			Button button = CreateButton(objectName, owner.transform, $">  {label}");
			DevModMenuFolderButton folder = button.gameObject.AddComponent<DevModMenuFolderButton>();
			folder.Menu = menu;
			folder.TargetPage = target;
			SetFirst(owner, button);
		}

		private static void CreateAction(DevModMenuPage owner, string objectName, string label, UnityAction action, bool closeAfter = false)
		{
			Button button = CreateButton(objectName, owner.transform, label);
			DevModMenuActionButton item = button.gameObject.AddComponent<DevModMenuActionButton>();
			item.CloseMenuAfterAction = closeAfter;
			UnityEventTools.AddPersistentListener(item.Action, action);
			SetFirst(owner, button);
		}

		private static void CreateToggle(DevModMenuPage owner, string objectName, string label, UnityAction enabledAction, UnityAction disabledAction)
		{
			GameObject root = CreateUIObject(objectName, owner.transform);
			root.AddComponent<Image>().color = ButtonColor;
			root.AddComponent<LayoutElement>().preferredHeight = 46f;
			Toggle toggle = root.AddComponent<Toggle>();
			TMP_Text text = CreateText("Label", root.transform, $"{label}: OFF", 16f, FontStyles.Normal);
			Stretch(text.rectTransform);
			text.margin = new Vector4(12f, 0f, 12f, 0f);
			text.alignment = TextAlignmentOptions.MidlineLeft;
			toggle.targetGraphic = root.GetComponent<Image>();

			DevModMenuToggleItem item = root.AddComponent<DevModMenuToggleItem>();
			item.Label = label;
			item.LabelText = text;
			UnityEventTools.AddPersistentListener(item.EnabledAction, enabledAction);
			UnityEventTools.AddPersistentListener(item.DisabledAction, disabledAction);
			SetFirst(owner, toggle);
		}

		private static void CreateBackButton(DevModMenuPage owner, DevModMenuController controller)
		{
			Button button = CreateButton("BackButton", owner.transform, "<  BACK");
			UnityEventTools.AddPersistentListener(button.onClick, controller.Back);
		}

		private static Button CreateButton(string objectName, Transform parent, string label)
		{
			GameObject root = CreateUIObject(objectName, parent);
			Image image = root.AddComponent<Image>();
			image.color = ButtonColor;
			Button button = root.AddComponent<Button>();
			button.targetGraphic = image;
			ColorBlock colors = button.colors;
			colors.normalColor = ButtonColor;
			colors.highlightedColor = HighlightColor;
			colors.selectedColor = HighlightColor;
			colors.pressedColor = new Color(0.06f, 0.5f, 0.55f, 1f);
			button.colors = colors;
			root.AddComponent<LayoutElement>().preferredHeight = 46f;

			TMP_Text text = CreateText("Label", root.transform, label, 16f, FontStyles.Normal);
			Stretch(text.rectTransform);
			text.margin = new Vector4(12f, 0f, 12f, 0f);
			text.alignment = TextAlignmentOptions.MidlineLeft;
			return button;
		}

		private static TMP_Text CreateText(string name, Transform parent, string value, float size, FontStyles style)
		{
			GameObject textObject = CreateUIObject(name, parent);
			TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
			text.text = value;
			text.fontSize = size;
			text.fontStyle = style;
			text.color = Color.white;
			text.enableWordWrapping = false;
			text.raycastTarget = false;
			return text;
		}

		private static GameObject CreateUIObject(string name, Transform parent)
		{
			GameObject gameObject = new GameObject(name, typeof(RectTransform));
			gameObject.layer = LayerMask.NameToLayer("UI");
			gameObject.transform.SetParent(parent, false);
			return gameObject;
		}

		private static void Stretch(RectTransform rect)
		{
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.offsetMin = Vector2.zero;
			rect.offsetMax = Vector2.zero;
		}

		private static void SetFirst(DevModMenuPage page, Selectable selectable)
		{
			if (page.FirstSelected == null)
			{
				page.FirstSelected = selectable;
			}
		}
	}
}
#endif
