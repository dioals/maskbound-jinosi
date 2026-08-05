using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MaskboundJinosi.UI
{
    public static class GameOverOverlay
    {
        private const string OverlayName = "GameOverOverlay";

        public static void Show(float elapsedTime)
        {
            if (GameObject.Find(OverlayName) != null)
            {
                return;
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

            Image backdrop = CreateImage(root.transform, "Backdrop", new Color(0f, 0f, 0f, 0.78f));
            Stretch(backdrop.rectTransform);

            Image panel = CreateImage(root.transform, "Panel", new Color(0.08f, 0.025f, 0.035f, 0.97f));
            RectTransform panelRect = panel.rectTransform;
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(850f, 330f);

            CreateText(panelRect, "Title", "GAME OVER", 82f, new Color(1f, 0.2f, 0.22f),
                new Vector2(0f, 65f), FontStyles.Bold);

            int seconds = Mathf.Max(0, Mathf.FloorToInt(elapsedTime));
            CreateText(panelRect, "Time", $"WAKTU  {seconds / 60:00}:{seconds % 60:00}", 42f,
                Color.white, new Vector2(0f, -25f), FontStyles.Normal);

            CreateText(panelRect, "Message", "TEKAN ATTACK (E) UNTUK MELANJUTKAN", 26f,
                new Color(0.75f, 0.75f, 0.75f), new Vector2(0f, -90f), FontStyles.Normal);
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
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
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
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
