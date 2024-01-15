using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Map
{
    public class MarkerUI : MonoBehaviour
    {
        [FormerlySerializedAs("data")] [SerializeField]
        private Marker marker;

        private Image _image;
        private TMP_Text _text;

        public Marker Data
        {
            get => marker;
            set
            {
                marker = value;
                InitializeParameters();
            }
        }

        public GUID Id => marker.id;

        public Color Color
        {
            get => marker.color;
            set
            {
                marker.color = value;
                _image.color = value;
            }
        }

        public string Label
        {
            get => marker.labelText;
            set
            {
                marker.labelText = value;
                _text.text = value;
            }
        }


        private void Start()
        {
            InitializeParameters();
        }

        private void InitializeParameters()
        {
            _text = GetComponentInChildren<TMP_Text>();
            _image = GetComponentInChildren<Image>();
            _text.text = marker.labelText;
            _image.color = marker.color;
        }
    }
}