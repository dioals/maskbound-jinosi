using System.Collections;
using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.Playables;
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
        [Tooltip("Seconds to wait after this trigger is called before its dialog starts. Default 0 = starts immediately when called.")]
        [SerializeField] private float activationDelay = 0f;

        [Header("Player")]
        [Tooltip("Freeze the player and force the idle animation while the dialog is playing.")]
        [SerializeField] private bool freezePlayerDuringDialog = true;

        [Header("HUD")]
        [Tooltip("Hide the gameplay HUD while the dialog sequence plays and restore it when it ends.")]
        [SerializeField] private bool hideHudDuringDialog = true;

        [Header("Intro Timeline")]
        [Tooltip("PlayableDirector playing the intro timeline. Paused while the dialog runs, stopped when it ends. If empty, the timeline is left running.")]
        [SerializeField] private PlayableDirector introDirector;

        private Flowchart _flowchartInstance;
        private AIBrain _bossBrain;
        private DamageOnTouch _bossDamage;
        private CorgiCharacter _player;
        private bool _sequenceStarted;
        private bool _sequenceFinished;
        private bool _activationPending;
        private bool _dialogExecuting;
        private bool _hudHidden;
        private bool _timelinePaused;
        private float _activationStartTime;

        protected virtual void Start()
        {
            // Freeze the boss from the start so it stands idle until the dialog
            // finishes and the fight begins.
            ResolveBoss();
            FreezeBoss();
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

        protected virtual CorgiCharacter GetMainPlayer()
        {
            if (LevelManager.HasInstance && LevelManager.Instance.Players != null && LevelManager.Instance.Players.Count > 0)
            {
                return LevelManager.Instance.Players[0];
            }

            return null;
        }

        /// <summary>
        /// Starts the activation countdown for this trigger. When the configured
        /// activation delay elapses, the boss dialog sequence starts. Callable
        /// from the timeline or from another dialog trigger.
        /// </summary>
        public virtual void ActivateTrigger()
        {
            if (_sequenceStarted || _sequenceFinished || _activationPending)
            {
                return;
            }

            if (!string.IsNullOrEmpty(saveFlagKey) && PlayerPrefs.GetInt(saveFlagKey, 0) == 1)
            {
                Debug.Log("[BossFightTrigger] Flag '" + saveFlagKey + "' already set, skipping dialog.", this);
                _sequenceFinished = true;
                return;
            }

            _activationPending = true;
            _activationStartTime = Time.time;
            Debug.Log("[BossFightTrigger] ActivateTrigger called. activationDelay=" + activationDelay + "s, countdown started.", this);
            StartCoroutine(WaitForPlayerThenStart());
        }

        /// <summary>
        /// Convenience wrapper kept for existing timeline wiring. Same as
        /// ActivateTrigger(): starts the countdown, then the dialog.
        /// </summary>
        public virtual void FreezePlayerAndStartDialog()
        {
            ActivateTrigger();
        }

        /// <summary>
        /// Waits until the player is spawned (LevelManager spawns it during scene
        /// load) and then freezes both characters and plays the dialog.
        /// </summary>
        protected virtual IEnumerator WaitForPlayerThenStart()
        {
            CorgiCharacter player = null;
            while (player == null)
            {
                player = GetMainPlayer();
                if (player == null)
                {
                    yield return null;
                }
            }

            Debug.Log("[BossFightTrigger] Player found, waiting for activation delay (" + activationDelay + "s since call).", this);

            // Hold the signal until the activation delay (counted from the moment
            // this trigger was called) has elapsed.
            while (Time.time - _activationStartTime < activationDelay)
            {
                yield return null;
            }

            Debug.Log("[BossFightTrigger] Activation delay elapsed (" + (Time.time - _activationStartTime).ToString("F2") + "s), starting dialog sequence.", this);

            _activationPending = false;
            _player = player;
            FreezePlayer();
            FreezeBoss();
            PlayDialog();
        }

        /// <summary>
        /// Freezes the player using CorgiEngine's Character.Freeze(): gravity is
        /// disabled, forces are zeroed and the character condition becomes Frozen,
        /// which blocks abilities that list Frozen as a blocking condition.
        /// Callable from the timeline via a signal.
        /// </summary>
        public virtual void FreezePlayer()
        {
            if (_player == null)
            {
                _player = GetMainPlayer();
            }

            if (!freezePlayerDuringDialog || _player == null)
            {
                return;
            }

            _player.Freeze();
            ForcePlayerIdle();

            Debug.Log("[BossFightTrigger] Player frozen.", this);
        }

        /// <summary>
        /// Unfreezes the player using CorgiEngine's Character.UnFreeze().
        /// Callable from the timeline via a signal.
        /// </summary>
        public virtual void UnfreezePlayer()
        {
            if (!freezePlayerDuringDialog || _player == null)
            {
                return;
            }

            _player.UnFreeze();

            Debug.Log("[BossFightTrigger] Player unfrozen.", this);
        }

        /// <summary>
        /// Freezes the boss: disables AI and damage so it stands idle and cannot
        /// hurt the player. Callable from the timeline via a signal.
        /// </summary>
        public virtual void FreezeBoss()
        {
            ResolveBoss();

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

            Debug.Log("[BossFightTrigger] Boss frozen.", this);
        }

        /// <summary>
        /// Unfreezes the boss: re-enables AI, transitions it to the fight state
        /// and re-enables its damage. Callable from the timeline via a signal.
        /// </summary>
        public virtual void UnfreezeBoss()
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

            Debug.Log("[BossFightTrigger] Boss unfrozen (state '" + fightStateName + "').", this);
        }

        /// <summary>
        /// Plays the dialog: resolves the flowchart and executes the configured
        /// block. Callable from the timeline via a signal.
        /// </summary>
        public virtual void PlayDialog()
        {
            if (_sequenceStarted || _sequenceFinished)
            {
                return;
            }

            _sequenceStarted = true;
            Debug.Log("[BossFightTrigger] PlayDialog called, executing block '" + blockName + "'.", this);

            if (_flowchartInstance == null)
            {
                ResolveFlowchart();
            }

            if (_flowchartInstance == null)
            {
                Debug.LogError("[BossFightTrigger] No Flowchart found to execute the dialog. Assign 'Flowchart Prefab' in the Inspector.", this);
                EndSequence();
                return;
            }

            PauseIntroTimeline();
            HideHud();
            StartCoroutine(ExecuteDialogRoutine());
        }

        /// <summary>
        /// Pauses the intro timeline while the dialog is running so the camera
        /// (and any remaining timeline content) freezes during the dialog.
        /// </summary>
        protected virtual void PauseIntroTimeline()
        {
            if (introDirector == null || _timelinePaused)
            {
                return;
            }

            _timelinePaused = true;
            introDirector.Pause();
            Debug.Log("[BossFightTrigger] Intro timeline paused.", this);
        }

        /// <summary>
        /// Resumes the intro timeline after the dialog ends so any remaining
        /// timeline content (e.g. camera blends) plays out. If the timeline was
        /// never paused (no director assigned), it is left alone.
        /// </summary>
        protected virtual void ResumeIntroTimeline()
        {
            if (introDirector == null)
            {
                return;
            }

            if (_timelinePaused)
            {
                introDirector.Resume();
                _timelinePaused = false;
                Debug.Log("[BossFightTrigger] Intro timeline resumed.", this);
            }
            else
            {
                introDirector.Stop();
                Debug.Log("[BossFightTrigger] Intro timeline was not paused, stopped instead.", this);
            }
        }

        /// <summary>
        /// Stops the dialog: unfreezes both characters, restores the HUD and
        /// starts the fight. Callable from the timeline via a signal.
        /// </summary>
        public virtual void StopDialog()
        {
            EndSequence();
        }

        /// <summary>
        /// Starts another dialog trigger (e.g. an NPC dialog) programmatically.
        /// Callable from a Fungus "Call Method" command inside the boss dialog to
        /// chain to the next dialog trigger. The target trigger's collider is not
        /// needed - TriggerDialog() is invoked directly.
        /// </summary>
        public virtual void CallDialogTrigger(NPCDialogTrigger target)
        {
            if (target == null)
            {
                Debug.LogWarning("[BossFightTrigger] CallDialogTrigger: target is null.", this);
                return;
            }

            Debug.Log("[BossFightTrigger] Calling dialog trigger '" + target.name + "'.", this);
            target.TriggerDialog();
        }

        protected virtual IEnumerator ExecuteDialogRoutine()
        {
            yield return new WaitForSeconds(dialogDelay);

            Debug.Log("[BossFightTrigger] Executing block '" + blockName + "' on flowchart '" + _flowchartInstance.name + "'.", this);
            _dialogExecuting = true;
            _flowchartInstance.ExecuteBlock(blockName);
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

        /// <summary>
        /// Called when the boss dialog finishes: unfreezes both characters,
        /// restores the HUD and starts the fight. Invoked automatically once the
        /// dialog ends, or explicitly from the timeline / a Fungus block.
        /// </summary>
        public virtual void UnfreezePlayerAndEndDialog()
        {
            EndSequence();
        }

        protected virtual void EndSequence()
        {
            if (_sequenceFinished)
            {
                return;
            }

            _sequenceFinished = true;
            _dialogExecuting = false;

            ResumeIntroTimeline();
            UnfreezeBoss();
            UnfreezePlayer();
            ShowHud();

            if (!string.IsNullOrEmpty(saveFlagKey))
            {
                PlayerPrefs.SetInt(saveFlagKey, 1);
                PlayerPrefs.Save();
                Debug.Log("[BossFightTrigger] Sequence finished, flag '" + saveFlagKey + "' set.", this);
            }
            else
            {
                Debug.Log("[BossFightTrigger] Sequence finished (no saveFlagKey set, not saved).", this);
            }
        }

        [ContextMenu("Force Start Sequence (testing)")]
        protected virtual void ForceStartSequenceForTesting()
        {
            if (_sequenceStarted || _sequenceFinished || _activationPending)
            {
                Debug.LogWarning("[BossFightTrigger] Sequence already started/finished.", this);
                return;
            }

            _activationPending = true;
            _activationStartTime = Time.time;
            Debug.Log("[BossFightTrigger] ForceStartSequenceForTesting called. activationDelay=" + activationDelay + "s, countdown started.", this);
            StartCoroutine(WaitForPlayerThenStart());
        }
    }
}
