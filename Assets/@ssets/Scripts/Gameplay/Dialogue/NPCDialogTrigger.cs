using System.Collections;
using MoreMountains.CorgiEngine;
using Unity.Cinemachine;
using UnityEngine;
using Flowchart = Fungus.Flowchart;

namespace MaskboundJinosi.Gameplay.Dialogue
{
    /// <summary>
    /// Fires when the player first enters a position: freezes the player, moves a
    /// cinematic camera in, fades the NPC in, then plays a Fungus dialog.
    /// Only plays once per save (tracked with PlayerPrefs).
    /// </summary>
    [AddComponentMenu("Maskbound/Dialogue/NPC Dialog Trigger")]
    [RequireComponent(typeof(BoxCollider2D))]
    public class NPCDialogTrigger : MonoBehaviour
    {
        [Header("NPC")]
        [Tooltip("NPC prefab to spawn when the trigger fires. Assign the NPC art here.")]
        [SerializeField] private GameObject npcPrefab;
        [Tooltip("Transform where the NPC spawns (only its position is used). Leave empty to spawn at this trigger's position.")]
        [SerializeField] private Transform npcSpawnPoint;
        [Tooltip("Destroy the spawned NPC after the dialog ends.")]
        [SerializeField] private bool destroyNpcAfterDialog;
        [Tooltip("Animator trigger played when the NPC appears (e.g. 'Appear'). Empty or missing parameter = fall back to alpha fade-in.")]
        [SerializeField] private string appearAnimatorParameter = "Appear";
        [Tooltip("Animator trigger played after the dialog ends (e.g. 'Disappear'). Empty or missing parameter = fall back to alpha fade-out.")]
        [SerializeField] private string disappearAnimatorParameter = "Disappear";
        [Tooltip("Seconds to wait after the disappear trigger before the NPC is destroyed - time for its disappear animation to play.")]
        [SerializeField] private float disappearWaitDuration = 1f;

        [Header("Dialog (Fungus)")]
        [Tooltip("Fungus Flowchart prefab holding the dialog. If empty, the first Flowchart in the scene is used.")]
        [SerializeField] private Flowchart flowchartPrefab;
        [Tooltip("Name of the block to execute on the Flowchart.")]
        [SerializeField] private string blockName = "IntroDialog";
        [Tooltip("PlayerPrefs key used so the dialog only plays once.")]
        [SerializeField] private string saveFlagKey = "Maskbound.IntroDialogShown";

        [Header("Sequence")]
        [Tooltip("How long the player stands frozen (camera settling) before the NPC appears.")]
        [SerializeField] private float cameraDelay = 0.6f;
        [Tooltip("Fallback fade-in duration when the NPC has no animator trigger, or the wait after the 'Appear' trigger fires.")]
        [SerializeField] private float npcFadeInDuration = 0.8f;
        [Tooltip("Fallback fade-out duration when the NPC has no animator trigger.")]
        [SerializeField] private float npcFadeOutDuration = 0.5f;
        [Tooltip("Pause between the NPC fully appearing and the dialog starting.")]
        [SerializeField] private float dialogDelay = 0.3f;

        [Header("Player")]
        [Tooltip("Freeze the player and force the idle animation while the sequence/dialog is playing.")]
        [SerializeField] private bool freezePlayerDuringDialog = true;

        [Header("HUD")]
        [Tooltip("Hide the gameplay HUD while the dialog sequence plays and restore it when it ends.")]
        [SerializeField] private bool hideHudDuringDialog = true;

        [Header("Camera (cinematic)")]
        [Tooltip("Create a temporary Cinemachine camera focused on the NPC during the sequence.")]
        [SerializeField] private bool useCinematicCamera = true;
        [Tooltip("Where the cinematic camera sits, relative to the NPC spawn point.")]
        [SerializeField] private Vector2 cameraOffset = new Vector2(0f, 1.5f);
        [Tooltip("Orthographic size of the cinematic camera (smaller = zoomed in, larger = zoomed out). Gameplay camera uses 10.")]
        [SerializeField] private float cameraOrthographicSize = 9f;
        [Tooltip("Camera priority while active - must be higher than the gameplay camera.")]
        [SerializeField] private int cameraPriority = 20;

