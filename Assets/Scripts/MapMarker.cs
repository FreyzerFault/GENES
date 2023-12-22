using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapMarker
{
    private readonly Color defaultColor = Color.white;
    public Color color;

    public string labelText;

    public Vector2 normalizedPosition;
    public Vector3 worldPosition;

    public MapMarker(Vector2 normalizedPosition, Vector3 worldPosition, [CanBeNull] string label, Color? color = null)
    {
        this.normalizedPosition = normalizedPosition;
        this.worldPosition = worldPosition;
        this.color = color ?? defaultColor;
        labelText = label ?? worldPosition.ToString();
    }

    public void UpdateMarkerUI(GameObject markerUI)
    {
        // Set Color & Label of Marker
        var sprite = markerUI.GetComponentInChildren<Image>();
        var label = markerUI.GetComponentInChildren<TMP_Text>();

        sprite.color = color;
        label.color = color;
        label.text = labelText;
    }
}