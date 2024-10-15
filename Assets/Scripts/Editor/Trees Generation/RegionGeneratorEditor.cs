using GENES.TreesGeneration;
using UnityEditor;
using UnityEngine;

namespace GENES.Editor.Trees_Generation
{
	[CustomEditor(typeof(RegionGenerator), true)]
	public class RegionGeneratorEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			var spawnerOlivos = (RegionGenerator)target;

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

		private void SeedSettingsGUI()
		{
			
		}
	}
}
