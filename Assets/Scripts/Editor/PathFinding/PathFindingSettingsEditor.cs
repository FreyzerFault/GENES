using GENES.PathFinding.Algorithms;
using GENES.PathFinding.Settings;
using UnityEditor;

namespace GENES.Editor.PathFinding
{
	[CustomEditor(typeof(PathFindingSettings))]
	public class PathFindingSettingsEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			var settings = (PathFindingSettings)target;

			EditorGUILayout.PropertyField(serializedObject.FindProperty("algorithm"));
			EditorGUILayout.PropertyField(
				serializedObject.FindProperty(
					settings.algorithm switch
					{
						PathFindingAlgorithmType.Astar => "aStarParameters",
						PathFindingAlgorithmType.AstarDirectional => "aStarDirectionalParameters",
						PathFindingAlgorithmType.Dijkstra => "dijkstraParameters",
						_ => "aStarParameters"
					}
				)
			);

			EditorGUILayout.Space();

			EditorGUILayout.IntSlider(serializedObject.FindProperty("maxIterations"), 100, 10000);
			EditorGUILayout.Slider(serializedObject.FindProperty("cellSize"), .1f, 5);

			EditorGUILayout.Space();

			EditorGUILayout.LabelField("Restricciones", EditorStyles.boldLabel);
			EditorGUILayout.Slider(serializedObject.FindProperty("maxSlopeAngle"), 5, 90);
			EditorGUILayout.Slider(serializedObject.FindProperty("minHeight"), 0, 100);

			EditorGUILayout.Space();

			serializedObject.FindProperty("useCache").boolValue = EditorGUILayout.ToggleLeft(
				"Use Cache",
				serializedObject.FindProperty("useCache").boolValue
			);
			// EditorGUILayout.PropertyField();

			serializedObject.ApplyModifiedProperties();
		}
	}
}
