using MaskboundJinosi.Skills;
using MoreMountains.CorgiEngine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MaskboundJinosi.UI
{
    [AddComponentMenu("Maskbound/UI/Skill Cooldown Slot UI")]
    public class SkillCooldownSlotUI : MonoBehaviour
    {
        [Header("Slot")]
        [Min(0)] [SerializeField] private int slotIndex;
        [SerializeField] private CharacterSkillCaster skillCaster;

        [Header("Icon")]
        [SerializeField] private Image skillIconImage;
        [SerializeField] private SpriteRenderer skillIconRenderer;

        [Header("Selection")]
        [Tooltip("Image frame/highlight yang tampil ketika slot ini sedang dipilih.")]
        [SerializeField] private Image selectionHighlight;
        [SerializeField] private bool hideSelectionWhenSlotEmpty = true;

        [Header("Cooldown")]
        [Tooltip("Gunakan Image Type Filled/Radial 360 untuk efek cooldown melingkar.")]
        [SerializeField] private Image cooldownOverlay;
        [SerializeField] private TMP_Text cooldownText;
        [SerializeField] private bool showDecimalBelowOneSecond;
        [SerializeField] private Color readyColor = Color.white;
        [SerializeField] private Color cooldownColor = new Color(0.45f, 0.45f, 0.45f, 1f);

        [Header("Selection Darken")]
        [Tooltip("Warna icon saat slot ini TIDAK sedang dipilih. Icon hanya balik ke readyColor/cooldownColor ketika dipilih.")]
        [SerializeField] private Color unselectedColor = new Color(0.35f, 0.35f, 0.35f, 1f);

        private Skill _displayedSkill;

        private void Awake()
        {
            ConfigureOverlay();
            ResolveCaster();
            RefreshIcon();
        }

        private void Update()
        {
            if (skillCaster == null)
            {
                ResolveCaster();
            }

            RefreshIcon();
            RefreshCooldown();
            RefreshSelection();
        }

        private void ResolveCaster()
        {
            if (skillCaster != null)
            {
                return;
            }

            if (LevelManager.HasInstance &&
                LevelManager.Instance.Players != null &&
                LevelManager.Instance.Players.Count > 0 &&
                LevelManager.Instance.Players[0] != null)
            {
                skillCaster = LevelManager.Instance.Players[0]
                    .GetComponentInChildren<CharacterSkillCaster>(true);
            }
        }

        private void RefreshIcon()
        {
            Skill skill = null;
            if (skillCaster != null && skillCaster.SkillSlots != null)
            {
                skill = skillCaster.SkillSlots.GetSkill(slotIndex);
            }

            if (_displayedSkill == skill)
            {
                return;
            }

            _displayedSkill = skill;
            Sprite icon = skill != null ? skill.Icon : null;

            if (skillIconImage != null)
            {
                skillIconImage.sprite = icon;
                skillIconImage.enabled = icon != null;
            }

            if (skillIconRenderer != null)
            {
                skillIconRenderer.sprite = icon;
                skillIconRenderer.enabled = icon != null;
            }

            if (cooldownOverlay != null)
            {
                cooldownOverlay.sprite = icon;
            }
        }

        private void RefreshCooldown()
        {
            float remaining = 0f;
            float duration = 0f;

            ActiveSkillData activeSkill = _displayedSkill as ActiveSkillData;
            if (skillCaster != null && activeSkill != null)
            {
                float skillRemaining = skillCaster.GetCooldownRemaining(activeSkill);
                float globalRemaining = skillCaster.GetGlobalCooldownRemaining();
                remaining = Mathf.Max(skillRemaining, globalRemaining);
                duration = skillRemaining >= globalRemaining
                    ? activeSkill.Cooldown
                    : skillCaster.GlobalCooldown;
            }

            bool coolingDown = remaining > 0f;
            float fill = duration > 0f ? Mathf.Clamp01(remaining / duration) : 0f;

            if (cooldownOverlay != null)
            {
                cooldownOverlay.enabled = coolingDown && cooldownOverlay.sprite != null;
                cooldownOverlay.fillAmount = fill;
            }

            bool selected = skillCaster != null &&
                skillCaster.SelectedSkillSlotIndex == slotIndex;
            Color iconColor = selected
                ? (coolingDown ? cooldownColor : readyColor)
                : unselectedColor;
            if (skillIconImage != null)
            {
                skillIconImage.color = iconColor;
            }
            if (skillIconRenderer != null)
            {
                skillIconRenderer.color = iconColor;
            }

            if (cooldownText != null)
            {
                cooldownText.gameObject.SetActive(coolingDown);
                cooldownText.text = showDecimalBelowOneSecond && remaining < 1f
                    ? remaining.ToString("0.0")
                    : Mathf.CeilToInt(remaining).ToString();
            }
        }

        private void RefreshSelection()
        {
            if (selectionHighlight == null)
            {
                return;
            }

            bool selected = skillCaster != null &&
                skillCaster.SelectedSkillSlotIndex == slotIndex;
            if (hideSelectionWhenSlotEmpty && _displayedSkill == null)
            {
                selected = false;
            }

            selectionHighlight.enabled = selected;
        }

        private void ConfigureOverlay()
        {
            if (selectionHighlight != null)
            {
                selectionHighlight.raycastTarget = false;
            }

            if (cooldownOverlay == null)
            {
                return;
            }

            cooldownOverlay.type = Image.Type.Filled;
            cooldownOverlay.fillMethod = Image.FillMethod.Radial360;
            cooldownOverlay.fillOrigin = (int)Image.Origin360.Top;
            cooldownOverlay.fillClockwise = false;
            cooldownOverlay.raycastTarget = false;
        }
    }
}
