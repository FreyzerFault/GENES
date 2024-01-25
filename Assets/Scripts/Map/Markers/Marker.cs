using System;
using ExtensionMethods;
using UnityEngine;

namespace Map.Markers
{
    [Serializable]
    public class Marker
    {
        [SerializeField] private string _labelText;

        [SerializeField] private Vector2 _normalizedPosition;
        [SerializeField] private Vector3 _worldPosition;

        [SerializeField] private MarkerState _state;

        public Marker(Vector2 normalizedPos, Vector3? worldPos = null, string label = "")
        {
            _normalizedPosition = normalizedPos;
            _worldPosition = worldPos ?? ToWorldPos(normalizedPos);

            _labelText = label == "" ? Vector3Int.RoundToInt(_worldPosition).ToString() : label;

            IsSelected = false;
            _state = MarkerState.Unchecked;
        }

        public Vector2 NormalizedPosition
        {
            get => _normalizedPosition;
            set
            {
                _normalizedPosition = value;
                _worldPosition = ToWorldPos(value);
                onPositionChange?.Invoke(this, value);
            }
        }

        public Vector3 WorldPosition
        {
            get => _worldPosition;
            set
            {
                _worldPosition = value;
                _normalizedPosition = ToNormPos(value);
                onPositionChange?.Invoke(this, _normalizedPosition);
            }
        }

        public string LabelText
        {
            get => _labelText;
            set
            {
                _labelText = value;
                onLabelChange?.Invoke(this, value);
            }
        }


        public MarkerState State
        {
            get => _state;
            set
            {
                _state = value;
                onStateChange?.Invoke(this, value);
            }
        }

        public bool IsNext => State == MarkerState.Next;
        public bool IsChecked => State == MarkerState.Checked;
        public bool IsUnchecked => State == MarkerState.Unchecked;

        public bool IsSelected { get; private set; }
        public event EventHandler<string> onLabelChange;
        public event EventHandler<Vector2> onPositionChange;
        public event EventHandler<bool> onSelected;
        public event EventHandler<MarkerState> onStateChange;

        public void Select()
        {
            IsSelected = true;
            onSelected?.Invoke(this, true);
        }

        public void Deselect()
        {
            IsSelected = false;
            onSelected?.Invoke(this, false);
        }

        private static Vector3 ToWorldPos(Vector2 normalizedPos)
        {
            return MapManager.Instance.TerrainData.GetWorldPosition(normalizedPos);
        }

        private static Vector3 ToNormPos(Vector3 worldPos)
        {
            return MapManager.Instance.TerrainData.GetNormalizedPosition(worldPos);
        }

        public override bool Equals(object obj)
        {
            return obj is Marker marker && marker.IsAtPoint(marker._normalizedPosition, 0.01f);
        }
    }

    [Serializable]
    public enum MarkerState
    {
        Next,
        Checked,
        Unchecked
    }
}