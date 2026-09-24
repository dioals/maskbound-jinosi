using UnityEngine;
using UnityEngine.Events;

namespace MaskboundJinosi.UI
{
    /// <summary>
    /// Jembatan tombol ke overlay tutorial: cari TutorialUI saat runtime
    /// (termasuk yang nonaktif) lalu tampilkan pagernya. Dipasang sekali di
    /// CreditPanel; tombol TUTORIAL memanggil ShowTutorial() lewat Inspector.
    /// OnTutorialFinished dipanggil saat user next di halaman terakhir.
    /// </summary>
    [AddComponentMenu("Maskbound/UI/Tutorial UI Opener")]
    public class TutorialUIOpener : MonoBehaviour
    {
        [Tooltip("Nama root TutorialUI di scene. Dicari otomatis, termasuk yang nonaktif.")]
        [SerializeField] private string tutorialRootName = "TutorialUI";

        [Header("Events")]
        [Tooltip("Dipanggil hanya saat user Next di halaman terakhir (selesai baca semua). Close manual tidak memanggil ini.")]
        public UnityEvent OnTutorialFinished;

        private CreditPager _tutorialPager;

        protected virtual void Awake()
        {
            ResolveTutorial();
            SubscribePager();
        }

        protected virtual void OnDestroy()
        {
            UnsubscribePager();
        }

        public virtual void ShowTutorial()
        {
            if (_tutorialPager == null)
            {
                ResolveTutorial();
                SubscribePager();
            }

            if (_tutorialPager == null)
            {
                Debug.LogWarning("[TutorialUIOpener] TutorialUI '" + tutorialRootName + "' tidak ketemu.", this);
                return;
            }

            _tutorialPager.Show();
        }

        protected virtual void ResolveTutorial()
        {
            GameObject[] objects = FindObjectsByType<GameObject>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (GameObject target in objects)
            {
                if (target != null && target.name == tutorialRootName)
                {
                    _tutorialPager = target.GetComponentInChildren<CreditPager>(true);
                    return;
                }
            }
        }

        protected virtual void SubscribePager()
        {
            if (_tutorialPager == null)
            {
                return;
            }

            _tutorialPager.OnCompletedLastPage.RemoveListener(HandlePagerFinished);
            _tutorialPager.OnCompletedLastPage.AddListener(HandlePagerFinished);
        }

        protected virtual void UnsubscribePager()
        {
            if (_tutorialPager == null)
            {
                return;
            }

            _tutorialPager.OnCompletedLastPage.RemoveListener(HandlePagerFinished);
        }

        protected virtual void HandlePagerFinished()
        {
            OnTutorialFinished?.Invoke();
        }
    }
}
