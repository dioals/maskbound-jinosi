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
        [SerializeField] private SpawnMode spawnMode = SpawnMode.AllPoints;
        [SerializeField] private int pointIndex;
        [SerializeField] private bool matchPointRotation = true;

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

            SpawnAt(group.GetPoint(index));
        }

        private void SpawnAt(Transform point)
        {
            if (point == null)
            {
                return;
            }

            Quaternion rotation = matchPointRotation ? point.rotation : Quaternion.identity;
            Instantiate(prefab, point.position, rotation);
        }
    }
}
