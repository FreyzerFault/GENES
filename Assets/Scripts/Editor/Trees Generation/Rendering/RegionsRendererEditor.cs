using DavidUtils.Editor.Rendering;
using GENES.TreesGeneration.Rendering;
using UnityEditor;

namespace GENES.Editor.Trees_Generation.Rendering
{
    [CustomEditor(typeof(RegionsRenderer))]
    public class RegionRendererEditor: DynamicRendererEditor
    {
        bool thicknessFoldout = true;
        
        public override void OnInspectorGUI()
        {
            var regionsRenderer = (RegionsRenderer) target;
            if (regionsRenderer == null) return;
            
            ColorGUI(regionsRenderer);
            
            EditorGUILayout.Separator();
            EditorGUILayout.Separator();
            
            ThicknessGUI(regionsRenderer);
        }
        
        private void ColorGUI(RegionsRenderer renderer) => base.ColorGUI(renderer);

        private void ThicknessGUI(RegionsRenderer renderer)
        {
            thicknessFoldout = EditorGUILayout.Foldout(thicknessFoldout, "THICKNESS", true, EditorStyles.foldoutHeader);
            if (!thicknessFoldout) return;

            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();
            float thickness = EditorGUILayout.Slider("Thickness", renderer.Thickness, 0, 1);
            if (EditorGUI.EndChangeCheck()) renderer.Thickness = thickness;
            EditorGUI.indentLevel--;
        }
    }
}
