using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapMarker : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private Image markerSprite;

    public Color Color => markerSprite.color;
    public string LabelText => label.text;

    private void Awake()
    {
        label ??= GetComponentInChildren<TMP_Text>();
        markerSprite ??= GetComponentInChildren<Image>();
    }

    public void SetMarkerColor(Color color)
    {
        markerSprite.color = color;
    }

    public void SetLabelText(string text)
    {
        label.text = text;
    }

    public void SetLabelColor(Color color)
    {
        label.color = color;
    }

    public void ToggleLabel()
    {
        label.gameObject.SetActive(!label.gameObject.activeSelf);
    }
}