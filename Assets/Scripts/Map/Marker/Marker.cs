using System;
using ExtensionMethods;
using UnityEngine;
using UnityEngine.Events;

namespace Map
{
    [Serializable]
    public class Marker
    {
        public UnityEvent<Vector2> onPositionChange;
        public UnityEvent<MarkerState> onStateChange;
        public UnityEvent<string> onLabelChange;
        public UnityEvent<bool> onSelected;

        public readonly Guid id;

        private string _labelText;

        private Vector2 _normalizedPosition;

        private MarkerState _state;
        private Vector3 _worldPosition;

        public Marker(Vector2 normalizedPos, Vector3 worldPos, string label = "")
        {
            _normalizedPosition = normalizedPos;
            _worldPosition = worldPos;

            _labelText = label ?? Vector3Int.RoundToInt(_worldPosition).ToString();

            id = Guid.NewGuid();

            IsSelected = false;
            _state = MarkerState.Unchecked;

            onPositionChange = new UnityEvent<Vector2>();
            onStateChange = new UnityEvent<MarkerState>();
            onLabelChange = new UnityEvent<string>();
            onSelected = new UnityEvent<bool>();
        }

        // Solo el normalizedPos => Se calcula la normalized
        public Marker(Vector2 normalizedPos, string label = "")
            : this(
                normalizedPos,
                ToWorldPos(normalizedPos),
                label
            )
        {
        }

        public Vector2 NormalizedPosition
        {
            get => _normalizedPosition;
            set
            {
                _normalizedPosition = value;
                _worldPosition = ToWorldPos(value);
                onPositionChange.Invoke(value);
            }
        }

        public Vector3 WorldPosition
        {
            get => _worldPosition;
            set
            {
                _worldPosition = value;
                _normalizedPosition = ToNormPos(value);
                onPositionChange.Invoke(_normalizedPosition);
            }
        }

        public string LabelText
        {
            get => _labelText;
            set
            {
                _labelText = value;
                onLabelChange.Invoke(value);
            }
        }


        public MarkerState State
        {
            get => _state;
            set
            {
                _state = value;
                onStateChange.Invoke(value);
            }
        }

        public bool IsNext => State == MarkerState.Next;
        public bool IsChecked => State == MarkerState.Checked;
        public bool IsUnchecked => State == MarkerState.Unchecked;

        public bool IsSelected { get; private set; }

        public void Select()
        {
            IsSelected = true;
            onSelected.Invoke(true);
        }

        public void Deselect()
        {
            IsSelected = false;
            onSelected.Invoke(false);
        }

        private static Vector3 ToWorldPos(Vector2 normalizedPos)
        {
            return MapManager.Instance.TerrainData.GetWorldPosition(normalizedPos);
        }

        private static Vector3 ToNormPos(Vector3 worldPos)
        {
            return MapManager.Instance.TerrainData.GetNormalizedPosition(worldPos);
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