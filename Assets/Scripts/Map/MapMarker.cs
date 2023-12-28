using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Map
{
    public class MapMarker
    {
        public bool selected = false;
        
        private readonly Color _defaultColor = Color.white;
        private Color selectedColor = Color.cyan;
        public Color color;

        public string labelText;

        public Vector2 normalizedPosition;
        public Vector3 worldPosition;

        public MapMarker(Vector2 normalizedPosition, Vector3 worldPosition, [CanBeNull] string label, Color? color = null)
        {
            this.normalizedPosition = normalizedPosition;
            this.worldPosition = worldPosition;
            this.color = color ?? _defaultColor;
            labelText = label ?? worldPosition.ToString();
        }

        public void UpdateMarkerUI(GameObject markerUI)
        {
            // Set Color & Label of Marker
            var sprite = markerUI.GetComponentInChildren<Image>();
            var label = markerUI.GetComponentInChildren<TMP_Text>();

            if (selected)
                sprite.color = selectedColor;
            else
                sprite.color = color;
            label.color = color;
            label.text = labelText;
        }
    }
}