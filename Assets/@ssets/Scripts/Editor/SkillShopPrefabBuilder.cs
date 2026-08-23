using System.IO;
using MaskboundJinosi.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MaskboundJinosi.Editor
{
    /// <summary>
    /// Editor utilities for the Skill Shop panel.
    ///
    /// "Maskbound/UI Skill Shop/Build Prefab" creates a ready-to-use UI prefab
    /// (Assets/@ssets/Prefabs/UI/SkillShopUI.prefab) with the full layout.
    ///
    /// "Maskbound/UI Skill Shop/Setup Selected" builds the prefab and wires every
    /// reference on the currently selected SkillShopPanel component in the scene.
    /// </summary>
    public static class SkillShopPrefabBuilder
    {
        private const string PrefabPath = "Assets/@ssets/Prefabs/UI/SkillShopUI.prefab";

        private static readonly Color BgColor = new Color(0.03f, 0.04f, 0.06f, 0.95f);
        private static readonly Color PanelColor = new Color(0.06f, 0.08f, 0.12f, 0.9f);
        private static readonly Color SectionColor = new Color(0.04f, 0.05f, 0.08f, 0.6f);
        private static readonly Color SlotEmptyColor = new Color(0.15f, 0.1f, 0.2f, 0.8f);
        private static readonly Color SlotBorderColor = new Color(0.7f, 0.55f, 0.2f, 0.9f);
        private static readonly Color DetailBgColor = new Color(0.05f, 0.06f, 0.1f, 0.92f);
        private static readonly Color GoldColor = new Color(1f, 0.85f, 0.3f);
        private static readonly Color CyanColor = new Color(0.2f, 0.9f, 0.85f);
        private static readonly Color TextColor = new Color(0.9f, 0.9f, 0.9f);
        private static readonly Color MutedColor = new Color(0.5f, 0.5f, 0.55f);
        private static readonly Color GreenColor = new Color(0.3f, 0.85f, 0.4f);
        private static readonly Color BuyColor = new Color(0.15f, 0.55f, 0.35f, 0.9f);

        [MenuItem("Maskbound/UI Skill Shop/Build Prefab")]
        public static void BuildPrefab()
        {
            GameObject root = BuildUI();

            string dir = Path.GetDirectoryName(PrefabPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            // Set active = false on the prefab asset (the runtime builder pattern).
            if (prefab != null)
            {
                prefab.SetActive(false);
                PrefabUtility.SavePrefabAsset(prefab);
                AssetDatabase.SaveAssets();
            }

            EditorUtility.DisplayDialog("Skill Shop",
                "Prefab UI Skill Shop dibuat di:\n" + PrefabPath +
                "\n\nSekarang:\n1. Pilih objek SkillShopPanel di scene\n2. Jalankan 'Setup Selected' untuk auto-wire referensi",
                "OK");

            Selection.activeObject = prefab;
        }

        [MenuItem("Maskbound/UI Skill Shop/Setup Selected")]
        public static void SetupSelected()
        {
            SkillShopPanel panel = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponent<SkillShopPanel>()
                : null;

            if (panel == null)
            {
                EditorUtility.DisplayDialog("Skill Shop",
                    "Pilih dulu objek yang punya komponen SkillShopPanel di scene/hierarchy.",
                    "OK");
                return;
            }

            GameObject ui = BuildUI();
            string dir = Path.GetDirectoryName(PrefabPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(ui, PrefabPath);
            Object.DestroyImmediate(ui);

            // The panel object must stay active in the scene so the child UI can
            // render when opened. The UI root itself starts inactive.
            panel.gameObject.SetActive(true);

            // Instantiate under the panel object so the Canvas scales with the scene.
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(panel.transform, false);
            instance.name = "SkillShopUI";
            instance.SetActive(false);

            WirePanel(panel, instance);

            EditorUtility.SetDirty(panel);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("Skill Shop",
                "Setup selesai.\n\nPrefab: " + PrefabPath +
                "\nInstance: " + instance.name + " (anak dari panel object)\n" +
                "Semua referensi sudah ter-wire.",
                "OK");

            Selection.activeGameObject = instance;
        }

        // ───────────────────── Wiring ─────────────────────

        private static void WirePanel(SkillShopPanel panel, GameObject ui)
        {
            SerializedObject so = new SerializedObject(panel);

            so.FindProperty("panelRoot").objectReferenceValue = ui;
            so.FindProperty("soulCountText").objectReferenceValue = Find(ui, "SoulCount")?.GetComponent<TextMeshProUGUI>();
            so.FindProperty("skillGridParent").objectReferenceValue = Find(ui, "Content")?.transform;
            so.FindProperty("skillEntryPrefab").objectReferenceValue = LoadEntryPrefab();

            so.FindProperty("activeSlotIcons").arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                so.FindProperty("activeSlotIcons").GetArrayElementAtIndex(i).objectReferenceValue =
                    Find(ui, "ActiveSlot" + i)?.transform.Find("Icon")?.GetComponent<Image>();
            }

            so.FindProperty("passiveSlotIcons").arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                so.FindProperty("passiveSlotIcons").GetArrayElementAtIndex(i).objectReferenceValue =
                    Find(ui, "PassiveSlot" + i)?.transform.Find("Icon")?.GetComponent<Image>();
            }

            so.FindProperty("detailIcon").objectReferenceValue = Find(ui, "DetailIcon")?.GetComponent<Image>();
            so.FindProperty("detailTypeText").objectReferenceValue = Find(ui, "DetailType")?.GetComponent<TextMeshProUGUI>();
            so.FindProperty("detailNameText").objectReferenceValue = Find(ui, "DetailName")?.GetComponent<TextMeshProUGUI>();
            so.FindProperty("detailDescText").objectReferenceValue = Find(ui, "DetailDesc")?.GetComponent<TextMeshProUGUI>();
            so.FindProperty("detailStatsText").objectReferenceValue = Find(ui, "DetailStats")?.GetComponent<TextMeshProUGUI>();
            so.FindProperty("detailCostText").objectReferenceValue = Find(ui, "DetailCost")?.GetComponent<TextMeshProUGUI>();
            so.FindProperty("detailSoulIcon").objectReferenceValue = Find(ui, "DetailSoulIcon")?.GetComponent<Image>();
            so.FindProperty("buyButton").objectReferenceValue = Find(ui, "BuyButton")?.GetComponent<Button>();

            so.ApplyModifiedProperties();
        }

        private static GameObject LoadEntryPrefab()
        {
            const string entryPath = "Assets/@ssets/Prefabs/UI/SkillShopEntry.prefab";
            GameObject entry = AssetDatabase.LoadAssetAtPath<GameObject>(entryPath);
            if (entry != null)
            {
                return entry;
            }

            GameObject built = BuildEntryPrefab();
            string dir = Path.GetDirectoryName(entryPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }
            entry = PrefabUtility.SaveAsPrefabAsset(built, entryPath);
            Object.DestroyImmediate(built);
            return entry;
        }

        // ───────────────────── UI Construction ─────────────────────

        private static GameObject BuildUI()
        {
            GameObject root = CreateUI(null, "SkillShopUI", typeof(RectTransform));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;

            // NOTE: No Canvas on this root on purpose. It is instantiated under the
            // scene Canvas (Bootstrap's Canvas), so it inherits that Canvas.

            // Backdrop
            Image backdrop = Add<Image>(CreateUI(root.transform, "Backdrop", typeof(RectTransform)));
            Stretch(backdrop.rectTransform);
            backdrop.color = BgColor;

            // Main panel
            Image mainPanel = Add<Image>(CreateUI(root.transform, "MainPanel", typeof(RectTransform)));
            Stretch(mainPanel.rectTransform);
            mainPanel.color = PanelColor;

            BuildTitleBar(mainPanel.rectTransform);
            BuildLeftSection(mainPanel.rectTransform);
            BuildCenterSection(mainPanel.rectTransform);
            BuildRightSection(mainPanel.rectTransform);

            return root;
        }

        private static void BuildTitleBar(RectTransform parent)
        {
            RectTransform tb = CreateUI(parent, "TitleBar", typeof(RectTransform)).GetComponent<RectTransform>();
            tb.anchorMin = new Vector2(0.55f, 0.88f);
            tb.anchorMax = new Vector2(0.98f, 0.99f);
            tb.offsetMin = tb.offsetMax = Vector2.zero;

            CreateLabel(tb, "Title", "SPIRITUAL GATE", 52f, GoldColor,
                new Vector2(0f, 10f), FontStyles.Bold, TextAlignmentOptions.MidlineRight);
            CreateLabel(tb, "Subtitle", "TRADE SOULS INTO MASK SPIRIT", 20f, MutedColor,
                new Vector2(0f, -18f), FontStyles.Normal, TextAlignmentOptions.MidlineRight);
        }

        private static void BuildLeftSection(RectTransform parent)
        {
            Image leftPanel = Add<Image>(CreateUI(parent, "LeftSection", typeof(RectTransform)));
            RectTransform leftRect = leftPanel.rectTransform;
            leftRect.anchorMin = new Vector2(0f, 0f);
            leftRect.anchorMax = new Vector2(0.42f, 1f);
            leftRect.offsetMin = new Vector2(10f, 10f);
            leftRect.offsetMax = new Vector2(-5f, -10f);
            leftPanel.color = SectionColor;

            // Active slots row
            RectTransform activeRow = CreateUI(leftRect, "ActiveSlots", typeof(HorizontalLayoutGroup)).GetComponent<RectTransform>();
            activeRow.anchorMin = new Vector2(0.05f, 0.85f);
            activeRow.anchorMax = new Vector2(0.95f, 0.98f);
            activeRow.offsetMin = activeRow.offsetMax = Vector2.zero;
            HorizontalLayoutGroup hlg = activeRow.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            for (int i = 0; i < 3; i++)
            {
                CreateSkillSlot(activeRow, "ActiveSlot" + i);
            }

            // Passive slots column
            RectTransform passiveCol = CreateUI(leftRect, "PassiveSlots", typeof(VerticalLayoutGroup)).GetComponent<RectTransform>();
            passiveCol.anchorMin = new Vector2(0.05f, 0.32f);
            passiveCol.anchorMax = new Vector2(0.35f, 0.82f);
            passiveCol.offsetMin = passiveCol.offsetMax = Vector2.zero;
            VerticalLayoutGroup vlg = passiveCol.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 10f;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = true;

            for (int i = 0; i < 3; i++)
            {
                CreateSkillSlot(passiveCol, "PassiveSlot" + i);
            }

            // Player art area
            Image art = Add<Image>(CreateUI(leftRect, "PlayerArt", typeof(RectTransform)));
            RectTransform artRect = art.rectTransform;
            artRect.anchorMin = new Vector2(0.1f, 0.02f);
            artRect.anchorMax = new Vector2(0.9f, 0.3f);
            artRect.offsetMin = artRect.offsetMax = Vector2.zero;
            art.color = new Color(0.1f, 0.12f, 0.15f, 0.5f);

            // Soul count
            Image soulBg = Add<Image>(CreateUI(leftRect, "SoulBg", typeof(RectTransform)));
            RectTransform soulRect = soulBg.rectTransform;
            soulRect.anchorMin = new Vector2(0.05f, 0.02f);
            soulRect.anchorMax = new Vector2(0.4f, 0.12f);
            soulRect.offsetMin = soulRect.offsetMax = Vector2.zero;
            soulBg.color = new Color(0f, 0f, 0f, 0.5f);

            CreateLabel(soulRect, "SoulCount", "80", 42f, GoldColor,
                Vector2.zero, FontStyles.Bold, TextAlignmentOptions.Center);
        }

        private static void BuildCenterSection(RectTransform parent)
        {
            Image gridContainer = Add<Image>(CreateUI(parent, "SkillGrid", typeof(RectTransform)));
            RectTransform gridRect = gridContainer.rectTransform;
            gridRect.anchorMin = new Vector2(0.35f, 0.05f);
            gridRect.anchorMax = new Vector2(0.68f, 0.9f);
            gridRect.offsetMin = new Vector2(5f, 5f);
            gridRect.offsetMax = new Vector2(-5f, -5f);
            gridContainer.color = new Color(0.04f, 0.05f, 0.07f, 0.5f);

            // Scroll view
            ScrollRect scroll = Add<ScrollRect>(CreateUI(gridRect, "Scroll", typeof(RectTransform)));
            RectTransform scrollRect = scroll.GetComponent<RectTransform>();
            Stretch(scrollRect);
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            // Viewport
            Image viewport = Add<Image>(CreateUI(scrollRect, "Viewport", typeof(RectTransform), typeof(Mask)));
            Stretch(viewport.rectTransform);
            viewport.color = new Color(0.05f, 0.04f, 0.07f, 1f);
            Mask mask = viewport.GetComponent<Mask>();
            mask.showMaskGraphic = true;
            scroll.viewport = viewport.rectTransform;

            // Content
            RectTransform content = CreateUI(viewport.transform, "Content",
                typeof(GridLayoutGroup), typeof(ContentSizeFitter)).GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(0f, 0f);

            GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(110f, 110f);
            grid.spacing = new Vector2(12f, 12f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.padding = new RectOffset(10, 10, 10, 10);

            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = content;
        }

        private static void BuildRightSection(RectTransform parent)
        {
            Image detailPanel = Add<Image>(CreateUI(parent, "DetailPanel", typeof(RectTransform)));
            RectTransform detailRect = detailPanel.rectTransform;
            detailRect.anchorMin = new Vector2(0.68f, 0.05f);
            detailRect.anchorMax = new Vector2(0.98f, 0.9f);
            detailRect.offsetMin = new Vector2(5f, 5f);
            detailRect.offsetMax = new Vector2(-5f, -5f);
            detailPanel.color = DetailBgColor;

            // Border top
            Image border = Add<Image>(CreateUI(detailRect, "Border", typeof(RectTransform)));
            RectTransform borderRect = border.rectTransform;
            borderRect.anchorMin = new Vector2(0.05f, 0.95f);
            borderRect.anchorMax = new Vector2(0.95f, 0.97f);
            borderRect.offsetMin = borderRect.offsetMax = Vector2.zero;
            border.color = CyanColor;

            CreateLabel(detailRect, "SectionTitle", "MASK SPIRIT", 32f, TextColor,
                new Vector2(0f, -30f), FontStyles.Bold, TextAlignmentOptions.Center);

            // Icon area
            Image iconBg = Add<Image>(CreateUI(detailRect, "IconBg", typeof(RectTransform)));
            RectTransform iconBgRect = iconBg.rectTransform;
            iconBgRect.anchorMin = new Vector2(0.25f, 0.7f);
            iconBgRect.anchorMax = new Vector2(0.75f, 0.88f);
            iconBgRect.offsetMin = iconBgRect.offsetMax = Vector2.zero;
            iconBg.color = new Color(0.1f, 0.12f, 0.18f, 0.8f);

            Image detailIcon = Add<Image>(CreateUI(iconBgRect, "DetailIcon", typeof(RectTransform)));
            RectTransform iconRect = detailIcon.rectTransform;
            iconRect.anchorMin = new Vector2(0.1f, 0.1f);
            iconRect.anchorMax = new Vector2(0.9f, 0.9f);
            iconRect.offsetMin = iconRect.offsetMax = Vector2.zero;
            detailIcon.color = Color.white;

            CreateLabel(detailRect, "DetailType", "PASSIVE SKILL", 18f, CyanColor,
                new Vector2(0f, -120f), FontStyles.Bold, TextAlignmentOptions.Center);
            CreateLabel(detailRect, "DetailName", "SELECT A SKILL", 30f, GoldColor,
                new Vector2(0f, -155f), FontStyles.Bold, TextAlignmentOptions.Center);

            TextMeshProUGUI desc = CreateLabel(detailRect, "DetailDesc", "Pilih skill dari grid untuk melihat detail.", 16f, TextColor,
                new Vector2(0f, -220f), FontStyles.Normal, TextAlignmentOptions.Center);
            desc.rectTransform.sizeDelta = new Vector2(380f, 100f);
            desc.enableWordWrapping = true;

            TextMeshProUGUI stats = CreateLabel(detailRect, "DetailStats", "", 18f, GreenColor,
                new Vector2(0f, -320f), FontStyles.Normal, TextAlignmentOptions.Center);
            stats.rectTransform.sizeDelta = new Vector2(380f, 80f);
            stats.enableWordWrapping = true;

            CreateLabel(detailRect, "CostLabel", "Skill Cost", 16f, MutedColor,
                new Vector2(0f, -430f), FontStyles.Normal, TextAlignmentOptions.Center);

            // Cost row
            RectTransform costRow = CreateUI(detailRect, "CostRow", typeof(HorizontalLayoutGroup)).GetComponent<RectTransform>();
            costRow.anchorMin = new Vector2(0.15f, 0.03f);
            costRow.anchorMax = new Vector2(0.85f, 0.12f);
            costRow.offsetMin = costRow.offsetMax = Vector2.zero;
            HorizontalLayoutGroup costLayout = costRow.GetComponent<HorizontalLayoutGroup>();
            costLayout.spacing = 8f;
            costLayout.childAlignment = TextAnchor.MiddleCenter;
            costLayout.childForceExpandWidth = false;
            costLayout.childForceExpandHeight = true;

            Image soulIcon = Add<Image>(CreateUI(costRow, "DetailSoulIcon", typeof(RectTransform), typeof(LayoutElement)));
            soulIcon.color = CyanColor;
            LayoutElement le = soulIcon.GetComponent<LayoutElement>();
            le.preferredWidth = 36f;
            le.preferredHeight = 36f;

            CreateLabel(costRow, "DetailCost", "30", 36f, GoldColor,
                Vector2.zero, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);

            // Buy button. Its label is turned into the "PRESS F TO BUY" prompt at
            // runtime by SkillShopPanel; the button visual is hidden.
            Button buyBtn = Add<Button>(CreateUI(detailRect, "BuyButton", typeof(RectTransform), typeof(Image)));
            RectTransform buyRect = buyBtn.GetComponent<RectTransform>();
            buyRect.anchorMin = new Vector2(0.1f, 0.14f);
            buyRect.anchorMax = new Vector2(0.9f, 0.22f);
            buyRect.offsetMin = buyRect.offsetMax = Vector2.zero;

            Image buyImg = buyBtn.GetComponent<Image>();
            buyImg.color = BuyColor;
            buyBtn.targetGraphic = buyImg;

            CreateLabel(buyRect, "BuyLabel", "BUY", 24f, Color.white,
                Vector2.zero, FontStyles.Bold, TextAlignmentOptions.Center);
        }

        private static void CreateSkillSlot(RectTransform parent, string name)
        {
            Image slot = Add<Image>(CreateUI(parent, name, typeof(RectTransform), typeof(LayoutElement)));
            slot.color = SlotEmptyColor;
            LayoutElement le = slot.GetComponent<LayoutElement>();
            le.preferredWidth = 80f;
            le.preferredHeight = 80f;

            // Border
            Image border = Add<Image>(CreateUI(slot.transform, "Border", typeof(RectTransform)));
            Stretch(border.rectTransform);
            border.color = SlotBorderColor;
            border.raycastTarget = false;

            // Inner fill
            Image inner = Add<Image>(CreateUI(slot.transform, "Icon", typeof(RectTransform)));
            RectTransform innerRect = inner.rectTransform;
            innerRect.anchorMin = new Vector2(0.08f, 0.08f);
            innerRect.anchorMax = new Vector2(0.92f, 0.92f);
            innerRect.offsetMin = innerRect.offsetMax = Vector2.zero;
            inner.color = SlotEmptyColor;
            inner.raycastTarget = false;

            // Plus icon
            TextMeshProUGUI plus = Add<TextMeshProUGUI>(CreateUI(slot.transform, "Plus", typeof(RectTransform)));
            Stretch(plus.rectTransform);
            plus.text = "+";
            plus.fontSize = 36f;
            plus.color = MutedColor;
            plus.alignment = TextAlignmentOptions.Center;
            plus.raycastTarget = false;
        }

        private static GameObject BuildEntryPrefab()
        {
            GameObject entry = CreateUI(null, "SkillEntry", typeof(RectTransform), typeof(Image), typeof(Button));
            Image bg = entry.GetComponent<Image>();
            bg.color = new Color(0.12f, 0.1f, 0.16f, 0.9f);

            Button btn = entry.GetComponent<Button>();
            btn.targetGraphic = bg;
            ColorBlock ecb = btn.colors;
            ecb.highlightedColor = new Color(0.2f, 0.18f, 0.28f);
            ecb.pressedColor = new Color(0.1f, 0.08f, 0.14f);
            btn.colors = ecb;

            Image icon = Add<Image>(CreateUI(entry.transform, "Icon", typeof(RectTransform)));
            RectTransform iconRect = icon.rectTransform;
            iconRect.anchorMin = new Vector2(0.1f, 0.1f);
            iconRect.anchorMax = new Vector2(0.9f, 0.9f);
            iconRect.offsetMin = iconRect.offsetMax = Vector2.zero;
            icon.color = Color.white;
            icon.raycastTarget = false;

            return entry;
        }

        // ───────────────────── Helpers ─────────────────────

        private static GameObject CreateUI(Transform parent, string name, params System.Type[] components)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            foreach (System.Type comp in components)
            {
                if (comp == typeof(RectTransform)) continue;
                go.AddComponent(comp);
            }
            return go;
        }

        private static T Add<T>(GameObject go) where T : Component
        {
            T comp = go.GetComponent<T>();
            if (comp == null) comp = go.AddComponent<T>();
            return comp;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static TextMeshProUGUI CreateLabel(RectTransform parent, string name, string value,
            float size, Color color, Vector2 position, FontStyles style, TextAlignmentOptions alignment)
        {
            GameObject go = CreateUI(parent, name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(380f, 50f);

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = value;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Transform Find(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;

            foreach (Transform child in root)
            {
                Transform result = Find(child, name);
                if (result != null) return result;
            }
            return null;
        }

        private static Transform Find(GameObject root, string name) => Find(root != null ? root.transform : null, name);
    }
}
