using System.Linq;
using DavidUtils.ExtensionMethods;
using DavidUtils.Rendering;
using UnityEngine;

namespace GENES.TreesGeneration.Rendering
{
    [RequireComponent(typeof(PointsRenderer))]
    public class OliveRegionRenderer : RegionRenderer
    {
        private OliveRegionData OliveData => (OliveRegionData) Data;
        
        private PointsRenderer _pointsRenderer;
        
        protected override void Awake()
        {
            base.Awake();
            
            _pointsRenderer = GetComponent<PointsRenderer>() ?? gameObject.AddComponent<PointsRenderer>();
        }

        protected override void UpdateData()
        {
            base.UpdateData();

            UpdatePointsPositions();
            UpdatePointsRadius();
            UpdatePointsColor();
        }

        private void UpdatePointsPositions() => _pointsRenderer.UpdateAllObj(OliveData.Olivos);
        private void UpdatePointsRadius() => _pointsRenderer.RadiusByPoint = OliveData.radiusByPoint;
        private void UpdatePointsColor() => 
            _pointsRenderer.Colors = ColorByType(OliveData.oliveType).ToFilledArray(OliveData.OlivosCount).ToArray();

        public override void Clear()
        {
            base.Clear();
            _pointsRenderer.Clear();
        }

        public override void ProjectOnTerrain(float offset = 0.1f, bool scaleToTerrainBounds = true)
        {
            base.ProjectOnTerrain(offset, scaleToTerrainBounds);
            _pointsRenderer.ProjectedOnTerrain = true;
        }

        #region COLORS

        private Color ColorByType(OliveType type) =>
            OliveData.oliveType switch
            {
                OliveType.Traditional => "#627b34".ToUnityColor(),
                OliveType.Intesive => "#6cb74a".ToUnityColor(),
                OliveType.SuperIntesive => "#36b733".ToUnityColor(),
                _ => Color.white
            };

        #endregion
    }
}
