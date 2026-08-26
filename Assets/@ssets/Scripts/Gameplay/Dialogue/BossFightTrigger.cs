using System.Collections;
using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Flowchart = Fungus.Flowchart;
using CorgiCharacter = MoreMountains.CorgiEngine.Character;

namespace MaskboundJinosi.Gameplay.Dialogue
{
    /// <summary>
    /// Boss fight intro: when the player enters the trigger, the boss is frozen
    /// (AI + damage disabled), the player is frozen, and a Fungus dialog plays.
    /// Once the dialog finishes, the boss AI starts (transition to its chase
    /// state) and the fight begins. Only plays once per save.
    /// </summary>
    [AddComponentMenu("Maskbound/Dialogue/Boss Fight Trigger")]
    [RequireComponent(typeof(BoxCollider2D))]
    public class BossFightTrigger : MonoBehaviour
    {
        [Header("Boss")]
        [Tooltip("The boss character. If empty, found by 'Boss Object Name' at runtime.")]
        [SerializeField] private CorgiCharacter boss;
        [Tooltip("Fallback: name of the boss GameObject (or root) to find at runtime.")]
        [SerializeField] private string bossObjectName = "PrabuKlana";
        [Tooltip("AI state to transition to when the fight starts.")]
        [SerializeField] private string fightStateName = "Chase";
        [Tooltip("Freeze the boss during the dialog: disable AIBrain and DamageOnTouch.")]
        [SerializeField] private bool freezeBossDuringDialog = true;

        [Header("Dialog (Fungus)")]
        [Tooltip("Fungus Flowchart prefab holding the dialog. If empty, the first Flowchart in the scene is used.")]
        [SerializeField] private Flowchart flowchartPrefab;
        [Tooltip("Name of the block to execute on the Flowchart.")]
        [SerializeField] private string blockName = "BossIntro";
        [Tooltip("PlayerPrefs key used so the dialog only plays once.")]
        [SerializeField] private string saveFlagKey = "Maskbound.BossIntroShown";

        [Header("Sequence")]
        [Tooltip("Delay before the dialog starts.")]
        [SerializeField] private float dialogDelay = 0.3f;

        [Header("Intro Timeline (optional)")]
        [Tooltip("Timeline played before the dialog: drives the camera and character animations. If empty, the dialog starts immediately.")]
        [SerializeField] private TimelineAsset introTimeline;
        [Tooltip("PlayableDirector that plays the intro timeline. If empty, a temporary one is created at runtime.")]
        [SerializeField] private PlayableDirector introDirector;

        [Header("Player")]
        [Tooltip("Freeze the player and force the idle animation while the dialog is playing.")]
        [SerializeField] private bool freezePlayerDuringDialog = true;

        [Header("HUD")]
        [Tooltip("Hide the gameplay HUD while the dialog sequence plays and restore it when it ends.")]
        [SerializeField] private bool hideHudDuringDialog = true;

        [Header("Testing")]
        [Tooltip("Editor testing: start the sequence shortly after the scene loads, without walking into the trigger.")]
        [SerializeField] private bool startOnLoadForTesting;
        [Tooltip("Delay before 'Start On Load For Testing' fires.")]
        [SerializeField] private float startOnLoadDelay = 1.5f;

        private Flowchart _flowchartInstance;
        private AIBrain _bossBrain;
        private DamageOnTouch _bossDamage;
        private CorgiCharacter _player;
        private CharacterAbility[] _playerAbilities;
        private bool[] _playerAbilitiesEnabledState;
        private bool _sequenceStarted;
        private bool _sequenceFinished;
        private bool _dialogExecuting;
        private bool _hudHidden;
        private PlayableDirector _activeDirector;

        protected virtual void Start()
        {
            // Freeze the boss from the start so it stands idle until the dialog
            // finishes and the fight begins.
            ResolveBoss();
            FreezeBoss();

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
            if (Time.timeSinceLevelLoad < 0.5f)
            {
                return;
            }

            TryStartSequence(other);
        }