        [Header("Testing")]
        [Tooltip("Editor testing: start the sequence shortly after the scene loads, without walking into the trigger. This bypasses the once-only flag.")]
        [SerializeField] private bool startOnLoadForTesting;
        [Tooltip("Delay before 'Start On Load For Testing' fires.")]
        [SerializeField] private float startOnLoadDelay = 1.5f;

        private GameObject _npcInstance;
        private SpriteRenderer _npcRenderer;
        private Animator _npcAnimator;
        private Flowchart _flowchartInstance;
        private CinemachineCamera _cinematicCamera;
        private Character _player;
        private CharacterAbility[] _playerAbilities;
        private bool[] _playerAbilitiesEnabledState;
        private bool _sequenceStarted;
        private bool _sequenceFinished;
        private bool _dialogExecuting;
        private bool _hudHidden;
        private float _targetCameraX;

        protected virtual void Start()
        {
            if (startOnLoadForTesting)
            {
                StartCoroutine(StartOnLoadRoutine());
            }
        }

        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            TryStartSequence(other);
        }

        protected virtual void OnTriggerStay2D(Collider2D other)
        {
            // Safety net: if Enter was missed (e.g. the player spawned inside the
            // trigger, or physics skipped the transition), start on Stay too.
            if (Time.timeSinceLevelLoad < 0.5f)
            {
                return;
            }

            TryStartSequence(other);
        }

        protected virtual void Update()
        {
            // While the sequence runs, keep the player locked to its idle animation.
            // This runs in Update (not LateUpdate) so the values are set before the
            // Animator evaluates them - LateUpdate writes happen after the Animator
            // update and get overwritten by Character.Update the next frame.
            if (_sequenceStarted && !_sequenceFinished && freezePlayerDuringDialog && _player != null)
            {
                ForcePlayerIdle();
            }

            if (!_dialogExecuting || _flowchartInstance == null)
            {
                return;
            }

            // Dialog is finished once no blocks are executing anymore.
            if (!_flowchartInstance.HasExecutingBlocks())
            {
                EndSequence();
            }
        }

        /// <summary>
        /// Starts the sequence if this collider belongs to the player and the
        /// dialog hasn't been shown yet.
        /// </summary>
        protected virtual void TryStartSequence(Collider2D other)
        {
            if (_sequenceStarted || _sequenceFinished)
            {
                return;
            }

            Character character = GetPlayerCharacter(other);
            if (character == null)
            {
                return;
            }

            if (PlayerPrefs.GetInt(saveFlagKey, 0) == 1)
            {
                Debug.Log("[NPCDialogTrigger] Flag '" + saveFlagKey + "' already set, skipping dialog.", this);
                _sequenceFinished = true;
                return;
            }

            StartSequence(character);
        }

        protected virtual IEnumerator StartOnLoadRoutine()
        {
            yield return new WaitForSeconds(startOnLoadDelay);

            if (_sequenceStarted || _sequenceFinished)
            {
                yield break;
            }

            Character player = GetMainPlayer();
            if (player == null)
            {
                Debug.LogWarning("[NPCDialogTrigger] Start On Load: no player found after " + startOnLoadDelay + "s.", this);
                yield break;
            }

            Debug.Log("[NPCDialogTrigger] Start On Load (testing) firing.", this);
            StartSequence(player);
        }

        protected virtual Character GetMainPlayer()
        {
            if (LevelManager.HasInstance && LevelManager.Instance.Players != null && LevelManager.Instance.Players.Count > 0)
            {
                return LevelManager.Instance.Players[0];
            }

            return null;
        }

