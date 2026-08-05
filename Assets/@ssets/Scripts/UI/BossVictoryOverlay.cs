using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MaskboundJinosi.UI
{
    public static class BossVictoryOverlay
    {
        private const string OverlayName = "BossVictoryOverlay";

        public static void Show(float elapsedTime, bool rewardEligible)
        {
            GameObject existing = GameObject.Find(OverlayName);
            if (existing != null)
            {
                Object.Destroy(existing);
            }

            GameObject root = new GameObject(OverlayName, typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Image backdrop = CreateImage(root.transform, "Backdrop", new Color(0f, 0f, 0f, 0.72f));
            Stretch(backdrop.rectTransform);

            Image panel = CreateImage(root.transform, "Panel", new Color(0.055f, 0.04f, 0.09f, 0.96f));
            RectTransform panelRect = panel.rectTransform;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(850f, 360f);

            CreateText(panelRect, "Title", "BOSS DEFEATED", 72f,
                new Color(1f, 0.78f, 0.2f), new Vector2(0f, 80f), FontStyles.Bold);

            int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(elapsedTime));
            string timeText = $"WAKTU  {totalSeconds / 60:00}:{totalSeconds % 60:00}";
            CreateText(panelRect, "Time", timeText, 46f, Color.white,
                new Vector2(0f, -5f), FontStyles.Normal);

            string result = rewardEligible ? "REWARD EARNED" : "TIME LIMIT EXCEEDED";
            Color resultColor = rewardEligible
                ? new Color(0.35f, 1f, 0.55f)
                : new Color(1f, 0.42f, 0.32f);
            CreateText(panelRect, "Result", result, 30f, resultColor,
                new Vector2(0f, -80f), FontStyles.Bold);

            CreateText(panelRect, "Continue", "TEKAN ATTACK (E) UNTUK MELANJUTKAN", 22f,
                new Color(0.78f, 0.78f, 0.78f), new Vector2(0f, -135f), FontStyles.Normal);
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            GameObject child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            child.transform.SetParent(parent, false);
            Image image = child.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static void CreateText(RectTransform parent, string name, string value, float size,
            Color color, Vector2 position, FontStyles style)
        {
            GameObject child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            child.transform.SetParent(parent, false);

            RectTransform rect = child.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(780f, 90f);

            TextMeshProUGUI text = child.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.fontStyle = style;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
