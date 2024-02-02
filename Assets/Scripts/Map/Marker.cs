using System;
using ExtensionMethods;
using UnityEngine;

namespace Map
{
    [Serializable]
    public class Marker
    {
        public static readonly float CollisionRadius = 0.05f;
        [SerializeField] private Vector2 normalizedPosition;
        [SerializeField] private Vector3 worldPosition;

        [SerializeField] private string labelText;
        [SerializeField] private MarkerState state;

        public Marker(Vector2 normalizedPos, Vector3? worldPos = null, string label = "")
        {
            normalizedPosition = normalizedPos;
            worldPosition = worldPos ?? Terrain.GetWorldPosition(normalizedPos);

            labelText = label == "" ? Vector3Int.RoundToInt(worldPosition).ToString() : label;

            IsSelected = false;
            state = MarkerState.Unchecked;
        }

        private Terrain Terrain => MapManager.Terrain;

        public Vector2 NormalizedPosition
        {
            get => normalizedPosition;
            set
            {
                normalizedPosition = value;
                worldPosition = Terrain.GetWorldPosition(value);
                OnPositionChange?.Invoke(this, value);
            }
        }

        public Vector3 WorldPosition
        {
            get => worldPosition;
            set
            {
                worldPosition = value;
                normalizedPosition = Terrain.GetNormalizedPosition(value);
                OnPositionChange?.Invoke(this, normalizedPosition);
            }
        }

        public string LabelText
        {
            get => labelText;
            set
            {
                labelText = value;
                OnLabelChange?.Invoke(this, value);
            }
        }


        public MarkerState State
        {
            get => state;
            set
            {
                state = value;
                OnStateChange?.Invoke(this, value);
            }
        }

        public bool IsNext => State == MarkerState.Next;
        public bool IsChecked => State == MarkerState.Checked;
        public bool IsUnchecked => State == MarkerState.Unchecked;
        public bool IsSelected { get; private set; }

        public event EventHandler<string> OnLabelChange;
        public event EventHandler<Vector2> OnPositionChange;
        public event EventHandler<bool> OnSelected;
        public event EventHandler<MarkerState> OnStateChange;

        public void Select()
        {
            IsSelected = true;
            OnSelected?.Invoke(this, true);
        }

        public void Deselect()
        {
            IsSelected = false;
            OnSelected?.Invoke(this, false);
        }

        // ==================== //

        // Collision Test
        public bool Collide(Marker marker) => Distance2D(marker) < CollisionRadius;

        public bool IsAtPoint(Vector2 normalizedPos) => DistanceTo(normalizedPos) < CollisionRadius;

        // 2D Distance
        public float DistanceTo(Vector2 normalizedPos) => Vector2.Distance(NormalizedPosition, normalizedPos);

        public float Distance2D(Marker marker) => DistanceTo(marker.NormalizedPosition);

        // 3D Distance
        public float DistanceTo(Vector3 globalPos) => Vector3.Distance(WorldPosition, globalPos);

        public float Distance3D(Marker marker) => DistanceTo(marker.WorldPosition);
    }

    [Serializable]
    public enum MarkerState
    {
        Next,
        Checked,
        Unchecked
    }
}