using System;
using DavidUtils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Core
{
    public enum MapRenderMode
    {
        Minimap,
        Fullscreen,
        Hidden
    }
    
    public class MapInputController : Singleton<MapInputController>
    {

        protected override void Awake()
        {
            base.Awake();
            GameManager.Instance.onGameStateChanged.AddListener(OnGameStateChanged);
        }

        private void Start() => HandleRenderModeChanged();


        private void OnGameStateChanged(GameManager.GameState gameState)
        {
            RenderMode = gameState switch
            {
                GameManager.GameState.Playing => MapRenderMode.Minimap,
                GameManager.GameState.Paused => MapRenderMode.Fullscreen,
                _ => MapRenderMode.Hidden
            };
        }
        
        
        #region STATE

        // El Render Mode cambia entre el Minimapa y el Mapa Completo
        // El Minimapa no es interactuable y el Mapa Completo si
        // Por lo que deben gestionarse independientemente
        [SerializeField] private MapRenderMode renderMode;

        [SerializeField] private GameObject minimapHUD;
        [SerializeField] private GameObject fullScreenMapHUD;

        public event Action<MapRenderMode> OnRenderModeChanged;

        public MapRenderMode RenderMode
        {
            get => renderMode;
            set
            {
                renderMode = value;
                HandleRenderModeChanged();
            }
        }
        
        private void HandleRenderModeChanged()
        {
            if ( fullScreenMapHUD == null && minimapHUD == null) return;
            if (minimapHUD != null)
                minimapHUD.SetActive(renderMode == MapRenderMode.Minimap);
            if ( fullScreenMapHUD != null)
                fullScreenMapHUD.SetActive(renderMode == MapRenderMode.Fullscreen);
            
            OnRenderModeChanged?.Invoke(renderMode);
        }
        
        #endregion
        
        
        #region ZOOM

        public event Action<float> OnZoom;
        
        private void OnZoomInOut(InputValue value)
        {
            float zoomScale = Mathf.Clamp(value.Get<float>(), -1, 1);
            if (zoomScale == 0) return;
            
            OnZoom?.Invoke(zoomScale);
        }
        
        #endregion
       
        
        #region MARKERS

        #endregion
    }
}
