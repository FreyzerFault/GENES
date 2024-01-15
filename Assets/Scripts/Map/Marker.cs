using System;
using ExtensionMethods;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Map
{
    [Serializable]
    public class Marker
    {

        public readonly GUID id;
        
        private Vector2 _normalizedPosition;
        private Vector3 _worldPosition;

        private string _labelText;

        private MarkerState _state;
        private bool _selected;

        public UnityEvent<Vector2> onPositionChange;
        public UnityEvent<MarkerState> onStateChange;
        public UnityEvent<string> onLabelChange;
        public UnityEvent<bool> onSelected;
        
        public Marker(Vector2 normalizedPos, Vector3 worldPos, string label = "")
        {
            _normalizedPosition = normalizedPos;
            _worldPosition = worldPos;

            _labelText = label ?? Vector3Int.RoundToInt(_worldPosition).ToString();

            id = GUID.Generate();

            _selected = false;
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

        public void Select()
        {
            _selected = true;
            onSelected.Invoke(true);
        }
        public void Deselect()
        {
            _selected = false;
            onSelected.Invoke(false);
        }

        public bool IsNext => State == MarkerState.Next;
        public bool IsChecked => State == MarkerState.Checked;
        public bool IsUnchecked => State == MarkerState.Unchecked;
        
        public bool IsSelected => _selected;
        
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