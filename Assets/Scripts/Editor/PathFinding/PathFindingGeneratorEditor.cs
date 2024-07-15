using GENES.PathFinding;
using UnityEditor;
using UnityEngine;

namespace GENES.Editor.PathFinding
{
	[CustomEditor(typeof(PathFindingGenerator))]
	public class PathFindingGeneratorEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			var generator = (PathFindingGenerator)target;

			if (GUILayout.Button("Redo PathFinding"))
			{
				generator.PathFindingAlgorithm.CleanCache();
				generator.RedoPath();
			}
		}
	}
}
