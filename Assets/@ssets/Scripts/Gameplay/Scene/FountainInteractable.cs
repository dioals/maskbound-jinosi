using System.Collections;
using MaskboundJinosi.Combat;
using MaskboundJinosi.Skills;
using MaskboundJinosi.UI;
using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using UnityEngine;

namespace MaskboundJinosi.Gameplay.Scene
{
    /// <summary>
    /// A fountain that the player can interact with.
    /// On interact: triggers meditation pose, disables player input, opens SkillShopPanel.
    /// On close: stops meditation, restores player input.
    /// </summary>
    [AddComponentMenu("Maskbound/Gameplay/Fountain Interactable")]
    public class FountainInteractable : MonoBehaviour
    {
        [Header("Prompt")]
        [SerializeField] private GameObject interactionPromptPrefab;
        [SerializeField] private Vector3 promptOffset = new Vector3(0f, 2f, 0f);
        [SerializeField] private float promptScale = 0.015f;

        [Header("Shop")]
        [SerializeField] private SkillShopPanel skillShopPanel;
        [SerializeField] private SkillSlotManager skillSlotManager;

        private Character _nearbyCharacter;
        private CharacterMeditation _meditationAbility;
        private CharacterAbility[] _allAbilities;
        private bool[] _abilityPreviousStates;
        private GameObject _runtimePrompt;
        private bool _shopOpen;

        private void Update()
        {
            if (_shopOpen || _nearbyCharacter == null || _nearbyCharacter.LinkedInputManager == null)
            {
                return;
            }

            if (_nearbyCharacter.LinkedInputManager.InteractButton.State.CurrentState == MMInput.ButtonStates.ButtonDown)
            {
                OpenShop();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Character character = other.GetComponent<Character>();
            if (character == null || !other.CompareTag("Player"))
            {
                return;
            }

            _nearbyCharacter = character;
            ShowPrompt();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            Character character = other.GetComponent<Character>();
            if (character == null || character != _nearbyCharacter)
            {
                return;
            }

            _nearbyCharacter = null;
            HidePrompt();

            if (_shopOpen)
            {
                CloseShop();
            }
        }

        private void OpenShop()
        {
            if (_shopOpen)
            {
                return;
            }

            _shopOpen = true;
            HidePrompt();

            // Find meditation ability and all abilities
            _meditationAbility = _nearbyCharacter.GetComponentInChildren<CharacterMeditation>(true);
            _allAbilities = _nearbyCharacter.GetComponentsInChildren<CharacterAbility>(true);
            _abilityPreviousStates = new bool[_allAbilities.Length];

            // Save current states and disable all abilities
            for (int i = 0; i < _allAbilities.Length; i++)
            {
                _abilityPreviousStates[i] = _allAbilities[i].AbilityPermitted;
                _allAbilities[i].PermitAbility(false);
            }

            // Stop horizontal force
            CorgiController controller = _nearbyCharacter.GetComponent<CorgiController>();
            if (controller != null)
            {
                controller.SetHorizontalForce(0f);
            }

            // Trigger meditation pose
            if (_meditationAbility != null)
            {
                _meditationAbility.PermitAbility(true);
                _meditationAbility.MeditateStart();
            }

            // Find skill slot manager if not assigned
            if (skillSlotManager == null)
            {
                skillSlotManager = _nearbyCharacter.GetComponentInChildren<SkillSlotManager>(true);
            }

            // Find the shop panel if not assigned (it may live in the Bootstrap scene)
            if (skillShopPanel == null)
            {
                skillShopPanel = Object.FindFirstObjectByType<SkillShopPanel>(FindObjectsInactive.Include);
            }

            // Open the shop panel
            if (skillShopPanel != null)
            {
                skillShopPanel.Open(skillSlotManager, OnShopClosed);
            }
        }

        private void CloseShop()
        {
            if (!_shopOpen)
            {
                return;
            }

            _shopOpen = false;

            // Stop meditation
            if (_meditationAbility != null)
            {
                _meditationAbility.MeditateStop();
                _meditationAbility.PermitAbility(false);
            }

            // Restore abilities
            for (int i = 0; i < _allAbilities.Length; i++)
            {
                _allAbilities[i].PermitAbility(_abilityPreviousStates[i]);
            }

            // Close panel
            if (skillShopPanel != null)
            {
                skillShopPanel.Close();
            }

            ShowPrompt();
        }

        private void OnShopClosed()
        {
            CloseShop();
        }

        private void ShowPrompt()
        {
            if (_runtimePrompt == null && interactionPromptPrefab != null)
            {
                _runtimePrompt = Instantiate(interactionPromptPrefab, transform, false);
                _runtimePrompt.transform.localPosition = promptOffset;
                _runtimePrompt.transform.localScale = Vector3.one * promptScale;
            }

            if (_runtimePrompt != null)
            {
                _runtimePrompt.SetActive(true);
            }
        }

        private void HidePrompt()
        {
            if (_runtimePrompt != null)
            {
                _runtimePrompt.SetActive(false);
            }
        }

        private void OnValidate()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }
    }
}
