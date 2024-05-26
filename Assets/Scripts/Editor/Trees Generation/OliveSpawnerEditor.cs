using DavidUtils.Editor.Spawning;
using TreesGeneration;
using UnityEditor;

namespace GENES.Editor.Trees_Generation
{
    [CustomEditor(typeof(OliveSpawner), true)]
    public class OliveSpawnerEditor: SpawnerBoxEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            var spawner = (OliveSpawner)target;

            if (spawner == null) return;
            
            EditorGUILayout.Space();
            
            // TODO - Añadir aqui botones para debugear
        }
    }
}
