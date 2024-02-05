using System;
using ExtensionMethods;
using UnityEngine;
using Utils;

namespace Map
{
    public enum MapState
    {
        Minimap,
        Fullscreen,
        Hidden
    }

    public class MapManager : Singleton<MapManager>
    {
        [SerializeField] private GameObject player;
        [SerializeField] private GameObject water;

        public PathGenerator mainPathGenerator;

        [SerializeField] private MapState mapState;

        [SerializeField] private float zoomMap = 1;
        [SerializeField] private float zoomMinimap = 1;

        private float[,] _heightMap;

        public MapState MapState
        {
            get => mapState;
            set
            {
                mapState = value;
                OnStateChanged?.Invoke(value);
            }
        }

        private float Zoom
        {
            get => mapState switch
            {
                MapState.Minimap => zoomMinimap,
                MapState.Fullscreen => zoomMap,
                _ => -1
            };
            set
            {
                switch (mapState)
                {
                    case MapState.Fullscreen:
                        zoomMap = value;
                        OnZoomChanged?.Invoke(value);
                        break;
                    case MapState.Minimap:
                        zoomMinimap = value;
                        OnZoomChanged?.Invoke(value);
                        break;
                    case MapState.Hidden:
                    default:
                        break;
                }
            }
        }

        // TERRAIN
        public static Terrain Terrain => Terrain.activeTerrain;

        public float TerrainWidth => Terrain.terrainData.size.x;
        public float TerrainHeight => Terrain.terrainData.size.z;

        public float WaterHeight => water.transform.position.y;

        // PLAYER
        public Vector3 PlayerPosition => player.transform.position;
        public Vector3 PlayerForward => player.transform.forward;
        public Vector2 PlayerNormalizedPosition => Terrain.GetNormalizedPosition(player.transform.position);
        public Vector2 PlayerDistanceToTopRightCorner => Vector2.one - PlayerNormalizedPosition;

        private float PlayerRotationAngle => player.transform.eulerAngles.y;

        public Quaternion PlayerRotationForUI =>
            Quaternion.AngleAxis(90 + PlayerRotationAngle, Vector3.back);

        private new void Awake()
        {
            base.Awake();

            player = GameObject.FindGameObjectWithTag("Player");
            water = GameObject.FindGameObjectWithTag("Water");
        }

        public event Action<MapState> OnStateChanged;
        public event Action<float> OnZoomChanged;

        public void ZoomIn(float zoomAmount = 0.1f) => Zoom += zoomAmount;

        public bool IsLegalPos(Vector2 normPos) => mainPathGenerator.IsLegalPos(normPos);
        public bool IsLegalPos(Vector3 normPos) => mainPathGenerator.IsLegalPos(normPos);
    }
}