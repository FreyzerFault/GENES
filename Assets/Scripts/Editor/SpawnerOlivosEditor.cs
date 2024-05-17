using TreesGeneration;
using UnityEditor;
using UnityEngine;

namespace Editor
{
	[CustomEditor(typeof(SpawnerOlivos))]
	public class SpawnerOlivosEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			var spawnerOlivos = (SpawnerOlivos)target;

			EditorGUILayout.Space();

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
		}
	}
}
