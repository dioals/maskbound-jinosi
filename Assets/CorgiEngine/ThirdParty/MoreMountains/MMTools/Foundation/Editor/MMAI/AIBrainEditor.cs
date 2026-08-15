using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MoreMountains.Tools
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(AIBrain), true)]
	public class AIBrainEditor : Editor
	{
		protected MMReorderableList _list;
		protected SerializedProperty _brainActive;
		protected SerializedProperty _resetBrainOnEnable;
		protected SerializedProperty _resetBrainOnStart;
		protected SerializedProperty _timeInThisState;
		protected SerializedProperty _target;
		protected SerializedProperty _owner;
		protected SerializedProperty _actionsFrequency;
		protected SerializedProperty _decisionFrequency;
		protected SerializedProperty _randomizeFrequencies;
		protected SerializedProperty _randomActionFrequency;
		protected SerializedProperty _randomDecisionFrequency;
		protected int _debugStateIndex;

		protected virtual void OnEnable()
		{
			_list = new MMReorderableList(serializedObject.FindProperty("States"));
			_list.elementNameProperty = "States";
			_list.elementDisplayType = MMReorderableList.ElementDisplayType.Expandable;

			_brainActive = serializedObject.FindProperty("BrainActive");
			_resetBrainOnEnable = serializedObject.FindProperty("ResetBrainOnEnable");
			_resetBrainOnStart = serializedObject.FindProperty("ResetBrainOnStart");
			_timeInThisState = serializedObject.FindProperty("TimeInThisState");
			_target = serializedObject.FindProperty("Target");
			_owner = serializedObject.FindProperty("Owner");
			_actionsFrequency = serializedObject.FindProperty("ActionsFrequency");
			_decisionFrequency = serializedObject.FindProperty("DecisionFrequency");
            
			_randomizeFrequencies = serializedObject.FindProperty("RandomizeFrequencies");
			_randomActionFrequency = serializedObject.FindProperty("RandomActionFrequency");
			_randomDecisionFrequency = serializedObject.FindProperty("RandomDecisionFrequency");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			_list.DoLayoutList();
			EditorGUILayout.PropertyField(_timeInThisState);
			EditorGUILayout.PropertyField(_owner);
			EditorGUILayout.PropertyField(_target);
			EditorGUILayout.PropertyField(_brainActive);
			EditorGUILayout.PropertyField(_resetBrainOnEnable);
			EditorGUILayout.PropertyField(_resetBrainOnStart);
			EditorGUILayout.PropertyField(_actionsFrequency);
			EditorGUILayout.PropertyField(_decisionFrequency);
			EditorGUILayout.PropertyField(_randomizeFrequencies);
			if (_randomizeFrequencies.boolValue)
			{
				EditorGUILayout.PropertyField(_randomActionFrequency);
				EditorGUILayout.PropertyField(_randomDecisionFrequency);
			}
			serializedObject.ApplyModifiedProperties();

			AIBrain brain = (AIBrain)target;
			if (brain.CurrentState != null)
			{
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Current State", brain.CurrentState.StateName);
			}

			DrawDebugStateSelector(brain);
		}

		protected virtual void DrawDebugStateSelector(AIBrain brain)
		{
			if (brain.States == null || brain.States.Count == 0)
			{
				return;
			}

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Debug State", EditorStyles.boldLabel);

			string[] stateNames = new string[brain.States.Count];
			for (int i = 0; i < brain.States.Count; i++)
			{
				stateNames[i] = string.IsNullOrEmpty(brain.States[i].StateName)
					? $"State {i} (Unnamed)"
					: brain.States[i].StateName;
			}

			_debugStateIndex = Mathf.Clamp(_debugStateIndex, 0, stateNames.Length - 1);
			_debugStateIndex = EditorGUILayout.Popup("Target State", _debugStateIndex, stateNames);

			using (new EditorGUI.DisabledScope(!Application.isPlaying || targets.Length != 1))
			{
				if (GUILayout.Button($"Transition To {stateNames[_debugStateIndex]}"))
				{
					brain.TransitionToState(brain.States[_debugStateIndex].StateName);
					EditorUtility.SetDirty(brain);
				}
			}

			if (!Application.isPlaying)
			{
				EditorGUILayout.HelpBox("Tombol debug aktif saat Play Mode.", MessageType.Info);
			}
		}
	}
}
