using System;
using JetBrains.Annotations;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Map
{
    public class MapMarker : MonoBehaviour
    {
        private MapMarkerData _data;

        private Image _image;
        private TMP_Text _text;

        public GUID Id => _data.id;

        public Color Color
        {
            get => _data.color;
            set
            {
                _data.color = value;
                _image.color = value;
            }
        }

        public string Label
        {
            get => _data.labelText;
            set
            {
                _data.labelText = value;
                _text.text = value;
            }
        }


        private void Start()
        {
            _text = GetComponentInChildren<TMP_Text>();
            _image = GetComponentInChildren<Image>();

            _text.text = _data.labelText;
            _image.color = _data.color;
        }

        public void SetData(MapMarkerData data)
        {
            _data = data;
        }
    }


    [Serializable]
    public struct MapMarkerData
    {
        private static readonly Color DefaultColor = Color.white;

        public Vector2 normalizedPosition;
        public Vector3 worldPosition;

        public Color color;
        public string labelText;

        public readonly GUID id;


        public MapMarkerData(Vector2 normalizedPosition, Vector3 worldPosition, [CanBeNull] string label,
            Color? color = null)
        {
            this.normalizedPosition = normalizedPosition;
            this.worldPosition = worldPosition;

            this.color = color ?? DefaultColor;
            labelText = label ?? worldPosition.ToString();

            id = GUID.Generate();
        }
    }
}