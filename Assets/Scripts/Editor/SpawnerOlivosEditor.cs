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
        
            SpawnerOlivos spawnerOlivos = (SpawnerOlivos) target;
            
            if (GUILayout.Button("Generate Seeds")) spawnerOlivos.GenerateSeeds();
            
            if (GUILayout.Button("Generate Voronoi Regions")) spawnerOlivos.GenerateVoronoiRegions();
            
            if (GUILayout.Button("Run Animation")) spawnerOlivos.RunAnimation();
            if (GUILayout.Button("Stop Animation")) spawnerOlivos.StopAnimation();
        }
    
    }
}
