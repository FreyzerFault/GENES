using Procrain.Editor;
using UI.MapUI;
using UnityEditor;
using UnityEngine;

namespace UI.Editor
{
    [CustomEditor(typeof(MapRendererUI), true)]
    public class MapRendererUIEditor : MapDisplayEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var mapUI = target as MapRendererUI;
            if (mapUI == null) return;
            
            if (GUILayout.Button("UpdateMap")) mapUI.DisplayMap();
            if (GUILayout.Button("UpdatePlayerIcon")) mapUI.UpdatePlayerIcon();
            if (GUILayout.Button("ApplyZoom")) mapUI.ApplyZoomSmooth();
        }
    }
}
