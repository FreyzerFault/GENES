using Markers;
using UnityEditor;
using UnityEngine;

namespace GENES.Editor.Markers
{
	[CustomEditor(typeof(MarkerManager))]
	public class MarkerManagerEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			var markerManager = (MarkerManager)target;

			if (GUILayout.Button("Clear All Markers")) markerManager.ClearAll();
		}
	}
}
