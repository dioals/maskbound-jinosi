#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using InControl;
using MaskboundJinosi.Gameplay.Scene;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MaskboundJinosi.UI.Editor
{
	public static class MainMenuBootstrapBuilder
	{
		private const string BootstrapPath = "Assets/@ssets/Scenes/Levels/Bootstrap.unity";
		private const string BuildRequestPath = "tmp/build-main-menu.request";
		private static readonly Color BackgroundColor = new Color(0.025f, 0.035f, 0.04f, 0.98f);
		private static readonly Color PanelColor = new Color(0.055f, 0.07f, 0.075f, 0.98f);
		private static readonly Color ButtonColor = new Color(0.11f, 0.14f, 0.145f, 1f);
		private static readonly Color SelectedColor = new Color(0.08f, 0.68f, 0.62f, 1f);
		private static Image _pauseOverlayStyle;
		private static Image _pauseBandStyle;
		private static Button _pauseButtonStyle;
		private static TMP_Text _pauseButtonTextStyle;

		[InitializeOnLoadMethod]
		private static void RunPendingBuildRequest()
		{
			EditorApplication.delayCall += () =>
			{
				if (!File.Exists(BuildRequestPath)) return;
				File.Delete(BuildRequestPath);
				Build();
			};
		}

		[MenuItem("Maskbound/Main Menu/Build Main Menu In Bootstrap")]
		public static void Build()
		{
			if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

			Scene scene = EditorSceneManager.OpenScene(BootstrapPath, OpenSceneMode.Single);
			Canvas canvas = FindNamedComponent<Canvas>("Canvas");
			BootstrapSceneLoader loader = Object.FindFirstObjectByType<BootstrapSceneLoader>(FindObjectsInactive.Include);
			if (canvas == null || loader == null)
			{
				Debug.LogError("[MainMenuBuilder] Bootstrap requires Canvas and BootstrapSceneLoader.");
				return;
			}

			CapturePauseSplashStyle();

			Transform oldMenu = canvas.transform.Find("MainMenuHost");
			if (oldMenu != null) Object.DestroyImmediate(oldMenu.gameObject);

			SerializedObject loaderData = new SerializedObject(loader);
			loaderData.FindProperty("loadFirstLevelOnStart").boolValue = false;
			loaderData.FindProperty("showSceneMenuOnStart").boolValue = false;
			loaderData.FindProperty("firstLevelName").stringValue = "Aras_Mamungkut_Forest";
			loaderData.ApplyModifiedPropertiesWithoutUndo();

			GameFlowManager flow = loader.GetComponent<GameFlowManager>();
			if (flow == null) flow = loader.gameObject.AddComponent<GameFlowManager>();

			GameObject host = CreateUIObject("MainMenuHost", canvas.transform);
			Stretch(host.GetComponent<RectTransform>());
			Image hostImage = host.AddComponent<Image>();
			if (_pauseOverlayStyle != null) CopyImageStyle(_pauseOverlayStyle, hostImage);
			else hostImage.color = BackgroundColor;
			MainMenuController menu = host.AddComponent<MainMenuController>();

			GameObject panel = CreateUIObject("MenuPanel", host.transform);
			RectTransform panelRect = panel.GetComponent<RectTransform>();
			panelRect.anchorMin = new Vector2(0f, 0f);
			panelRect.anchorMax = new Vector2(0f, 1f);
			panelRect.pivot = new Vector2(0f, 0.5f);
			panelRect.anchoredPosition = Vector2.zero;
			panelRect.sizeDelta = new Vector2(732f, 0f);
			Image panelImage = panel.AddComponent<Image>();
			if (_pauseBandStyle != null) CopyImageStyle(_pauseBandStyle, panelImage);
			else panelImage.color = PanelColor;
			VerticalLayoutGroup panelLayout = panel.AddComponent<VerticalLayoutGroup>();
			panelLayout.padding = new RectOffset(82, 82, 70, 70);
			panelLayout.spacing = 12f;
			panelLayout.childControlHeight = true;
			panelLayout.childControlWidth = true;
			panelLayout.childForceExpandHeight = false;

			TMP_Text gameTitle = CreateText("GameTitle", panel.transform, "MASKBOUND JINOSI", 42f, FontStyles.Bold);
			gameTitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 70f;
			TMP_Text pageTitle = CreateText("PageTitle", panel.transform, "MAIN MENU", 24f, FontStyles.Bold);
			pageTitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 42f;

			GameObject pagesRoot = CreateUIObject("Pages", panel.transform);
			pagesRoot.AddComponent<LayoutElement>().flexibleHeight = 1f;
			Stretch(pagesRoot.GetComponent<RectTransform>());

			List<MainMenuPage> pages = new List<MainMenuPage>();
			MainMenuPage home = CreatePage("HomePage", "MAIN MENU", pagesRoot.transform, pages);
			MainMenuPage settings = CreatePage("SettingsPage", "SETTINGS", pagesRoot.transform, pages);
			MainMenuPage credits = CreatePage("CreditsPage", "CREDITS", pagesRoot.transform, pages);
			MainMenuPage devLevels = CreatePage("DevLevelSelectPage", "LEVEL SELECT [DEV]", pagesRoot.transform, pages);

			Button newGame = CreateAction(home, "NewGameButton", "NEW GAME", flow.StartNewGame);
			Button continueGame = CreateAction(home, "ContinueButton", "CONTINUE", flow.ContinueGame);
			Button settingsFolder = CreateFolder(home, "SettingsButton", "SETTINGS", menu, settings);
			Button creditsFolder = CreateFolder(home, "CreditsButton", "CREDITS", menu, credits);
			Button devFolder = CreateFolder(home, "DevLevelSelectButton", "LEVEL SELECT  [DEV]", menu, devLevels);
			CreateAction(home, "QuitButton", "QUIT GAME", flow.QuitGame);

			TMP_Text settingsValue = CreateText("SettingsValueText", settings.transform, "Volume 100%    Fullscreen ON", 15f, FontStyles.Normal);
			settingsValue.color = new Color(0.72f, 0.76f, 0.77f, 1f);
			settingsValue.gameObject.AddComponent<LayoutElement>().preferredHeight = 38f;
			CreateAction(settings, "VolumeDownButton", "MASTER VOLUME  -", flow.DecreaseMasterVolume);
			CreateAction(settings, "VolumeUpButton", "MASTER VOLUME  +", flow.IncreaseMasterVolume);
			CreateAction(settings, "FullscreenButton", "TOGGLE FULLSCREEN", flow.ToggleFullscreen);
			CreateAction(settings, "ClearContinueButton", "CLEAR CONTINUE DATA", flow.ClearContinueData);
			CreateBack(settings, menu);

			TMP_Text creditsText = CreateText("CreditsText", credits.transform,
				"MASKBOUND JINOSI\n\nGame development team\nCorgi Engine + InControl", 17f, FontStyles.Normal);
			creditsText.enableWordWrapping = true;
			creditsText.alignment = TextAlignmentOptions.TopLeft;
			creditsText.gameObject.AddComponent<LayoutElement>().preferredHeight = 260f;
			CreateBack(credits, menu);

			CreateSceneButton(devLevels, "ArasButton", "ARAS MAMUNGKUT FOREST", flow, "Aras_Mamungkut_Forest");
			CreateSceneButton(devLevels, "DahaButton", "DAHA KINGDOM", flow, "Daha_Kingdom");
			CreateSceneButton(devLevels, "SabrangButton", "SABRANG KINGDOM", flow, "Sabrang_Kingdom");
			CreateSceneButton(devLevels, "SendangButton", "SENDANG SANCTUM", flow, "Sendang_Sanctum");
			CreateBack(devLevels, menu);

			menu.HomePage = home;
			menu.Pages = pages.ToArray();
			menu.PageTitleText = pageTitle;
			menu.ControllerBackButton = InputControlType.Action2;

			InControlMenuSelectionInput input = host.AddComponent<InControlMenuSelectionInput>();
			input.PanelRoot = host;
			input.FirstSelected = newGame;
			input.SubmitButton = InputControlType.Action1;
			input.CancelButton = InputControlType.Action2;
			input.UseDPad = true;
			input.UseLeftStick = true;
			input.StickThreshold = 0.5f;

			flow.SceneLoader = loader;
			flow.BootstrapSceneName = "Bootstrap";
			flow.FirstGameplayScene = "Aras_Mamungkut_Forest";
			flow.LoadingSceneName = "LoadingScreen";
			flow.MainMenuRoot = host;
			flow.ContinueButton = continueGame;
			flow.SettingsValueText = settingsValue;
			flow.GameplayUIRoots = FindGameplayUIRoots();
			flow.DevelopmentOnlyObjects = new[] { devFolder.gameObject, devLevels.gameObject };

			foreach (MainMenuPage page in pages) page.gameObject.SetActive(page == home);

			EditorUtility.SetDirty(loader);
			EditorUtility.SetDirty(flow);
			EditorUtility.SetDirty(host);
			EditorSceneManager.MarkSceneDirty(scene);
			EditorSceneManager.SaveScene(scene);
			Selection.activeGameObject = host;
			Debug.Log("[MainMenuBuilder] Main menu created in Bootstrap. Joystick navigation is enabled.", host);
		}

		private static MainMenuPage CreatePage(string name, string title, Transform parent, List<MainMenuPage> pages)
		{
			GameObject root = CreateUIObject(name, parent);
			Stretch(root.GetComponent<RectTransform>());
			VerticalLayoutGroup layout = root.AddComponent<VerticalLayoutGroup>();
			layout.spacing = 8f;
			layout.childControlHeight = true;
			layout.childControlWidth = true;
			layout.childForceExpandHeight = false;
			MainMenuPage page = root.AddComponent<MainMenuPage>();
			page.Title = title;
			pages.Add(page);
			return page;
		}

		private static Button CreateFolder(MainMenuPage page, string name, string label, MainMenuController menu, MainMenuPage target)
		{
			Button button = CreateButton(name, page.transform, $">  {label}");
			MainMenuFolderButton folder = button.gameObject.AddComponent<MainMenuFolderButton>();
			folder.Menu = menu;
			folder.TargetPage = target;
			SetFirst(page, button);
			return button;
		}

		private static Button CreateAction(MainMenuPage page, string name, string label, UnityAction action)
		{
			Button button = CreateButton(name, page.transform, label);
			UnityEventTools.AddPersistentListener(button.onClick, action);
			SetFirst(page, button);
			return button;
		}

		private static void CreateSceneButton(MainMenuPage page, string name, string label, GameFlowManager flow, string sceneName)
		{
			Button button = CreateButton(name, page.transform, label);
			UnityEventTools.AddStringPersistentListener(button.onClick, flow.LoadDeveloperScene, sceneName);
			SetFirst(page, button);
		}

		private static void CreateBack(MainMenuPage page, MainMenuController menu)
		{
			Button button = CreateButton("BackButton", page.transform, "<  BACK");
			UnityEventTools.AddPersistentListener(button.onClick, menu.Back);
			SetFirst(page, button);
		}

		private static Button CreateButton(string name, Transform parent, string label)
		{
			GameObject root = CreateUIObject(name, parent);
			Image image = root.AddComponent<Image>();
			image.color = ButtonColor;
			Button button = root.AddComponent<Button>();
			button.targetGraphic = image;
			ColorBlock colors = button.colors;
			colors.normalColor = ButtonColor;
			colors.highlightedColor = SelectedColor;
			colors.selectedColor = SelectedColor;
			colors.pressedColor = new Color(0.05f, 0.48f, 0.44f, 1f);
			colors.disabledColor = new Color(0.08f, 0.09f, 0.09f, 0.65f);
			button.colors = colors;
			root.AddComponent<LayoutElement>().preferredHeight = 72f;

			if (_pauseButtonStyle != null)
			{
				CopyImageStyle(_pauseButtonStyle.targetGraphic as Image, image);
				button.transition = _pauseButtonStyle.transition;
				button.colors = _pauseButtonStyle.colors;
				button.spriteState = _pauseButtonStyle.spriteState;
				button.animationTriggers = _pauseButtonStyle.animationTriggers;
			}

			TMP_Text text = CreateText("Label", root.transform, label, 20f, FontStyles.Bold);
			Stretch(text.rectTransform);
			text.margin = new Vector4(14f, 0f, 14f, 0f);
			text.alignment = TextAlignmentOptions.MidlineLeft;
			return button;
		}

		private static TMP_Text CreateText(string name, Transform parent, string value, float size, FontStyles style)
		{
			GameObject root = CreateUIObject(name, parent);
			TextMeshProUGUI text = root.AddComponent<TextMeshProUGUI>();
			text.text = value;
			text.fontSize = size;
			text.fontStyle = style;
			text.color = Color.white;
			text.enableWordWrapping = false;
			text.raycastTarget = false;
			if (_pauseButtonTextStyle != null)
			{
				text.font = _pauseButtonTextStyle.font;
				text.fontSharedMaterial = _pauseButtonTextStyle.fontSharedMaterial;
				text.color = _pauseButtonTextStyle.color;
				text.characterSpacing = _pauseButtonTextStyle.characterSpacing;
			}
			return text;
		}

		private static void CapturePauseSplashStyle()
		{
			GameObject pauseSplash = FindNamedObject("PauseSplash");
			if (pauseSplash == null) return;

			_pauseOverlayStyle = pauseSplash.GetComponent<Image>();
			Transform whiteBand = FindChildRecursive(pauseSplash.transform, "WhiteBand");
			_pauseBandStyle = whiteBand != null ? whiteBand.GetComponent<Image>() : null;
			Transform resume = FindChildRecursive(pauseSplash.transform, "ButtonResume");
			_pauseButtonStyle = resume != null ? resume.GetComponent<Button>() : null;
			_pauseButtonTextStyle = resume != null ? resume.GetComponentInChildren<TMP_Text>(true) : null;
		}

		private static Transform FindChildRecursive(Transform root, string childName)
		{
			foreach (Transform child in root)
			{
				if (child.name == childName) return child;
				Transform nested = FindChildRecursive(child, childName);
				if (nested != null) return nested;
			}
			return null;
		}

		private static void CopyImageStyle(Image source, Image target)
		{
			if (source == null || target == null) return;
			target.sprite = source.sprite;
			target.overrideSprite = source.overrideSprite;
			target.material = source.material;
			target.color = source.color;
			target.type = source.type;
			target.preserveAspect = source.preserveAspect;
			target.fillCenter = source.fillCenter;
			target.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
		}

		private static GameObject[] FindGameplayUIRoots()
		{
			List<GameObject> roots = new List<GameObject>();
			string[] names = { "HUD" };
			foreach (string name in names)
			{
				GameObject target = FindNamedObject(name);
				if (target != null) roots.Add(target);
			}
			return roots.ToArray();
		}

		private static T FindNamedComponent<T>(string name) where T : Component
		{
			GameObject target = FindNamedObject(name);
			return target != null ? target.GetComponent<T>() : null;
		}

		private static GameObject FindNamedObject(string name)
		{
			GameObject[] objects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			foreach (GameObject target in objects)
			{
				if (target != null && target.name == name) return target;
			}
			return null;
		}

		private static GameObject CreateUIObject(string name, Transform parent)
		{
			GameObject root = new GameObject(name, typeof(RectTransform));
			root.layer = LayerMask.NameToLayer("UI");
			root.transform.SetParent(parent, false);
			return root;
		}

		private static void Stretch(RectTransform rect)
		{
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.offsetMin = Vector2.zero;
			rect.offsetMax = Vector2.zero;
		}

		private static void SetFirst(MainMenuPage page, Selectable selectable)
		{
			if (page.FirstSelected == null) page.FirstSelected = selectable;
		}
	}
}
#endif