        protected virtual Character GetPlayerCharacter(Collider2D other)
        {
            Character character = other.GetComponentInParent<Character>();
            if (character != null)
            {
                return character;
            }

            if (other.CompareTag("Player"))
            {
                Debug.Log("[NPCDialogTrigger] Collider '" + other.name + "' has no Character, matched player by tag.", this);
                character = GetMainPlayer();
            }

            return character;
        }

        /// <summary>
        /// Freezes the player, spawns the NPC, starts the cinematic camera and runs
        /// the timed sequence: delay -> NPC fade-in -> dialog.
        /// </summary>
        protected virtual void StartSequence(Character player)
        {
            _sequenceStarted = true;
            _player = player;

            Debug.Log("[NPCDialogTrigger] Player entered trigger at " + transform.position + ", starting sequence.", this);

            DisableMovementAbility();
            HideHud();
            ResolveFlowchart();
            CreateCinematicCamera();

            if (_flowchartInstance == null)
            {
                Debug.LogError("[NPCDialogTrigger] No Flowchart found to execute the dialog. Assign 'Flowchart Prefab' in the Inspector.", this);
                EndSequence();
                return;
            }

            StartCoroutine(SequenceRoutine());
        }

        protected virtual IEnumerator SequenceRoutine()
        {
            // 0. Wait for the player to land on the ground if they are airborne
            //    (e.g. they entered the trigger while jumping or falling).
            yield return new WaitUntil(() => _player == null || !_player.Airborne);

            // Force idle now that the player has landed.
            ForcePlayerIdle();

            // 1. Camera pans horizontally to the spawn point while the player stands idle.
            if (useCinematicCamera && _cinematicCamera != null)
            {
                yield return StartCoroutine(PanCameraToTarget());
            }
            else
            {
                yield return new WaitForSeconds(cameraDelay);
            }

            // 2. Spawn NPC after the camera has finished focusing.
            SpawnNpc();
            if (_cinematicCamera != null && _npcInstance != null)
            {
                _cinematicCamera.LookAt = _npcInstance.transform;
            }

            // 3. NPC appears (animator trigger, or fade-in fallback).
            yield return StartCoroutine(ShowNpc());

            // 4. Short beat, then the dialog starts.
            yield return new WaitForSeconds(dialogDelay);

            Debug.Log("[NPCDialogTrigger] Executing block '" + blockName + "' on flowchart '" + _flowchartInstance.name + "'.", this);
            _dialogExecuting = true;
            _flowchartInstance.ExecuteBlock(blockName);
        }

