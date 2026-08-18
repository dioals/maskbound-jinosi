using System.Collections;
using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MaskboundJinosi.Gameplay.Scene
{
    /// <summary>
    /// An interactable doorway that moves the player to another scene.
    /// Input is handled by CharacterButtonActivation, so the same Interact action works
    /// with keyboard, gamepad/joystick, and the Corgi mobile interact button.
    /// </summary>
    [AddComponentMenu("Maskbound/Scene/Map Transition Door")]
    public class MapTransitionDoor : ButtonActivated
    {
        [Header("Map Destination")]
        [Tooltip("Exact scene name. The scene must be enabled in Build Settings.")]
        [SerializeField] private string destinationScene;
        [SerializeField, Min(0f)] private float transitionDelay;
        [SerializeField] private bool useBootstrapLoadingScreen = true;
        [SerializeField] private string loadingSceneName = "LoadingScreen";

        [Header("Interaction Prompt")]
        [Tooltip("Optional prefab shown above the door. Its text auto-adapts to the bound Interact key (e.g. InteractionPrompt.prefab). Falls back to the built-in promptText when empty.")]
        [SerializeField] private GameObject interactionPromptPrefab;
        [SerializeField] private string promptText = "INTERACT";
        [SerializeField] private Vector3 promptOffset = new Vector3(0f, 1.25f, 0f);
        [SerializeField, Min(0.1f)] private float promptScale = 0.015f;
        [SerializeField] private Color promptColor = Color.white;
        [SerializeField] private Color promptBackgroundColor = new Color(0f, 0f, 0f, 0.75f);

        private GameObject _runtimePrompt;
        private bool _isTransitioning;
        private Character _nearbyCharacter;

        public string DestinationScene
        {
            get => destinationScene;
            set => destinationScene = value;
        }

        public override void Initialization()
        {
            // This component provides its own prompt, so no ButtonPrompt prefab is required.
            UseVisualPrompt = true;
            AlwaysShowPrompt = false;
            ShowPromptWhenColliding = true;
            InputType = InputTypes.Default;
            ButtonActivatedRequirement = ButtonActivatedRequirements.Character;
            RequiresPlayerType = true;
            // Polling the character's linked InputManager below also makes the door work
            // on player prefabs that don't yet contain CharacterButtonActivation.
            RequiresButtonActivationAbility = false;
            // The door drives its own input and prompt, so it must not mutate the player's
            // CharacterButtonActivation state. Doing so sets InButtonActivatedZone, which
            // makes CharacterJump block jumping anywhere near the door.
            ShouldUpdateState = false;
            base.Initialization();
            CreateRuntimePrompt();
            SetRuntimePromptVisible(false);
        }

        private void Update()
        {
            if (_isTransitioning || _nearbyCharacter == null || _nearbyCharacter.LinkedInputManager == null)
            {
                return;
            }

            if (_nearbyCharacter.LinkedInputManager.InteractButton.State.CurrentState == MMInput.ButtonStates.ButtonDown)
            {
                TriggerButtonAction(_nearbyCharacter.gameObject);
            }
        }

        protected override void TriggerEnter(GameObject enteringObject)
        {
            base.TriggerEnter(enteringObject);
            if (_currentCharacter != null)
            {
                _nearbyCharacter = _currentCharacter;
            }
        }

        protected override void TriggerExit(GameObject exitingObject)
        {
            Character exitingCharacter = exitingObject.GetComponent<Character>();
            base.TriggerExit(exitingObject);
            if (exitingCharacter != null && exitingCharacter == _nearbyCharacter)
            {
                _nearbyCharacter = null;
                // The base class only hides its own Corgi ButtonPrompt, which this
                // door never creates, so we hide our runtime prompt ourselves.
                HidePrompt();
            }
        }

        public override void TriggerButtonAction(GameObject instigator)
        {
            if (_isTransitioning || !IsDestinationValid())
            {
                return;
            }

            if (!CheckNumberOfUses())
            {
                PromptError();
                return;
            }

            base.TriggerButtonAction(instigator);
            _isTransitioning = true;
            HidePrompt();
            StartCoroutine(LoadDestination());
        }

        public override void ShowPrompt()
        {
            CreateRuntimePrompt();
            SetRuntimePromptVisible(!_isTransitioning);
        }

        public override void HidePrompt()
        {
            SetRuntimePromptVisible(false);
        }

        private IEnumerator LoadDestination()
        {
            if (transitionDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(transitionDelay);
            }

            Time.timeScale = 1f;

            BootstrapSceneLoader loader = FindFirstObjectByType<BootstrapSceneLoader>(FindObjectsInactive.Include);
            if (useBootstrapLoadingScreen && loader != null)
            {
                loader.LoadLevel(destinationScene);
                yield break;
            }

            if (useBootstrapLoadingScreen && Application.CanStreamedLevelBeLoaded(loadingSceneName))
            {
                MMSceneLoadingManager.LoadScene(destinationScene, loadingSceneName);
                yield break;
            }

            SceneManager.LoadScene(destinationScene);
        }

        private bool IsDestinationValid()
        {
            if (string.IsNullOrWhiteSpace(destinationScene))
            {
                Debug.LogWarning("MapTransitionDoor has no Destination Scene assigned.", this);
                return false;
            }

            if (!Application.CanStreamedLevelBeLoaded(destinationScene))
            {
                Debug.LogError($"Scene '{destinationScene}' is not available. Add it to Build Settings first.", this);
                return false;
            }

            return true;
        }

        private void CreateRuntimePrompt()
        {
            if (_runtimePrompt != null)
            {
                return;
            }

            if (interactionPromptPrefab != null)
            {
                _runtimePrompt = Instantiate(interactionPromptPrefab, transform, false);
                _runtimePrompt.transform.localPosition = promptOffset;
                _runtimePrompt.transform.localScale = Vector3.one * promptScale;
                return;
            }

            _runtimePrompt = new GameObject("Interaction Prompt", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            _runtimePrompt.transform.SetParent(transform, false);
            _runtimePrompt.transform.localPosition = promptOffset;
            _runtimePrompt.transform.localScale = Vector3.one * promptScale;

            Canvas canvas = _runtimePrompt.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;

            RectTransform canvasRect = _runtimePrompt.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(240f, 64f);

            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            background.transform.SetParent(_runtimePrompt.transform, false);
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            background.GetComponent<UnityEngine.UI.Image>().color = promptBackgroundColor;

            GameObject label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(background.transform, false);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10f, 4f);
            labelRect.offsetMax = new Vector2(-10f, -4f);

            TextMeshProUGUI text = label.GetComponent<TextMeshProUGUI>();
            text.text = promptText;
            text.color = promptColor;
            text.fontSize = 30f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;
        }

        private void SetRuntimePromptVisible(bool visible)
        {
            if (_runtimePrompt != null)
            {
                _runtimePrompt.SetActive(visible);
            }
        }

        private void OnValidate()
        {
            Collider2D zone = GetComponent<Collider2D>();
            if (zone != null)
            {
                zone.isTrigger = true;
            }
        }
    }
}
