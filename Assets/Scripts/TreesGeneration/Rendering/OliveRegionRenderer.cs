using DavidUtils.Rendering;
using UnityEngine;

namespace GENES.TreesGeneration.Rendering
{
    [RequireComponent(typeof(PointsRenderer))]
    public class OliveRegionRenderer : RegionRenderer
    {
        private OliveRegionData OliveData => (OliveRegionData) Data;
        
        private PointsRenderer _olivesRenderer;
        
        protected override void Awake()
        {
            base.Awake();
            
            _olivesRenderer = GetComponent<PointsRenderer>() ?? gameObject.AddComponent<PointsRenderer>();
        }

        protected override void UpdateData()
        {
            _olivesRenderer.UpdateAllObj(OliveData.Olivos);
            UpdateRadius();
        }

        private void UpdateRadius() => _olivesRenderer.RadiusByPoint = OliveData.radiusByPoint;

        public override void Clear()
        {
            base.Clear();
            _olivesRenderer.Clear();
        }

        public override void ProjectOnTerrain(float offset = 0.1f, bool scaleToTerrainBounds = true)
        {
            base.ProjectOnTerrain(offset, scaleToTerrainBounds);
            _olivesRenderer.ProjectedOnTerrain = true;
        }
    }
}
