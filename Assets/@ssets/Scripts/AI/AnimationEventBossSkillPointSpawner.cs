using UnityEngine;

namespace MaskboundJinosi.AI
{
    [AddComponentMenu("Maskbound/AI/Animation Event Boss Skill Point Spawner")]
    public class AnimationEventBossSkillPointSpawner : MonoBehaviour
    {
        public enum SpawnMode
        {
            AllPoints,
            PointIndex,
            RandomPoint
        }

        [SerializeField] private string groupId = "prabu_klana_skill";
        [SerializeField] private GameObject prefab;
        [Header("Weakness Variant")]
        [SerializeField] private GameObject specialPrefab;
        [Tooltip("Index spawn point yang berubah menjadi Hammer Bomb saat gilirannya tiba.")]
        [SerializeField] private int[] specialPointIndices = { 1 };
        [Tooltip("Bomb paling cepat muncul pada serangan Hammer Rain ke berapa.")]
        [SerializeField, Min(1)] private int minimumAttackBeforeSpecial = 2;
        [Tooltip("Bomb paling lambat muncul pada serangan Hammer Rain ke berapa.")]
        [SerializeField, Min(1)] private int maximumAttackBeforeSpecial = 3;
        [SerializeField] private SpawnMode spawnMode = SpawnMode.AllPoints;
        [SerializeField] private int pointIndex;
        [SerializeField] private bool matchPointRotation = true;

        private int _attackCount;
        private int _specialAttackNumber;
        private bool _spawnSpecialThisSequence;

        public void SpawnConfigured()
        {
            if (prefab == null || !BossSkillSpawnPointGroup.TryGet(groupId, out BossSkillSpawnPointGroup group))
            {
                return;
            }

            switch (spawnMode)
            {
                case SpawnMode.PointIndex:
                    SpawnAt(group.GetPoint(pointIndex));
                    break;
                case SpawnMode.RandomPoint:
                    SpawnAt(group.GetRandomPoint());
                    break;
                case SpawnMode.AllPoints:
                default:
                    for (int i = 0; i < group.Points.Count; i++)
                    {
                        SpawnAt(group.Points[i]);
                    }
                    break;
            }
        }

        public void SpawnAtPointIndex(int index)
        {
            if (prefab == null || !BossSkillSpawnPointGroup.TryGet(groupId, out BossSkillSpawnPointGroup group))
            {
                return;
            }

            if (index == 0 || _specialAttackNumber <= 0)
            {
                PrepareSpecialSequence();
            }

            GameObject selectedPrefab = _spawnSpecialThisSequence && IsSpecialPoint(index)
                ? specialPrefab
                : prefab;
            SpawnAt(group.GetPoint(index), selectedPrefab);
        }

        private void PrepareSpecialSequence()
        {
            if (_specialAttackNumber <= 0)
            {
                ChooseNextSpecialAttack();
            }

            _attackCount++;
            _spawnSpecialThisSequence = specialPrefab != null && _attackCount >= _specialAttackNumber;
            if (_spawnSpecialThisSequence)
            {
                _attackCount = 0;
                ChooseNextSpecialAttack();
            }
        }

        private void ChooseNextSpecialAttack()
        {
            int min = Mathf.Max(1, minimumAttackBeforeSpecial);
            int max = Mathf.Max(min, maximumAttackBeforeSpecial);
            _specialAttackNumber = Random.Range(min, max + 1);
        }

        private bool IsSpecialPoint(int index)
        {
            if (specialPointIndices == null) { return false; }
            for (int i = 0; i < specialPointIndices.Length; i++)
            {
                if (specialPointIndices[i] == index) { return true; }
            }
            return false;
        }

        private void SpawnAt(Transform point)
        {
            SpawnAt(point, prefab);
        }

        private void SpawnAt(Transform point, GameObject selectedPrefab)
        {
            if (point == null || selectedPrefab == null)
            {
                return;
            }

            Quaternion rotation = matchPointRotation ? point.rotation : Quaternion.identity;
            Instantiate(selectedPrefab, point.position, rotation);
        }
    }
}
