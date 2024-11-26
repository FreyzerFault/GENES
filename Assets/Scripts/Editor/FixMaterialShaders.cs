using UnityEditor;
using UnityEngine;

namespace GENES.Editor
{
    public class FixMaterialShaders : UnityEditor.Editor
    {
        [MenuItem("Tools/Fix Shaders")]
        public static void ChangeShaders()
        {
            var noMaterialsToFix = true;
            
            // Obtener todos los materiales en el proyecto
            string[] materialGuids = AssetDatabase.FindAssets("t:Material");
            foreach (string guid in materialGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                
                // Solo cambiar los materiales con el shader 'Lit'
                if (material == null || material.shader.name != "Universal Render Pipeline/Lit") continue;
                
                if (material.isVariant)
                {
                    Debug.LogWarning($"<color=maroon>Material '{material.name}' es un variant. No se puede cambiar el shader.</color>", material);
                    continue;
                }
                
                // Cambiar el shader a Simple Lit
                material.shader = Shader.Find("Universal Render Pipeline/Simple Lit");
                EditorUtility.SetDirty(material);
                noMaterialsToFix = false;
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(noMaterialsToFix
                ? "<color=yellow>Nada que arreglar. No hay materiales con el shader 'Lit'.</color>"
                : "<color=green>Shaders arreglados. Cambiado Shader 'Lit' a 'Simple Lit'.</color>");
        }
    }
}
