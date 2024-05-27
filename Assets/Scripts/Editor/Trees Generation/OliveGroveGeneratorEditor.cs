using TreesGeneration;
using UnityEditor;
using UnityEngine;

namespace GENES.Editor.Trees_Generation
{
	[CustomEditor(typeof(OliveGroveGenerator))]
	public class OliveGroveGeneratorEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			var spawnerOlivos = (OliveGroveGenerator)target;

			EditorGUILayout.BeginHorizontal();

			// Boton para generar Voronoi
			if (GUILayout.Button("Populate Regions"))
				spawnerOlivos.Run();

			if (GUILayout.Button("New Seeds"))
			{
				spawnerOlivos.RandomizeSeeds();
				spawnerOlivos.Run();
			}

			if (GUILayout.Button("Reset"))
				spawnerOlivos.Reset();

			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space();

			base.OnInspectorGUI();
		}
	}
}
