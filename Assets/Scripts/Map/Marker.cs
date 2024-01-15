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
        private static readonly Color DefaultColor = Color.white;

        public Vector2 normalizedPosition;
        public Vector3 worldPosition;

        public Color color;
        public string labelText;
        public bool selected;

        public UnityEvent<MarkerState> OnStateChange;

        public readonly GUID id;


        public Marker(Vector2 normalizedPos, Vector3 worldPos, string label = "",
            Color? color = null)
        {
            normalizedPosition = normalizedPos;
            worldPosition = worldPos;

            this.color = color ?? DefaultColor;
            labelText = label ?? worldPosition.ToString();

            id = GUID.Generate();

            State = MarkerState.Unchecked;
            selected = false;
            OnStateChange = new UnityEvent<MarkerState>();
        }

        // Solo el normalizedPos => Se calcula la normalized
        public Marker(Vector2 normalizedPos, string label = "", Color? color = null)
            : this(
                normalizedPos,
                MapManager.Instance.TerrainData.GetWorldPosition(normalizedPos),
                label,
                color
            )
        {
        }

        public MarkerState State
        {
            get => State;
            set
            {
                State = value;
                OnStateChange.Invoke(value);
            }
        }


        public bool IsNext => State == MarkerState.Next;
        public bool IsChecked => State == MarkerState.Checked;
    }


    [Serializable]
    public enum MarkerState
    {
        Next,
        Checked,
        Unchecked
    }
}