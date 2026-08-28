using MaskboundJinosi.Gameplay.Dialogue;
using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using UnityEngine;

namespace MaskboundJinosi.AI
{
    /// <summary>
    /// AI action yang memanggil sebuah dialog trigger (BossFightTrigger atau
    /// NPCDialogTrigger) saat boss masuk state ini. Cocok untuk memulai dialog
    /// dari tengah alur AI boss (misal setelah state idle/Chase tertentu).
    /// </summary>
    [AddComponentMenu("Maskbound/AI/Actions/AI Action Call Dialog Trigger")]
    public class AIActionCallDialogTrigger : AIAction
    {
        [Header("Targets")]
        [Tooltip("Optional. Trigger boss fight yang diaktifkan (ActivateTrigger). Jika kosong, dicari di scene.")]
        [SerializeField] private BossFightTrigger bossFightTrigger;
        [Tooltip("Optional. Trigger dialog NPC yang diaktifkan (TriggerDialog). Jika kosong, dicari di scene.")]
        [SerializeField] private NPCDialogTrigger npcDialogTrigger;

        [Header("Behavior")]
        [Tooltip("Hanya panggil sekali selama hidup boss. Matikan jika ingin dipanggil tiap masuk state.")]
        [SerializeField] private bool playOnlyOnce = true;

        private bool _hasCalled;

        public override void Initialization()
        {
            if (!ShouldInitialize)
            {
                return;
            }

            ResolveTargets();
            base.Initialization();
        }

        public override void OnEnterState()
        {
            base.OnEnterState();

            if (_hasCalled && playOnlyOnce)
            {
                return;
            }

            ResolveTargets();
            CallDialog();
        }

        public override void PerformAction()
        {
            // Dialog dipanggil sekali saat masuk state, bukan tiap frame.
        }

        private void CallDialog()
        {
            if (bossFightTrigger != null)
            {
                bossFightTrigger.ActivateTrigger();
                _hasCalled = true;
                Debug.Log("[AIActionCallDialogTrigger] BossFightTrigger '" + bossFightTrigger.name + "' dipanggil.", this);
                return;
            }

            if (npcDialogTrigger != null)
            {
                npcDialogTrigger.TriggerDialog();
                _hasCalled = true;
                Debug.Log("[AIActionCallDialogTrigger] NPCDialogTrigger '" + npcDialogTrigger.name + "' dipanggil.", this);
                return;
            }

            Debug.LogWarning("[AIActionCallDialogTrigger] Tidak ada target trigger dialog. Isi 'Boss Fight Trigger' atau 'NPC Dialog Trigger' di Inspector.", this);
        }

        private void ResolveTargets()
        {
            if (bossFightTrigger == null)
            {
                bossFightTrigger = FindFirstObjectByType<BossFightTrigger>(FindObjectsInactive.Include);
            }

            if (npcDialogTrigger == null)
            {
                npcDialogTrigger = FindFirstObjectByType<NPCDialogTrigger>(FindObjectsInactive.Include);
            }
        }
    }
}