        protected virtual void Update()
        {
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

        protected virtual void TryStartSequence(Collider2D other)
        {
            if (_sequenceStarted || _sequenceFinished)
            {
                return;
            }

            CorgiCharacter character = GetPlayerCharacter(other);
            if (character == null)
            {
                return;
            }

            if (PlayerPrefs.GetInt(saveFlagKey, 0) == 1)
            {
                Debug.Log("[BossFightTrigger] Flag '" + saveFlagKey + "' already set, skipping dialog.", this);
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

            CorgiCharacter player = GetMainPlayer();
            if (player == null)
            {
                Debug.LogWarning("[BossFightTrigger] Start On Load: no player found after " + startOnLoadDelay + "s.", this);
                yield break;
            }

            Debug.Log("[BossFightTrigger] Start On Load (testing) firing.", this);
            StartSequence(player);
        }

        protected virtual CorgiCharacter GetMainPlayer()
        {
            if (LevelManager.HasInstance && LevelManager.Instance.Players != null && LevelManager.Instance.Players.Count > 0)
            {
                return LevelManager.Instance.Players[0];
            }

            return null;
        }

        protected virtual CorgiCharacter GetPlayerCharacter(Collider2D other)
        {
            CorgiCharacter character = other.GetComponentInParent<CorgiCharacter>();
            if (character != null)
            {
                return character;
            }

            if (other.CompareTag("Player"))
            {
                character = GetMainPlayer();
            }

            return character;
        }

        protected virtual void StartSequence(CorgiCharacter player)
        {
            _sequenceStarted = true;
            _player = player;

            Debug.Log("[BossFightTrigger] Player entered trigger, starting boss intro.", this);

            FreezePlayer();
            HideHud();
            ResolveBoss();
            ResolveFlowchart();

            if (_flowchartInstance == null)
            {
                Debug.LogError("[BossFightTrigger] No Flowchart found to execute the dialog. Assign 'Flowchart Prefab' in the Inspector.", this);
                EndSequence();
                return;
            }

            StartCoroutine(SequenceRoutine());
        }

        protected virtual IEnumerator SequenceRoutine()
        {
            // Freeze the boss so it stays idle while the intro plays.
            FreezeBoss();

            if (introTimeline != null)
            {
                yield return StartCoroutine(PlayIntroTimeline());
            }

            yield return new WaitForSeconds(dialogDelay);

            Debug.Log("[BossFightTrigger] Executing block '" + blockName + "' on flowchart '" + _flowchartInstance.name + "'.", this);
            _dialogExecuting = true;
            _flowchartInstance.ExecuteBlock(blockName);
        }

        /// <summary>
        /// Creates/resolves the PlayableDirector, binds the timeline's runtime
        /// targets (player, boss, cinematic cameras), plays it and waits until
        /// the timeline has finished.
        /// </summary>
        protected virtual IEnumerator PlayIntroTimeline()
        {
            PlayableDirector director = introDirector;
            bool temporaryDirector = false;
            if (director == null)
            {
                GameObject go = new GameObject("BossIntro_TimelineDirector");
                director = go.AddComponent<PlayableDirector>();
                temporaryDirector = true;
            }

            director.playableAsset = introTimeline;
            BindTimelineRuntimeTargets(director);

            _activeDirector = director;
            director.Play();

            // Wait until the timeline finishes before the dialog starts.
            while (director != null && director.state == PlayState.Playing)
            {
                yield return null;
            }

            if (temporaryDirector && director != null)
            {
                Destroy(director.gameObject);
            }

            _activeDirector = null;
        }

        /// <summary>
        /// Binds tracks that reference runtime-spawned objects: the player and the
        /// boss. Also resolves Cinemachine shot exposed references to the actual
        /// cameras in the scene, so the timeline drives them.
        /// </summary>
        protected virtual void BindTimelineRuntimeTargets(PlayableDirector director)
        {
            TimelineAsset asset = director.playableAsset as TimelineAsset;
            if (asset == null)
            {
                return;
            }

            CorgiCharacter player = _player != null ? _player : GetMainPlayer();
            CorgiCharacter bossChar = boss;

            foreach (TrackAsset track in asset.GetOutputTracks())
            {
                if (track == null)
                {
                    continue;
                }

                if (track is AnimationTrack)
                {
                    if (player != null && track.name.IndexOf("Player", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        director.SetGenericBinding(track, player.gameObject);
                        Debug.Log("[BossFightTrigger] Bound timeline track '" + track.name + "' to player '" + player.name + "'.", this);
                    }
                    else if (bossChar != null && track.name.IndexOf("Boss", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        director.SetGenericBinding(track, bossChar.gameObject);
                        Debug.Log("[BossFightTrigger] Bound timeline track '" + track.name + "' to boss '" + bossChar.name + "'.", this);
                    }
                }
                else if (track is CinemachineTrack)
                {
                    BindCinemachineShots(director, track);
                }
            }
        }

        /// <summary>
        /// Resolves CinemachineShot exposed references so each shot uses the
        /// CinemachineCamera with the matching name in the scene.
        /// </summary>
        protected virtual void BindCinemachineShots(PlayableDirector director, TrackAsset track)
        {
            foreach (TimelineClip clip in track.GetClips())
            {
                if (clip == null || clip.asset == null)
                {
                    continue;
                }

                string cameraName = clip.displayName != null ? clip.displayName.Trim() : null;
                CinemachineCamera camera = FindCinemachineCameraByName(cameraName);
                if (camera == null)
                {
                    Debug.LogWarning("[BossFightTrigger] No CinemachineCamera named '" + cameraName + "' found for timeline shot.", this);
                    continue;
                }

                // The shot's exposed reference is already bound by the editor when
                // the timeline was set up; only override it when it is not bound yet.
                PropertyName exposedName = GetShotExposedName(clip);
                if (exposedName.ToString().Length > 0)
                {
                    director.SetReferenceValue(exposedName, camera);
                }

                Debug.Log("[BossFightTrigger] Bound Cinemachine shot '" + cameraName + "' to '" + camera.name + "'.", this);
            }
        }

        /// <summary>
        /// Reads the exposed reference name of a CinemachineShot clip asset via
        /// reflection (Cinemachine 3 stores it in VirtualCamera.exposedName).
        /// </summary>
        protected virtual PropertyName GetShotExposedName(TimelineClip clip)
        {
            System.Type type = clip.asset.GetType();
            System.Reflection.FieldInfo field = type.GetField("VirtualCamera");
            if (field == null)
            {
                field = type.GetField("m_VirtualCamera");
            }

            if (field == null)
            {
                return default;
            }

            object value = field.GetValue(clip.asset);
            if (value == null)
            {
                return default;
            }

            System.Reflection.FieldInfo nameField = value.GetType().GetField("exposedName");
            if (nameField == null)
            {
                return default;
            }

            object name = nameField.GetValue(value);
            return name is PropertyName pn ? pn : default;
        }

        protected virtual CinemachineCamera FindCinemachineCameraByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            CinemachineCamera[] cameras = FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (CinemachineCamera camera in cameras)
            {
                if (camera.gameObject.name.Trim() == name.Trim())
                {
                    return camera;
                }
            }

            return null;
        }

        protected virtual void ResolveBoss()
        {
            if (boss == null)
            {
                boss = FindBossByName();
            }

            if (boss != null)
            {
                _bossBrain = boss.GetComponent<AIBrain>();
                _bossDamage = boss.GetComponentInChildren<DamageOnTouch>(true);
            }
            else
            {
                Debug.LogWarning("[BossFightTrigger] Boss not found (name '" + bossObjectName + "'). Fight will not start automatically.", this);
            }
        }

        protected virtual CorgiCharacter FindBossByName()
        {
            if (string.IsNullOrWhiteSpace(bossObjectName))
            {
                return null;
            }

            CorgiCharacter[] characters = FindObjectsByType<CorgiCharacter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (CorgiCharacter character in characters)
            {
                Transform root = character.transform.root;
                if (character.gameObject.name == bossObjectName ||
                    (root != null && root.name == bossObjectName))
                {
                    return character;
                }
            }

            return null;
        }

        /// <summary>
        /// Freezes the boss during the dialog: disables AI and damage so the boss
        /// stands idle and cannot hurt the player while they talk.
        /// </summary>
        protected virtual void FreezeBoss()
        {
            if (!freezeBossDuringDialog)
            {
                return;
            }

            if (_bossBrain != null)
            {
                _bossBrain.enabled = false;
            }

            if (_bossDamage != null)
            {
                _bossDamage.enabled = false;
            }
        }

        /// <summary>
        /// Starts the fight: re-enables the boss AI, transitions it to the fight
        /// state and re-enables its damage.
        /// </summary>
        protected virtual void StartFight()
        {
            if (_bossBrain != null)
            {
                _bossBrain.enabled = true;
                _bossBrain.BrainActive = true;
                _bossBrain.TransitionToState(fightStateName);
            }

            if (_bossDamage != null)
            {
                _bossDamage.enabled = true;
            }

            Debug.Log("[BossFightTrigger] Boss fight started (state '" + fightStateName + "').", this);
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

        protected virtual void FreezePlayer()
        {
            if (!freezePlayerDuringDialog || _player == null)
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

            CorgiController controller = _player.GetComponent<CorgiController>();
            if (controller != null)
            {
                controller.SetHorizontalForce(0f);
            }
        }

        protected virtual void UnfreezePlayer()
        {
            if (!freezePlayerDuringDialog || _player == null)
            {
                return;
            }

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

            StartFight();
            UnfreezePlayer();
            ShowHud();

            PlayerPrefs.SetInt(saveFlagKey, 1);
            PlayerPrefs.Save();

            Debug.Log("[BossFightTrigger] Sequence finished, flag '" + saveFlagKey + "' set.", this);
        }

        [ContextMenu("Force Start Sequence (testing)")]
        protected virtual void ForceStartSequenceForTesting()
        {
            if (_sequenceStarted || _sequenceFinished)
            {
                Debug.LogWarning("[BossFightTrigger] Sequence already started/finished.", this);
                return;
            }

            CorgiCharacter player = GetMainPlayer();
            if (player == null)
            {
                Debug.LogError("[BossFightTrigger] No player found. Run the game first, then use this command.", this);
                return;
            }

            StartSequence(player);
        }
    }
}
