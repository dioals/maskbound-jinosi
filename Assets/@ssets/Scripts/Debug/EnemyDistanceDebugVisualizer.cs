using System;
using System.Collections.Generic;
using MoreMountains.CorgiEngine;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MaskboundJinosi.Debugging
{
    [ExecuteAlways]
    [AddComponentMenu("Maskbound/Debug/Enemy Distance Debug Visualizer")]
    public class EnemyDistanceDebugVisualizer : MonoBehaviour
    {
        [Serializable]
        public class DistanceBoundary
        {
            public string Label = "Attack Range";
            [Min(0f)] public float Distance = 3f;
            public Color Color = Color.yellow;
        }

        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private bool autoFindPlayer = true;
        [SerializeField] private bool useHorizontalDistanceOnly = true;

        [Header("Distance Ruler")]
        [SerializeField, Min(1)] private int maximumDistance = 7;
        [SerializeField, Min(0.05f)] private float tickHeight = 0.35f;
        [SerializeField] private float verticalOffset;
        [SerializeField] private Color rulerColor = new Color(1f, 1f, 1f, 0.65f);
        [SerializeField] private bool showBothDirections = true;

        [Header("Combat Boundaries")]
        [SerializeField] private List<DistanceBoundary> boundaries = new List<DistanceBoundary>
        {
            new DistanceBoundary
            {
                Label = "Attack 2",
                Distance = 3f,
                Color = new Color(1f, 0.25f, 0.2f, 0.9f)
            },
            new DistanceBoundary
            {
                Label = "Attack 1",
                Distance = 5f,
                Color = new Color(1f, 0.75f, 0.1f, 0.9f)
            }
        };

        [Header("Target Line")]
        [SerializeField] private bool showTargetLine = true;
        [SerializeField] private Color targetLineColor = Color.cyan;

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        public float CurrentDistance
        {
            get
            {
                if (target == null)
                {
                    return -1f;
                }

                Vector3 difference = target.position - transform.position;
                return useHorizontalDistanceOnly
                    ? Mathf.Abs(difference.x)
                    : difference.magnitude;
            }
        }

        private void OnEnable()
        {
            TryFindPlayer();
        }

        private void Update()
        {
            if (target == null)
            {
                TryFindPlayer();
            }
        }

        private void TryFindPlayer()
        {
            if (!autoFindPlayer || target != null)
            {
                return;
            }

            if (Application.isPlaying &&
                LevelManager.HasInstance &&
                LevelManager.Instance.Players != null &&
                LevelManager.Instance.Players.Count > 0 &&
                LevelManager.Instance.Players[0] != null)
            {
                target = LevelManager.Instance.Players[0].transform;
                return;
            }

#if UNITY_EDITOR
            Character[] characters = FindObjectsOfType<Character>(true);
            foreach (Character character in characters)
            {
                if (character.CharacterType == Character.CharacterTypes.Player)
                {
                    target = character.transform;
                    return;
                }
            }
#endif
        }

        private void OnDrawGizmos()
        {
            TryFindPlayer();

            Vector3 origin = transform.position + Vector3.up * verticalOffset;
            DrawRuler(origin);
            DrawBoundaries(origin);
            DrawTarget(origin);
        }

        private void DrawRuler(Vector3 origin)
        {
            Gizmos.color = rulerColor;
            float leftExtent = showBothDirections ? -maximumDistance : 0f;
            Gizmos.DrawLine(
                origin + Vector3.right * leftExtent,
                origin + Vector3.right * maximumDistance);

            for (int distance = 0; distance <= maximumDistance; distance++)
            {
                DrawTick(origin, distance);
                if (showBothDirections && distance > 0)
                {
                    DrawTick(origin, -distance);
                }
            }
        }

        private void DrawTick(Vector3 origin, int signedDistance)
        {
            Vector3 tickPosition = origin + Vector3.right * signedDistance;
            Gizmos.DrawLine(
                tickPosition + Vector3.down * tickHeight,
                tickPosition + Vector3.up * tickHeight);

#if UNITY_EDITOR
            GUIStyle style = CreateLabelStyle(rulerColor);
            Handles.Label(
                tickPosition + Vector3.up * (tickHeight + 0.08f),
                $"{Mathf.Abs(signedDistance)}m",
                style);
#endif
        }

        private void DrawBoundaries(Vector3 origin)
        {
            if (boundaries == null)
            {
                return;
            }

            foreach (DistanceBoundary boundary in boundaries)
            {
                if (boundary == null || boundary.Distance < 0f)
                {
                    continue;
                }

                DrawBoundary(origin, boundary, 1f);
                if (showBothDirections && boundary.Distance > 0f)
                {
                    DrawBoundary(origin, boundary, -1f);
                }
            }
        }

        private void DrawBoundary(Vector3 origin, DistanceBoundary boundary, float direction)
        {
            Vector3 position = origin + Vector3.right * boundary.Distance * direction;
            float height = tickHeight * 2.25f;

            Gizmos.color = boundary.Color;
            Gizmos.DrawLine(
                position + Vector3.down * height,
                position + Vector3.up * height);
            Gizmos.DrawWireSphere(position, 0.12f);

#if UNITY_EDITOR
            GUIStyle style = CreateLabelStyle(boundary.Color);
            Handles.Label(
                position + Vector3.up * (height + 0.08f),
                $"{boundary.Label} ({boundary.Distance:0.##}m)",
                style);
#endif
        }

        private void DrawTarget(Vector3 origin)
        {
            if (target == null)
            {
#if UNITY_EDITOR
                Handles.Label(
                    origin + Vector3.up * 1.25f,
                    "Target: belum ditemukan",
                    CreateLabelStyle(Color.red));
#endif
                return;
            }

            Vector3 targetPosition = target.position;
            if (useHorizontalDistanceOnly)
            {
                targetPosition.y = origin.y;
                targetPosition.z = origin.z;
            }

            if (showTargetLine)
            {
                Gizmos.color = targetLineColor;
                Gizmos.DrawLine(origin, targetPosition);
                Gizmos.DrawWireSphere(targetPosition, 0.15f);
            }

#if UNITY_EDITOR
            Handles.Label(
                Vector3.Lerp(origin, targetPosition, 0.5f) + Vector3.up * 0.25f,
                $"Player: {CurrentDistance:0.00}m",
                CreateLabelStyle(targetLineColor));
#endif
        }

#if UNITY_EDITOR
        private static GUIStyle CreateLabelStyle(Color color)
        {
            return new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = color },
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12
            };
        }
#endif
    }
}