        /// <summary>
        /// Smoothly pans the cinematic camera horizontally from its current X to the
        /// target X (spawn point + offset), keeping Y and Z constant.
        /// </summary>
        protected virtual IEnumerator PanCameraToTarget()
        {
            if (_cinematicCamera == null)
            {
                yield break;
            }

            float startX = _cinematicCamera.transform.position.x;
            float y = _cinematicCamera.transform.position.y;
            float z = _cinematicCamera.transform.position.z;
            float elapsed = 0f;

            while (elapsed < cameraDelay)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / Mathf.Max(0.01f, cameraDelay)));
                float x = Mathf.Lerp(startX, _targetCameraX, t);
                _cinematicCamera.transform.position = new Vector3(x, y, z);
                yield return null;
            }

            // Ensure final position is exact.
            _cinematicCamera.transform.position = new Vector3(_targetCameraX, y, z);
        }

        /// <summary>
        /// Makes the NPC appear: fires the 'Appear' animator trigger when the NPC has
        /// an Animator with that parameter, otherwise falls back to an alpha fade-in.
        /// </summary>
        protected virtual IEnumerator ShowNpc()
        {
            if (_npcInstance == null)
            {
                yield break;
            }

            if (TrySetAnimatorTrigger(_npcAnimator, appearAnimatorParameter))
            {
                // Give the appear animation time to play before the dialog starts.
                yield return new WaitForSeconds(npcFadeInDuration);
                yield break;
            }

            yield return StartCoroutine(FadeNpcIn());
        }

        protected virtual IEnumerator FadeNpcIn()
        {
            if (_npcRenderer == null)
            {
                yield break;
            }

            _npcRenderer.color = new Color(1f, 1f, 1f, 0f);

            float elapsed = 0f;
            while (elapsed < npcFadeInDuration)
            {
                elapsed += Time.deltaTime;
                _npcRenderer.color = new Color(1f, 1f, 1f, Mathf.Clamp01(elapsed / Mathf.Max(0.01f, npcFadeInDuration)));
                yield return null;
            }

            _npcRenderer.color = Color.white;
        }

        protected virtual void SpawnNpc()
        {
            if (npcPrefab == null)
            {
                Debug.LogWarning("[NPCDialogTrigger] 'Npc Prefab' is empty, no NPC will be spawned.", this);
                return;
            }

            Vector3 spawnPosition = npcSpawnPoint != null ? npcSpawnPoint.position : transform.position;
            _npcInstance = Instantiate(npcPrefab, spawnPosition, Quaternion.identity);
            _npcRenderer = _npcInstance.GetComponentInChildren<SpriteRenderer>();
            _npcAnimator = _npcInstance.GetComponentInChildren<Animator>();
            Debug.Log("[NPCDialogTrigger] Spawned NPC '" + _npcInstance.name + "' at " + _npcInstance.transform.position + ".", this);
            EnsureNpcVisible();
        }

        protected virtual void ResolveFlowchart()
        {
            if (flowchartPrefab != null)
            {
                _flowchartInstance = Instantiate(flowchartPrefab).GetComponent<Flowchart>();
            }
            else
            {
                _flowchartInstance = FindFirstObjectByType<Flowchart>();
            }
        }

        protected virtual void CreateCinematicCamera()
        {
            if (!useCinematicCamera)
            {
                return;
            }

            GameObject go = new GameObject("NPCDialog_CinematicCamera");
            _cinematicCamera = go.AddComponent<CinemachineCamera>();
            _cinematicCamera.Priority.Value = cameraPriority;
            _cinematicCamera.OutputChannel = OutputChannels.Default;

            // Keep the same depth as the gameplay camera (usually far behind the world,
            // e.g. z = -15) or the view ends up inside the geometry and shows a white screen.
            float cameraZ = Camera.main != null ? Camera.main.transform.position.z : -15f;

            // Start at the gameplay camera's current position so CinemachineBrain
            // blends smoothly from the gameplay camera to this one.
            float startX = Camera.main != null ? Camera.main.transform.position.x : 0f;
            float cameraY = Camera.main != null ? Camera.main.transform.position.y : 0f;
            _cinematicCamera.transform.position = new Vector3(startX, cameraY, cameraZ);

            // Calculate the target X (spawn point + offset) for the horizontal pan.
            Vector3 spawnPos = npcSpawnPoint != null ? npcSpawnPoint.position : transform.position;
            _targetCameraX = spawnPos.x + cameraOffset.x;

            _cinematicCamera.Lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
            _cinematicCamera.Lens.OrthographicSize = cameraOrthographicSize;

            Debug.Log("[NPCDialogTrigger] Cinematic camera created at " + _cinematicCamera.transform.position + ", target X: " + _targetCameraX + ".", this);
        }

        protected virtual void DestroyCinematicCamera()
        {
            if (_cinematicCamera != null)
            {
                Destroy(_cinematicCamera.gameObject);
                _cinematicCamera = null;
            }
        }

        protected virtual void FreezePlayer()
        {
            if (!freezePlayerDuringDialog || _player == null)
            {
                return;
            }

            // Disable input abilities so no new movement commands are accepted,
            // but keep gravity active so the player finishes their current action
            // (e.g. falling to the ground) before the dialog sequence starts.
            DisableMovementAbility();
        }

        /// <summary>
        /// Disables ALL player abilities (movement, jump, attack, skill, etc.) so no
        /// input is processed during the dialog. Also stops horizontal velocity.
        /// The enabled state of each ability is saved so it can be restored exactly.
        /// </summary>
        protected virtual void DisableMovementAbility()
        {
            if (_player == null)
            {
                return;
            }

            _playerAbilities = _player.GetComponents<CharacterAbility>();
            _playerAbilitiesEnabledState = new bool[_playerAbilities.Length];

            for (int i = 0; i < _playerAbilities.Length; i++)
            {
                _playerAbilitiesEnabledState[i] = _playerAbilities[i].enabled;
                _playerAbilities[i].enabled = false;
            }

            // Stop horizontal velocity so the player doesn't slide when the
            // dialog trigger fires (e.g. entering while jumping forward).
            CorgiController controller = _player.GetComponent<CorgiController>();
            if (controller != null)
            {
                controller.SetHorizontalForce(0f);
            }
        }

        /// <summary>
        /// Re-enables all player abilities that were active before the dialog started.
        /// </summary>
        protected virtual void RestoreMovementAbility()
        {
            if (_playerAbilities == null || _playerAbilitiesEnabledState == null)
            {
                return;
            }

            for (int i = 0; i < _playerAbilities.Length; i++)
            {
                _playerAbilities[i].enabled = _playerAbilitiesEnabledState[i];
            }

            _playerAbilities = null;
            _playerAbilitiesEnabledState = null;
        }

        /// <summary>
        /// Pushes the player's animator back to its idle state, even if the player
        /// is still holding a movement input during the dialog.
        /// </summary>
        protected virtual void ForcePlayerIdle()
        {
            if (_player == null)
            {
                return;
            }

            CharacterStates.MovementStates state = _player.MovementState.CurrentState;
            if (state == CharacterStates.MovementStates.Walking
                || state == CharacterStates.MovementStates.Running
                || state == CharacterStates.MovementStates.Dashing)
            {
                _player.MovementState.ChangeState(CharacterStates.MovementStates.Idle);
            }

            Animator animator = _player.CharacterAnimator;
            if (animator != null)
            {
                animator.SetBool("Idle", true);
                animator.SetBool("Walking", false);
                animator.SetBool("Running", false);
                animator.SetBool("Dashing", false);
                animator.SetFloat("Speed", 0f);
                animator.SetFloat("xSpeed", 0f);
                animator.SetFloat("ySpeed", 0f);
            }
        }

        protected virtual void UnfreezePlayer()
        {
            if (!freezePlayerDuringDialog || _player == null)
            {
                return;
            }

            RestoreMovementAbility();
        }

        /// <summary>
        /// Hides the gameplay HUD while the dialog sequence is playing. Only sets
        /// the flag when the HUD was actually active, so the sequence can restore
        /// it later without forcing it on if it was hidden for another reason.
        /// </summary>
        protected virtual void HideHud()
        {
            if (!hideHudDuringDialog || _hudHidden)
            {
                return;
            }

            GameObject hud = GetHud();
            if (hud != null && hud.activeSelf)
            {
                _hudHidden = true;
                hud.SetActive(false);
            }
        }

        /// <summary>
        /// Restores the HUD if this sequence hid it.
        /// </summary>
        protected virtual void ShowHud()
        {
            if (!_hudHidden)
            {
                return;
            }

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
        protected virtual GameObject GetHud()
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

        protected virtual void EndSequence()
        {
            if (_sequenceFinished)
            {
                return;
            }

            _sequenceFinished = true;
            _dialogExecuting = false;

            StartCoroutine(EndSequenceRoutine());
        }

        /// <summary>
        /// Finishes the sequence in order: the NPC fades out and disappears first,
        /// then the cinematic camera returns to the gameplay camera, and only then
        /// is the player freed again.
        /// </summary>
        protected virtual IEnumerator EndSequenceRoutine()
        {
            // 1. Brahmana disappears (animator trigger, or fade-out fallback) while
            //    the camera still holds on him.
            if (destroyNpcAfterDialog && _npcInstance != null)
            {
                yield return StartCoroutine(HideNpc());
                Destroy(_npcInstance);
                _npcInstance = null;
                _npcRenderer = null;
                _npcAnimator = null;
            }

            // 2. Only now the cinematic camera is removed and the gameplay camera takes over.
            DestroyCinematicCamera();

            // 3. Player is free to move again.
            UnfreezePlayer();

            // 4. HUD comes back now that the dialog is over.
            ShowHud();

            // 5. Remember this dialog was already shown.
            PlayerPrefs.SetInt(saveFlagKey, 1);
            PlayerPrefs.Save();

            Debug.Log("[NPCDialogTrigger] Sequence finished, flag '" + saveFlagKey + "' set.", this);
        }

        /// <summary>
        /// Makes the NPC disappear: fires the 'Disappear' animator trigger when the NPC
        /// has an Animator with that parameter, waits for its animation, otherwise
        /// falls back to an alpha fade-out.
        /// </summary>
        protected virtual IEnumerator HideNpc()
        {
            if (_npcInstance == null)
            {
                yield break;
            }

            if (TrySetAnimatorTrigger(_npcAnimator, disappearAnimatorParameter))
            {
                // Give the disappear animation time to play before the NPC is removed.
                yield return new WaitForSeconds(disappearWaitDuration);
                yield break;
            }

            yield return StartCoroutine(FadeNpcOut());
        }

        /// <summary>
        /// Fires an animator trigger, or logs a hint and returns false when the NPC
        /// has an Animator but not the requested parameter (caller falls back to fade).
        /// </summary>
        protected virtual bool TrySetAnimatorTrigger(Animator animator, string parameterName)
        {
            if (animator == null || string.IsNullOrEmpty(parameterName))
            {
                return false;
            }

            for (int i = 0; i < animator.parameterCount; i++)
            {
                if (animator.parameters[i].name == parameterName)
                {
                    animator.SetTrigger(parameterName);
                    return true;
                }
            }

            Debug.LogWarning("[NPCDialogTrigger] Animator of '" + _npcInstance.name + "' has no parameter '" + parameterName + "', falling back to alpha fade.", this);
            return false;
        }

        protected virtual IEnumerator FadeNpcOut()
        {
            if (_npcRenderer == null)
            {
                yield break;
            }

            Color startColor = _npcRenderer.color;

            float elapsed = 0f;
            while (elapsed < npcFadeOutDuration)
            {
                elapsed += Time.deltaTime;
                _npcRenderer.color = new Color(startColor.r, startColor.g, startColor.b,
                    Mathf.Clamp01(1f - elapsed / Mathf.Max(0.01f, npcFadeOutDuration)));
                yield return null;
            }

            _npcRenderer.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
        }

        /// <summary>
        /// If the NPC prefab has no sprite assigned yet, generates a small golden
        /// placeholder circle so the spawn is visible during development.
        /// </summary>
        protected virtual void EnsureNpcVisible()
        {
            if (_npcInstance == null)
            {
                return;
            }

            SpriteRenderer spriteRenderer = _npcInstance.GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer == null || spriteRenderer.sprite != null)
            {
                return;
            }

            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f - size * 0.5f) / (size * 0.5f);
                    float dy = (y + 0.5f - size * 0.5f) / (size * 0.5f);
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    texture.SetPixel(x, y, distance <= 1f ? new Color(0.91f, 0.78f, 0.42f, 1f) : Color.clear);
                }
            }
            texture.Apply();

            spriteRenderer.sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>
        /// Editor/testing helper: right-click the component in the Inspector and
        /// choose "Force Start Sequence" to trigger it without walking into the zone.
        /// </summary>
        [ContextMenu("Force Start Sequence (testing)")]
        protected virtual void ForceStartSequenceForTesting()
        {
            if (_sequenceStarted || _sequenceFinished)
            {
                Debug.LogWarning("[NPCDialogTrigger] Sequence already started/finished.", this);
                return;
            }

            Character character = GetMainPlayer();
            if (character == null)
            {
                Debug.LogError("[NPCDialogTrigger] No player found. Run the game first, then use this command.", this);
                return;
            }

            StartSequence(character);
        }
    }
}
