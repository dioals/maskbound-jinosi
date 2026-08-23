using System;
using System.Collections.Generic;
using MaskboundJinosi.Input;
using MaskboundJinosi.Skills;
using MaskboundJinosi.Soul;
using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MaskboundJinosi.UI
{
    /// <summary>
    /// Full-screen "Spiritual Gate" skill shop panel.
    /// Left: player art area + equipment slots + soul count.
    /// Center: scrollable skill icon grid with WASD/joystick navigation.
    /// Right: detail panel showing selected skill info and buy prompt.
    ///
    /// Navigation: WASD or left stick / D-pad.
    /// Buy: Interact (F on keyboard, or the interact button on controller).
    /// Close: Meditate (M) or Escape.
    ///
    /// All UI references are wired manually in the Inspector.
    /// Use the "Maskbound/UI Skill Shop/Setup Selected" editor menu to
    /// auto-generate the prefab and wire every reference.
    /// </summary>
    [AddComponentMenu("Maskbound/UI/Skill Shop Panel")]
    public class SkillShopPanel : MonoBehaviour
    {
        [Header("Data")]
        [Tooltip("Skills available to buy in the shop grid.")]
        [SerializeField] private ActiveSkillData[] availableSkills;

        [Header("Root")]
        [Tooltip("Root object that gets toggled on open/close.")]
        [SerializeField] private GameObject panelRoot;

        [Header("Slots")]
        [Tooltip("Active skill slot icons (3).")]
        [SerializeField] private Image[] activeSlotIcons = new Image[3];
        [Tooltip("Passive skill slot icons (3).")]
        [SerializeField] private Image[] passiveSlotIcons = new Image[3];

        [Header("Left Section")]
        [SerializeField] private TextMeshProUGUI soulCountText;

        [Header("Center Section")]
        [Tooltip("Parent transform under which skill grid entries are instantiated.")]
        [SerializeField] private Transform skillGridParent;
        [Tooltip("Prefab used for each skill grid entry (must have an Icon Image child).")]
        [SerializeField] private GameObject skillEntryPrefab;

        [Header("Detail Panel")]
        [SerializeField] private Image detailIcon;
        [SerializeField] private TextMeshProUGUI detailTypeText;
        [SerializeField] private TextMeshProUGUI detailNameText;
        [SerializeField] private TextMeshProUGUI detailDescText;
        [SerializeField] private TextMeshProUGUI detailStatsText;
        [SerializeField] private TextMeshProUGUI detailCostText;
        [SerializeField] private Image detailSoulIcon;
        [Tooltip("Existing Buy button. Its visual is hidden at runtime and its label becomes the buy prompt text.")]
        [SerializeField] private Button buyButton;

        [Header("Navigation")]
        [Tooltip("Number of columns in the skill grid (matches the GridLayoutGroup constraint).")]
        [SerializeField] private int gridColumns = 3;

        private SkillSlotManager _slotManager;
        private Action _onClosed;
        private bool _isOpen;
        private bool _hudHidden;

        private readonly List<ActiveSkillData> _visibleSkills = new List<ActiveSkillData>();
        private readonly List<GameObject> _skillEntries = new List<GameObject>();
        private readonly List<Image> _entryBgImages = new List<Image>();
        private readonly Color _entryNormalColor = new Color(0.12f, 0.1f, 0.16f, 0.9f);
        private readonly Color _entrySelectedColor = new Color(0.28f, 0.22f, 0.1f, 0.95f);
        private int _selectedIndex = -1;
        private ActiveSkillData _selectedSkill;

        private TextMeshProUGUI _buyPromptText;
        private bool _buyPromptResolved;

        private float _navRepeatTimer;
        private Vector2Int _navDirection;
        private bool _navFreshPress;
        private const float NavRepeatDelay = 0.2f;

        public bool IsOpen => _isOpen;

        // ───────────────────── Public API ─────────────────────

        public void Open(SkillSlotManager slotManager, Action onClosed = null)
        {
            if (_isOpen) return;

            _slotManager = slotManager;
            _onClosed = onClosed;
            _isOpen = true;

            RefreshAll();
            HideHud();
            if (panelRoot != null) panelRoot.SetActive(true);
            Time.timeScale = 0f;
        }

        public void Close()
        {
            if (!_isOpen) return;

            _isOpen = false;
            Time.timeScale = 1f;
            if (panelRoot != null) panelRoot.SetActive(false);
            ShowHud();
            _onClosed?.Invoke();
        }

        // ───────────────────── Update (navigation + input) ─────────────────────

        private void Update()
        {
            if (!_isOpen) return;

            bool interactDown = false;
            bool meditateDown = false;
            Vector2 moveInput = Vector2.zero;

            if (InputManager.HasInstance)
            {
                InputManager input = InputManager.Instance;
                interactDown = input.InteractButton != null
                    && input.InteractButton.State.CurrentState == MMInput.ButtonStates.ButtonDown;
                moveInput = input.PrimaryMovement;

                MaskboundInControlInputManager maskbound = input as MaskboundInControlInputManager;
                meditateDown = maskbound != null && maskbound.MeditateButton != null
                    && maskbound.MeditateButton.State.CurrentState == MMInput.ButtonStates.ButtonDown;
            }

            // Keyboard fallbacks (in case the input manager is not active or unbound).
            if (UnityEngine.Input.GetKeyDown(KeyCode.F))
            {
                interactDown = true;
            }
            if (UnityEngine.Input.GetKeyDown(KeyCode.M) || UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                meditateDown = true;
            }
            if (UnityEngine.Input.GetKey(KeyCode.W) || UnityEngine.Input.GetKey(KeyCode.UpArrow)) moveInput.y += 1f;
            if (UnityEngine.Input.GetKey(KeyCode.S) || UnityEngine.Input.GetKey(KeyCode.DownArrow)) moveInput.y -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.A) || UnityEngine.Input.GetKey(KeyCode.LeftArrow)) moveInput.x -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.D) || UnityEngine.Input.GetKey(KeyCode.RightArrow)) moveInput.x += 1f;

            HandleNavigation(moveInput);

            if (interactDown)
            {
                TryBuySelected();
                return;
            }

            if (meditateDown)
            {
                Close();
            }
        }

        private void HandleNavigation(Vector2 moveInput)
        {
            if (_visibleSkills.Count == 0) return;

            int dx = 0;
            int dy = 0;

            if (moveInput.x > 0.5f) dx = 1;
            else if (moveInput.x < -0.5f) dx = -1;
            if (moveInput.y > 0.5f) dy = 1;
            else if (moveInput.y < -0.5f) dy = -1;

            Vector2Int dir = new Vector2Int(dx, dy);

            // No input: reset hold state.
            if (dir == Vector2Int.zero)
            {
                _navRepeatTimer = 0f;
                _navDirection = Vector2Int.zero;
                _navFreshPress = false;
                return;
            }

            // Direction changed: this is a fresh press, move immediately.
            if (dir != _navDirection)
            {
                _navDirection = dir;
                _navFreshPress = true;
                _navRepeatTimer = 0f;
                MoveSelection(dx, dy);
                return;
            }

            // Same direction held: only move again after the repeat delay.
            if (_navFreshPress)
            {
                _navRepeatTimer += Time.unscaledDeltaTime;
                if (_navRepeatTimer >= NavRepeatDelay)
                {
                    _navFreshPress = false;
                    _navRepeatTimer = 0f;
                    MoveSelection(dx, dy);
                }
            }
        }

        private void MoveSelection(int dx, int dy)
        {
            int columns = Mathf.Max(1, gridColumns);
            int row = _selectedIndex >= 0 ? _selectedIndex / columns : 0;
            int col = _selectedIndex >= 0 ? _selectedIndex % columns : 0;

            if (dx != 0)
            {
                col = Mathf.Clamp(col + dx, 0, columns - 1);
            }
            if (dy != 0)
            {
                row = Mathf.Clamp(row - dy, 0, (_visibleSkills.Count - 1) / columns);
            }

            SelectIndex(row * columns + col);
        }

        // ───────────────────── HUD visibility ─────────────────────

        /// <summary>
        /// Hides the gameplay HUD while the shop is open. Only sets the flag when
        /// the HUD was actually active, so closing the shop restores it without
        /// forcing it on if it was hidden for another reason.
        /// </summary>
        private void HideHud()
        {
            if (_hudHidden) return;

            GameObject hud = GetHud();
            if (hud != null && hud.activeSelf)
            {
                _hudHidden = true;
                hud.SetActive(false);
            }
        }

        /// <summary>
        /// Restores the HUD if the shop hid it.
        /// </summary>
        private void ShowHud()
        {
            if (!_hudHidden) return;

            GameObject hud = GetHud();
            if (hud != null)
            {
                hud.SetActive(true);
            }

            _hudHidden = false;
        }

        /// <summary>
        /// Returns the gameplay HUD GameObject: the one bound to GUIManager, or a
        /// scene object named "HUD" as a fallback (same lookup BootstrapSceneLoader uses).
        /// </summary>
        private static GameObject GetHud()
        {
            if (GUIManager.HasInstance && GUIManager.Instance.HUD != null)
            {
                return GUIManager.Instance.HUD;
            }

            GameObject[] objects = FindObjectsByType<GameObject>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (GameObject target in objects)
            {
                if (target != null && target.name == "HUD")
                {
                    return target;
                }
            }

            return null;
        }

        // ───────────────────── Refresh ─────────────────────

        private void RefreshAll()
        {
            RefreshSlots();
            RefreshGrid();
            RefreshDetail();
            RefreshSoul();
        }

        private void RefreshSoul()
        {
            if (soulCountText != null)
            {
                soulCountText.text = SoulWallet.CurrentSoul.ToString();
            }
        }

        private void RefreshSlots()
        {
            if (_slotManager == null) return;

            // Fill active skill slots (first 3 slots)
            for (int i = 0; i < activeSlotIcons.Length; i++)
            {
                Skill skill = _slotManager.GetSkill(i);
                SetSlotIcon(activeSlotIcons[i], skill);
            }

            // Fill passive skill slots (next 3 slots, or all if fewer active)
            int passiveOffset = Mathf.Min(_slotManager.SlotCount, 3);
            for (int i = 0; i < passiveSlotIcons.Length; i++)
            {
                int slotIdx = passiveOffset + i;
                Skill skill = _slotManager.GetSkill(slotIdx);
                SetSlotIcon(passiveSlotIcons[i], skill);
            }
        }

        private static void SetSlotIcon(Image icon, Skill skill)
        {
            if (icon == null) return;

            icon.sprite = skill != null ? skill.Icon : null;
            icon.color = skill != null && skill.Icon != null ? Color.white : new Color(0.15f, 0.1f, 0.2f, 0.8f);
        }

        private void RefreshGrid()
        {
            // Clear old entries
            foreach (GameObject entry in _skillEntries)
            {
                if (entry != null) Destroy(entry);
            }
            _skillEntries.Clear();
            _entryBgImages.Clear();
            _visibleSkills.Clear();
            _selectedIndex = -1;

            if (availableSkills == null || skillGridParent == null) return;

            foreach (ActiveSkillData skill in availableSkills)
            {
                if (skill == null) continue;

                GameObject entry = Instantiate(skillEntryPrefab, skillGridParent);
                entry.SetActive(true);

                // Set icon
                Image icon = entry.transform.Find("Icon")?.GetComponent<Image>();
                if (icon != null)
                {
                    icon.sprite = skill.Icon;
                    icon.enabled = skill.Icon != null;
                }

                // Cache the background image for selection tinting.
                Image bg = entry.GetComponent<Image>();
                if (bg != null)
                {
                    bg.color = _entryNormalColor;
                }

                _skillEntries.Add(entry);
                _entryBgImages.Add(bg);
                _visibleSkills.Add(skill);
            }

            // Select the first skill by default.
            SelectIndex(0);
        }

        private void SelectIndex(int index)
        {
            if (_visibleSkills.Count == 0) return;

            index = Mathf.Clamp(index, 0, _visibleSkills.Count - 1);
            if (_selectedIndex == index) return;

            _selectedIndex = index;
            _selectedSkill = _visibleSkills[index];
            RefreshDetail();
            UpdateSelectionHighlight();
        }

        private void UpdateSelectionHighlight()
        {
            // Tint the selected entry's background; reset the rest.
            for (int i = 0; i < _entryBgImages.Count; i++)
            {
                Image bg = _entryBgImages[i];
                if (bg == null) continue;

                bg.color = i == _selectedIndex ? _entrySelectedColor : _entryNormalColor;
            }
        }

        private void RefreshDetail()
        {
            if (_selectedSkill == null)
            {
                if (detailIcon != null) detailIcon.sprite = null;
                if (detailIcon != null) detailIcon.enabled = false;
                if (detailTypeText != null) detailTypeText.text = "";
                if (detailNameText != null) detailNameText.text = "SELECT A SKILL";
                if (detailDescText != null) detailDescText.text = "Pilih skill dari grid untuk melihat detail.";
                if (detailStatsText != null) detailStatsText.text = "";
                if (detailCostText != null) detailCostText.text = "—";
                if (buyButton != null)
                {
                    TextMeshProUGUI label = GetBuyPromptText();
                    if (label != null) label.text = "";
                }
                return;
            }

            if (detailIcon != null)
            {
                detailIcon.sprite = _selectedSkill.Icon;
                detailIcon.enabled = _selectedSkill.Icon != null;
            }

            if (detailTypeText != null)
            {
                detailTypeText.text = _selectedSkill.SkillType == SkillType.Passive
                    ? "PASSIVE SKILL"
                    : "ACTIVE SKILL";
                detailTypeText.color = _selectedSkill.SkillType == SkillType.Passive
                    ? new Color(0.2f, 0.9f, 0.85f) : new Color(1f, 0.85f, 0.3f);
            }

            if (detailNameText != null)
            {
                detailNameText.text = _selectedSkill.DisplayName?.ToUpper() ?? "UNKNOWN";
            }

            if (detailDescText != null)
            {
                detailDescText.text = _selectedSkill.Description ?? "";
            }

            if (detailStatsText != null)
            {
                string stats = "";
                if (_selectedSkill.Damage > 0) stats += $"Damage: {_selectedSkill.Damage}\n";
                if (_selectedSkill.Cooldown > 0) stats += $"Cooldown: {_selectedSkill.Cooldown}s\n";
                if (_selectedSkill.Duration > 0) stats += $"Duration: {_selectedSkill.Duration}s";
                detailStatsText.text = stats.TrimEnd('\n');
            }

            if (detailCostText != null)
            {
                if (_selectedSkill.SoulPrice <= 0)
                {
                    detailCostText.text = "FREE";
                    detailCostText.color = new Color(0.3f, 0.85f, 0.4f);
                }
                else
                {
                    detailCostText.text = _selectedSkill.SoulPrice.ToString();
                    detailCostText.color = SoulWallet.CanSpend(_selectedSkill.SoulPrice)
                        ? new Color(1f, 0.85f, 0.3f) : new Color(0.85f, 0.3f, 0.3f);
                }
            }

            if (buyButton != null)
            {
                bool canBuy = _selectedSkill.SoulPrice <= 0 || SoulWallet.CanSpend(_selectedSkill.SoulPrice);

                // Hide the button's visual so it reads as a text prompt.
                Image btnImage = buyButton.GetComponent<Image>();
                if (btnImage != null)
                {
                    Color c = btnImage.color;
                    c.a = 0f;
                    btnImage.color = c;
                }

                TextMeshProUGUI label = GetBuyPromptText();
                if (label != null)
                {
                    label.text = canBuy ? "PRESS F TO BUY" : "NOT ENOUGH SOUL";
                    label.color = canBuy
                        ? new Color(0.3f, 0.85f, 0.4f)
                        : new Color(0.85f, 0.3f, 0.3f);
                }
            }
        }

        /// <summary>
        /// Locates the TextMeshPro label used as the buy prompt. It is cached
        /// after the first lookup. Looks on the buy button itself first, then
        /// searches its children.
        /// </summary>
        private TextMeshProUGUI GetBuyPromptText()
        {
            if (_buyPromptResolved)
            {
                return _buyPromptText;
            }

            _buyPromptResolved = true;
            _buyPromptText = null;

            if (buyButton != null)
            {
                _buyPromptText = buyButton.GetComponent<TextMeshProUGUI>();
                if (_buyPromptText == null)
                {
                    _buyPromptText = buyButton.GetComponentInChildren<TextMeshProUGUI>(true);
                }
            }

            return _buyPromptText;
        }

        // ───────────────────── Buy Logic ─────────────────────

        private void TryBuySelected()
        {
            if (_selectedSkill == null) return;

            int price = _selectedSkill.SoulPrice;
            if (price > 0 && !SoulWallet.CanSpend(price)) return;

            // Deduct soul
            if (price > 0)
            {
                SoulWallet.Spend(price);
            }

            // Find first empty slot and equip
            if (_slotManager != null)
            {
                for (int i = 0; i < _slotManager.SlotCount; i++)
                {
                    if (_slotManager.GetSkill(i) == null)
                    {
                        _slotManager.Equip(i, _selectedSkill);
                        break;
                    }
                }
            }

            // Refresh everything
            RefreshAll();
        }

        private void OnDestroy()
        {
            if (_isOpen)
            {
                Time.timeScale = 1f;
            }
        }
    }
}
