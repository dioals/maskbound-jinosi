using System.Collections;
using MaskboundJinosi.UI;
using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using UnityEngine;

namespace MaskboundJinosi.Gameplay.Scene
{
    [AddComponentMenu("Maskbound/Scene/Player Game Over Overlay Controller")]
    public class PlayerGameOverOverlayController : MonoBehaviour, MMEventListener<CorgiEngineEvent>
    {
        [SerializeField] private bool resetChallengeTimer = true;
        [SerializeField] private GameFlowManager gameFlowManager;
        [SerializeField] private DemoBossChallengeTimer challengeTimer;

        private Coroutine _returnRoutine;

        private void Awake()
        {
            gameFlowManager ??= GetComponent<GameFlowManager>();
            challengeTimer ??= GetComponent<DemoBossChallengeTimer>();
        }

        private void OnEnable()
        {
            this.MMEventStartListening<CorgiEngineEvent>();
        }

        private void OnDisable()
        {
            this.MMEventStopListening<CorgiEngineEvent>();
        }

        public void OnMMEvent(CorgiEngineEvent engineEvent)
        {
            if (engineEvent.EventType != CorgiEngineEventTypes.GameOver || _returnRoutine != null)
            {
                return;
            }

            if (challengeTimer != null)
            {
                challengeTimer.StopTimer();
            }

            GameOverOverlay.Show();
            _returnRoutine = StartCoroutine(ReturnToStartScreen());
        }

        private IEnumerator ReturnToStartScreen()
        {
            // Tunggu input baru setelah overlay muncul, bukan input dari frame kematian.
            yield return null;
            while (!OverlayConfirmInput.WasPressedThisFrame())
            {
                yield return null;
            }

            if (resetChallengeTimer)
            {
                challengeTimer?.ResetTimer();
            }

            gameFlowManager?.ReturnToMainMenu();
            _returnRoutine = null;
        }
    }
}
