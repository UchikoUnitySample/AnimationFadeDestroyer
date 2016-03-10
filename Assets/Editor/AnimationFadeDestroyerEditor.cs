using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections;
using System.Collections.Generic;


[InitializeOnLoad]
public class AnimationFadeDestroyer : EditorWindow
{
	public GameObject target;
	Vector2 scrollPosition = Vector2.zero;
	Dictionary<string, List<ChildAnimatorState>> layerStateList;

	[MenuItem("AnimationFadeDestroyer/OpenWindow", false)]
	public static void OpenWindow()
	{
		GetWindow<AnimationFadeDestroyer>();
	}

	public void OnSelectionChange()
	{
		var act = Selection.activeGameObject;

		if (act != null && act.GetComponent<Animator>() != null) {
			target = act;
			GatherStates();
			Repaint();
		}
	}

	void OnGUI()
	{
		if (target == null || target.GetComponent<Animator>() == null || layerStateList == null) {
			EditorGUILayout.HelpBox("Select target GameObject", MessageType.Info);
			return;
		}

		EditorGUILayout.InspectorTitlebar(false, target);

		EditorGUILayout.Space();

		scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUI.skin.box);

		var animator = target.GetComponent<Animator>();
		var controller = GetTargetAnimatorController();
		int transitionCount = 0;

		foreach (var layerInfo in layerStateList) {
			var layer = System.Array.Find<AnimatorControllerLayer>(
				controller.layers, 
				p => p.name == layerInfo.Key
			);

			foreach (var state in layerInfo.Value) {
				var transition = GetTransition(state.state.name, layer);

				if (animator == null || transition == null) {
					continue;
				}

				transitionCount++;

				if (transition.duration > 0 || transition.offset > 0 || transition.exitTime < 0.995f) {
					EditorGUILayout.BeginHorizontal("box");

					EditorGUILayout.LabelField(state.state.name, EditorStyles.boldLabel);

					if (GUILayout.Button("Display")) {
						Selection.activeObject = transition;
						FocusInspectorWindow();
					}

					if (GUILayout.Button("Apply")) {
						transition.exitTime = 0.9999999f;
						transition.duration = 0;
						transition.offset = 0;

						Selection.activeObject = transition;
						FocusInspectorWindow();
					}

					EditorGUILayout.EndHorizontal();
				}
			}
		}

		if (transitionCount > 0) {
			EditorGUILayout.LabelField("Apply all animations.");
			EditorGUILayout.LabelField("Number of transitions: " + transitionCount);
		}

		GUILayout.EndScrollView();
	}

	void GatherStates()
	{
		layerStateList = new Dictionary<string, List<ChildAnimatorState>>();

		foreach (var layer in GetTargetAnimatorController().layers) {
			var layerInStates = new List<ChildAnimatorState>();
			foreach (var state in layer.stateMachine.states) {
				layerInStates.Add(state);
			}
			layerStateList.Add(layer.name, layerInStates);
		}
	}

	AnimatorController GetTargetAnimatorController()
	{
		return target.GetComponent<Animator>().runtimeAnimatorController as AnimatorController;
	}

	AnimatorStateTransition GetTransition(string nextStateName, AnimatorControllerLayer layer)
	{
		var animator = target.GetComponent<Animator>();
		AnimatorStateMachine stateMachine = layer.stateMachine;

		// Find transition from current state
		int index = animator.GetLayerIndex(layer.name);
		int currentHash = animator.GetCurrentAnimatorStateInfo(index).fullPathHash;
		foreach (var childState in stateMachine.states) {
			if (Animator.StringToHash(layer.name + "." + childState.state.name) == currentHash) {
				var transition = System.Array.Find<AnimatorStateTransition>
					(childState.state.transitions, p => p.destinationState.name == nextStateName);
				if (transition != null) {
					return transition;
				}
			}
		}

		// Find transition from "Any State"
		return System.Array.Find<AnimatorStateTransition>(
			stateMachine.anyStateTransitions,
			p => p.destinationState.name == nextStateName
		);
	}

	static void FocusInspectorWindow()
	{
		var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
		foreach (var window in windows) {
			if (window.titleContent.text == "Inspector") {
				window.Focus();
			}
		}
	}
}