using System.Collections.Generic;
using UnityEngine;

namespace MaskboundJinosi.AI
{
    [AddComponentMenu("Maskbound/AI/Boss Skill Spawn Point Group")]
    public class BossSkillSpawnPointGroup : MonoBehaviour
    {
        private static readonly Dictionary<string, BossSkillSpawnPointGroup> Groups = new();

        [SerializeField] private string groupId = "prabu_klana_skill";
        [SerializeField] private List<Transform> points = new();
        [SerializeField] private bool autoCollectChildren = true;

        public string GroupId => groupId;
        public IReadOnlyList<Transform> Points => points;

        private void Awake()
        {
            CollectChildrenIfNeeded();
            Groups[groupId] = this;
        }

        private void OnDestroy()
        {
            if (Groups.TryGetValue(groupId, out BossSkillSpawnPointGroup current) && current == this)
            {
                Groups.Remove(groupId);
            }
        }

        public static bool TryGet(string id, out BossSkillSpawnPointGroup group)
        {
            return Groups.TryGetValue(id, out group) && group != null;
        }

        public Transform GetPoint(int index)
        {
            if (points.Count == 0)
            {
                return null;
            }

            return points[Mathf.Clamp(index, 0, points.Count - 1)];
        }

        public Transform GetRandomPoint()
        {
            if (points.Count == 0)
            {
                return null;
            }

            return points[Random.Range(0, points.Count)];
        }

        private void CollectChildrenIfNeeded()
        {
            if (!autoCollectChildren || points.Count > 0)
            {
                return;
            }

            for (int i = 0; i < transform.childCount; i++)
            {
                points.Add(transform.GetChild(i));
            }
        }
    }
}
