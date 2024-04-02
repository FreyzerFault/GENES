using System;
using DavidUtils.ExtensionMethods;
using UnityEngine;

namespace Map
{
    [Serializable]
    public class Marker
    {
        [SerializeField] private Vector2 normalizedPosition;

        [SerializeField] private Vector3 worldPosition;

        [SerializeField] private string labelText;

        [SerializeField] private MarkerState state;

        [SerializeField] private bool selected;

        [SerializeField] public bool hovered;

        public Marker(Vector2 normalizedPos, Vector3? worldPos = null, string label = "")
        {
            normalizedPosition = normalizedPos;
            worldPosition = worldPos ?? Terrain.GetWorldPosition(normalizedPos);

            labelText = label == "" ? Vector3Int.RoundToInt(worldPosition).ToString() : label;

            Selected = false;
            state = MarkerState.Unchecked;
        }

        public float CollisionRadius => MarkerManager.Instance.collisionRadius;

        private Terrain Terrain => MapManager.Terrain;

        public Vector2 NormalizedPosition
        {
            get => normalizedPosition;
            set
            {
                normalizedPosition = value;
                worldPosition = Terrain.GetWorldPosition(value);
                OnPositionChange?.Invoke(value);
            }
        }

        public Vector3 WorldPosition
        {
            get => worldPosition;
            set
            {
                worldPosition = value;
                normalizedPosition = Terrain.GetNormalizedPosition(value);
                OnPositionChange?.Invoke(normalizedPosition);
            }
        }

        public string LabelText
        {
            get => labelText;
            set
            {
                labelText = value;
                OnLabelChange?.Invoke(value);
            }
        }

        public MarkerState State
        {
            get => state;
            set
            {
                state = value;
                OnStateChange?.Invoke(value);
            }
        }

        public bool IsNext => State == MarkerState.Next;
        public bool IsChecked => State == MarkerState.Checked;
        public bool IsUnchecked => State == MarkerState.Unchecked;

        public bool Selected
        {
            get => selected;
            set
            {
                selected = value;
                OnSelected?.Invoke(value);
            }
        }

        public event Action<string> OnLabelChange;
        public event Action<Vector2> OnPositionChange;
        public event Action<MarkerState> OnStateChange;
        public event Action<bool> OnSelected;

        // ==================== //

        // Collision Test
        public bool Collide(Marker marker) => Distance2D(marker) < CollisionRadius;

        public bool IsAtPoint(Vector2 normalizedPos) => Distance2D(normalizedPos) < CollisionRadius;

        // 2D Distance
        public float Distance2D(Vector2 normalizedPos) =>
            Vector2.Distance(NormalizedPosition, normalizedPos);

        public float Distance2D(Marker marker) => Distance2D(marker.NormalizedPosition);

        // 3D Distance
        public float Distance3D(Vector3 globalPos) => Vector3.Distance(WorldPosition, globalPos);

        public float Distance3D(Marker marker) => Distance3D(marker.WorldPosition);
    }

    [Serializable]
    public enum MarkerState
    {
        Next,
        Checked,
        Unchecked
    }
}